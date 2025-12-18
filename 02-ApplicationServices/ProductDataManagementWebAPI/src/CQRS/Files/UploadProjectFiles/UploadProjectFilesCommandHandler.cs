using Business.Interfaces.Configurations;
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
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UploadProjectFilesCommandHandler> logger;

        public UploadProjectFilesCommandHandler(
            IRepository<ProjectFile> projectFileRepo,
            IRepository<ProjectFileVersion> projectFileVersionRepo,
            IRepository<ProjectFileVersionComment> commentRepo,
            IRepository<ProjectFilePackage> projectFilePackageRepo,
            IBlobStorageService blobStorageService,
            ICurrentUser currentUser,
            ILogger<UploadProjectFilesCommandHandler> logger)
        {
            this.projectFileRepo = projectFileRepo;
            this.projectFileVersionRepo = projectFileVersionRepo;
            this.commentRepo = commentRepo;
            this.projectFilePackageRepo = projectFilePackageRepo;
            this.blobStorageService = blobStorageService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(UploadProjectFilesCommand request, CancellationToken cancellationToken)
        {
            // Get existing package by ID (validator ensures it exists and belongs to current user)
            ProjectFilePackage package = (await projectFilePackageRepo.GetFirstBySearch(
                pfp => pfp.Id == request.ProjectFilePackageId &&
                       pfp.ProjectId == request.ProjectId &&
                       pfp.TenantId == request.TenantId &&
                       pfp.OwnerId == currentUser.Id &&
                       !pfp.IsDeleted))!;

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

                    // Utworzenie ProjectFile - Id jest generowane automatycznie przez BaseEntity
                    ProjectFile projectFile = new ProjectFile
                    {
                        TenantId = request.TenantId,
                        ProjectId = request.ProjectId,
                        ProjectFilePackageId = package.Id,
                        OwnerId = currentUser.Id,
                        FileName = file.FileName,
                        DisplayName = displayName,
                        CurrentVersionId = null, // Ustawiamy null, zaktualizujemy po utworzeniu wersji
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    // Pobieramy wygenerowane ID
                    Guid fileId = projectFile.Id;

                    string fileExtension = Path.GetExtension(file.FileName);
                    int versionNumber = 1;
                    
                    // Tworzymy ProjectFileVersion - Id jest generowane automatycznie
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

                    // Ustawiamy blob path i nazwę pliku w wersji
                    firstVersion.BlobFileName = blobFileName;
                    firstVersion.BlobPath = blobPath;

                    // Upload pliku do blob storage przed zapisem do bazy
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

                    // Zapisujemy zmiany, aby ProjectFile i ProjectFileVersion zostały zapisane przed ustawieniem CurrentVersionId
                    await projectFileRepo.SaveChangesAsync(cancellationToken);

                    // Teraz ustawiamy CurrentVersionId i aktualizujemy
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

            return Unit.Value;
        }
    }
}
