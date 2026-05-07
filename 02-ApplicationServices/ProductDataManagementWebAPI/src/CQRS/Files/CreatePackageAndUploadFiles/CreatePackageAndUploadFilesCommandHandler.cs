using Business.Interfaces.Configurations;
using Business.Interfaces.Helpers;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Files;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.CreatePackageAndUploadFiles
{
    public class CreatePackageAndUploadFilesCommandHandler : IRequestHandler<CreatePackageAndUploadFilesCommand, Unit>
    {
        private readonly IRepository<ProjectFile> projectFileRepo;
        private readonly IRepository<ProjectFileVersion> projectFileVersionRepo;
        private readonly IRepository<ProjectFileVersionComment> commentRepo;
        private readonly IRepository<ProjectFilePackage> projectFilePackageRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly IProjectFilesService projectFilesService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<CreatePackageAndUploadFilesCommandHandler> logger;

        public CreatePackageAndUploadFilesCommandHandler(
            IRepository<ProjectFile> projectFileRepo,
            IRepository<ProjectFileVersion> projectFileVersionRepo,
            IRepository<ProjectFileVersionComment> commentRepo,
            IRepository<ProjectFilePackage> projectFilePackageRepo,
            IBlobStorageService blobStorageService,
            IProjectFilesService projectFilesService,
            ICurrentUser currentUser,
            ILogger<CreatePackageAndUploadFilesCommandHandler> logger)
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

        public async Task<Unit> Handle(CreatePackageAndUploadFilesCommand request, CancellationToken cancellationToken)
        {
            // Create new package (validator ensures it doesn't already exist)
            ProjectFilePackage package = new ProjectFilePackage
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                OwnerId = currentUser.Id,
                Name = request.PackageName,
                CreatedByUserId = currentUser.Id,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await projectFilePackageRepo.Insert(package);
            await projectFilePackageRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Created new package {PackageName} (ID: {PackageId}) for user {UserId} in project {ProjectId}",
                request.PackageName, package.Id, currentUser.Id, request.ProjectId);

            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);
            string packageNameForBlob = FileHelper.NormalizePackageNameForBlobPath(request.PackageName);

            var allProjectFiles = new List<ProjectFile>();
            var allProjectFileVersions = new List<ProjectFileVersion>();
            var allComments = new List<ProjectFileVersionComment>();

            foreach (FileUploadItem fileItem in request.Files)
            {
                try
                {
                    IFormFile file = fileItem.File;
                    
                    string displayName = !string.IsNullOrWhiteSpace(fileItem.DisplayName)
                        ? fileItem.DisplayName
                        : Path.GetFileNameWithoutExtension(file.FileName);

                    // Step 1: Create ProjectFile WITHOUT CurrentVersionId to break circular dependency
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

                    allProjectFiles.Add(projectFile);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Error preparing file {FileName} for upload to new package in project {ProjectId}",
                        fileItem.File.FileName, request.ProjectId);
                    throw;
                }
            }

            // Step 2: Insert ProjectFiles first (without CurrentVersionId set)
            if (allProjectFiles.Any())
            {
                await projectFileRepo.InsertRange(allProjectFiles);
                await projectFileRepo.SaveChangesAsync(cancellationToken);
            }

            // Step 3: Now create versions and upload files
            for (int i = 0; i < request.Files.Count; i++)
            {
                FileUploadItem fileItem = request.Files[i];
                ProjectFile projectFile = allProjectFiles[i];

                try
                {
                    IFormFile file = fileItem.File;
                    Guid fileId = projectFile.Id;

                    string fileExtension = Path.GetExtension(file.FileName);
                    int versionNumber = 1;
                    
                    // Create ProjectFileVersion now that ProjectFile exists in database
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

                    // Upload file to blob storage
                    using (Stream stream = file.OpenReadStream())
                    {
                        await blobStorageService.UploadAsync(
                            containerName,
                            blobPath,
                            stream,
                            file.ContentType,
                            cancellationToken);
                    }

                    allProjectFileVersions.Add(firstVersion);

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

                        allComments.Add(comment);
                    }

                    logger.LogInformation(
                        "File {FileName} (ID: {FileId}) with version {VersionNumber} prepared for upload to new package {PackageName} in project {ProjectId} by user {UserId}",
                        file.FileName, fileId, versionNumber, request.PackageName, request.ProjectId, currentUser.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Error uploading file {FileName} to new package in project {ProjectId}",
                        fileItem.File.FileName, request.ProjectId);
                    throw;
                }
            }

            // Step 4: Insert all versions
            if (allProjectFileVersions.Any())
            {
                await projectFileVersionRepo.InsertRange(allProjectFileVersions);
                await projectFileVersionRepo.SaveChangesAsync(cancellationToken);
            }

            // Step 5: Update CurrentVersionId on each ProjectFile now that versions exist
            for (int i = 0; i < allProjectFiles.Count; i++)
            {
                allProjectFiles[i].CurrentVersionId = allProjectFileVersions[i].Id;
                await projectFileRepo.Update(allProjectFiles[i]);
            }

            // Step 6: Insert comments
            if (allComments.Any())
            {
                await commentRepo.InsertRange(allComments);
            }

            // Invalidate all relevant caches
            await projectFilesService.InvalidateProjectFilesCacheAsync(request.TenantId, request.ProjectId, cancellationToken);
            await projectFilesService.InvalidateProjectVersionsCacheAsync(request.TenantId, request.ProjectId, cancellationToken);
            
            if (allComments.Any())
            {
                await projectFilesService.InvalidateProjectCommentsCacheAsync(request.TenantId, request.ProjectId, cancellationToken);
            }

            logger.LogInformation(
                "Created package {PackageName} with {FileCount} files for project {ProjectId}. Cache invalidated.",
                request.PackageName, allProjectFiles.Count, request.ProjectId);

            return Unit.Value;
        }
    }
}
