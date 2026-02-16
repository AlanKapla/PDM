using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Helpers;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.UploadProjectFiles
{
    public class UploadProjectFilesCommandHandler : IRequestHandler<UploadProjectFilesCommand, Unit>
    {
        private readonly IRepository<ProjectFile> projectFileRepo;
        private readonly IRepository<ProjectFileVersion> projectFileVersionRepo;
        private readonly IRepository<ProjectFileVersionComment> commentRepo;
        private readonly IRepository<ProjectFilePackage> projectFilePackageRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly IProjectFilesService projectFilesService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UploadProjectFilesCommandHandler> logger;

        public UploadProjectFilesCommandHandler(
            IRepository<ProjectFile> projectFileRepo,
            IRepository<ProjectFileVersion> projectFileVersionRepo,
            IRepository<ProjectFileVersionComment> commentRepo,
            IRepository<ProjectFilePackage> projectFilePackageRepo,
            IBlobStorageService blobStorageService,
            IProjectFilesService projectFilesService,
            ICurrentUser currentUser,
            ILogger<UploadProjectFilesCommandHandler> logger)
        {
            this.projectFileRepo = projectFileRepo;
            this.projectFileVersionRepo = projectFileVersionRepo;
            this.commentRepo = commentRepo;
            this.projectFilePackageRepo = projectFilePackageRepo;
            this.blobStorageService = blobStorageService;
            this.projectFilesService = projectFilesService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(UploadProjectFilesCommand request, CancellationToken cancellationToken)
        {
            // 1. Verify package exists and belongs to the correct project/tenant
            ProjectFilePackage? package = await projectFilePackageRepo.GetFirstBySearch(
                pfp => pfp.Id == request.ProjectFilePackageId &&
                       pfp.ProjectId == request.ProjectId &&
                       pfp.TenantId == request.TenantId &&
                       !pfp.IsDeleted) ?? throw new NotFoundApiException(nameof(ProjectFilePackage), request.ProjectFilePackageId.ToString());

            // 2. Authorization check: tenant admin OR project admin OR package owner
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(request.TenantId, request.ProjectId, cancellationToken);
            bool isPackageOwner = package.OwnerId == currentUser.Id;
            
            if (!isAdmin && !isPackageOwner)
            {
                throw new NotFoundApiException(nameof(ProjectFilePackage), request.ProjectFilePackageId.ToString());
            }

            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);
            string packageNameForBlob = FileHelper.NormalizePackageNameForBlobPath(package.Name);

            foreach (FileUploadItem fileItem in request.Files)
            {
                try
                {
                    IFormFile file = fileItem.File;
                    
                    string displayName = !string.IsNullOrWhiteSpace(fileItem.DisplayName)
                        ? fileItem.DisplayName
                        : Path.GetFileNameWithoutExtension(file.FileName);

                    // Create ProjectFile - Id is generated automatically by BaseEntity
                    ProjectFile projectFile = new ProjectFile
                    {
                        TenantId = request.TenantId,
                        ProjectId = request.ProjectId,
                        ProjectFilePackageId = package.Id,
                        OwnerId = currentUser.Id,
                        FileName = file.FileName,
                        DisplayName = displayName,
                        CurrentVersionId = null,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    Guid fileId = projectFile.Id;

                    string fileExtension = Path.GetExtension(file.FileName);
                    int versionNumber = 1;
                    
                    // Create ProjectFileVersion - Id is generated automatically
                    ProjectFileVersion firstVersion = new ProjectFileVersion
                    {
                        ProjectFileId = fileId,
                        VersionNumber = versionNumber,
                        CreatedByUserId = currentUser.Id,
                        ContentType = file.ContentType,
                        FileSizeBytes = file.Length,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    Guid versionId = firstVersion.Id;
                    string blobFileName = $"{versionId}{fileExtension}";
                    string blobPath = $"{request.TenantId}/{request.ProjectId}/{packageNameForBlob}/{fileId}/{versionNumber}/{blobFileName}";

                    firstVersion.BlobFileName = blobFileName;
                    firstVersion.BlobPath = blobPath;

                    // Upload file to blob storage before saving to database
                    using (Stream stream = file.OpenReadStream())
                    {
                        await blobStorageService.UploadAsync(
                            containerName,
                            blobPath,
                            stream,
                            file.ContentType,
                            cancellationToken);
                    }

                    await projectFileRepo.Insert(projectFile);
                    await projectFileVersionRepo.Insert(firstVersion);

                    // Save changes to ensure ProjectFile and ProjectFileVersion are saved before setting CurrentVersionId
                    await projectFileRepo.SaveChangesAsync(cancellationToken);

                    // Now set CurrentVersionId and update
                    projectFile.CurrentVersionId = versionId;
                    await projectFileRepo.Update(projectFile);

                    if (!string.IsNullOrWhiteSpace(fileItem.Comment))
                    {
                        ProjectFileVersionComment comment = new ProjectFileVersionComment
                        {
                            ProjectFileVersionId = versionId,
                            ProjectId = request.ProjectId,
                            UserId = currentUser.Id,
                            TenantId = request.TenantId,
                            Content = fileItem.Comment,
                            CreatedAt = DateTime.UtcNow,
                            IsDeleted = false
                        };

                        await commentRepo.Insert(comment);
                    }

                    logger.LogInformation(
                        "File {FileName} (ID: {FileId}) with version {VersionNumber} uploaded to package {PackageName} (ID: {PackageId}) in project {ProjectId} by user {UserId}",
                        file.FileName, fileId, versionNumber, package.Name, package.Id, request.ProjectId, currentUser.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Error uploading file {FileName} to package {PackageId} in project {ProjectId}",
                        fileItem.File.FileName, request.ProjectFilePackageId, request.ProjectId);
                    throw;
                }
            }

            // Invalidate cache after successful upload
            await projectFilesService.InvalidateProjectFilesCacheAsync(request.TenantId, request.ProjectId, cancellationToken);
            await projectFilesService.InvalidateProjectVersionsCacheAsync(request.TenantId, request.ProjectId, cancellationToken);

            if (request.Files.Any(f => !string.IsNullOrWhiteSpace(f.Comment)))
            {
                await projectFilesService.InvalidateProjectCommentsCacheAsync(request.TenantId, request.ProjectId, cancellationToken);
            }

            return Unit.Value;
        }
    }
}
