using Business.Interfaces.Configurations;
using Business.Interfaces.Helpers;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
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
        private readonly ICurrentUser currentUser;
        private readonly ILogger<CreatePackageAndUploadFilesCommandHandler> logger;

        public CreatePackageAndUploadFilesCommandHandler(
            IRepository<ProjectFile> projectFileRepo,
            IRepository<ProjectFileVersion> projectFileVersionRepo,
            IRepository<ProjectFileVersionComment> commentRepo,
            IRepository<ProjectFilePackage> projectFilePackageRepo,
            IBlobStorageService blobStorageService,
            ICurrentUser currentUser,
            ILogger<CreatePackageAndUploadFilesCommandHandler> logger)
        {
            this.projectFileRepo = projectFileRepo;
            this.projectFileVersionRepo = projectFileVersionRepo;
            this.commentRepo = commentRepo;
            this.projectFilePackageRepo = projectFilePackageRepo;
            this.blobStorageService = blobStorageService;
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

                    // Set CurrentVersionId after version is created
                    projectFile.CurrentVersionId = versionId;

                    allProjectFiles.Add(projectFile);
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

            // Insert all entities in batches
            if (allProjectFiles.Any())
            {
                await projectFileRepo.InsertRange(allProjectFiles);
            }

            if (allProjectFileVersions.Any())
            {
                await projectFileVersionRepo.InsertRange(allProjectFileVersions);
            }

            if (allComments.Any())
            {
                await commentRepo.InsertRange(allComments);
            }

            return Unit.Value;
        }
    }
}
