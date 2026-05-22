using Business.Interfaces.Configurations;
using Business.Interfaces.Helpers;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.Files;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.CreatePackageAndUploadFiles
{
    public sealed class CreatePackageAndUploadFilesCommandHandler : IRequestHandler<CreatePackageAndUploadFilesCommand, Unit>
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
            ProjectFilePackage package = BuildPackage(request);
            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);
            string packageNameForBlob = FileHelper.NormalizePackageNameForBlobPath(request.PackageName);

            List<ProjectFile> projectFiles = new List<ProjectFile>();
            List<ProjectFileVersion> versions = new List<ProjectFileVersion>();
            List<ProjectFileVersionComment> comments = new List<ProjectFileVersionComment>();

            BuildFilesAndVersions(request, package.Id, projectFiles, versions, comments);

            List<string> uploadedBlobPaths = new List<string>();

            try
            {
                await UploadBlobsAsync(containerName, packageNameForBlob, request, projectFiles, versions, uploadedBlobPaths, cancellationToken);

                await projectFilePackageRepo.Insert(package);
                await projectFileRepo.InsertRange(projectFiles);
                await projectFileVersionRepo.InsertRange(versions);
                if (comments.Count > 0)
                {
                    await commentRepo.InsertRange(comments);
                }

                await InvalidateCachesAsync(request, comments.Count > 0, cancellationToken);

                logger.LogInformation(
                    "Created package {PackageName} (ID: {PackageId}) with {FileCount} files for project {ProjectId} by user {UserId}",
                    request.PackageName, package.Id, projectFiles.Count, request.ProjectId, currentUser.Id);

                return Unit.Value;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to create package {PackageName} in project {ProjectId}; compensating {BlobCount} blob(s)",
                    request.PackageName, request.ProjectId, uploadedBlobPaths.Count);

                await CompensateBlobsAsync(containerName, uploadedBlobPaths, cancellationToken);
                throw;
            }
        }

        private ProjectFilePackage BuildPackage(CreatePackageAndUploadFilesCommand request) =>
            new ProjectFilePackage
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                OwnerId = currentUser.Id,
                Name = request.PackageName,
                CreatedByUserId = currentUser.Id,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

        private void BuildFilesAndVersions(
            CreatePackageAndUploadFilesCommand request,
            Guid packageId,
            List<ProjectFile> projectFiles,
            List<ProjectFileVersion> versions,
            List<ProjectFileVersionComment> comments)
        {
            foreach (FileUploadItem fileItem in request.Files)
            {
                IFormFile file = fileItem.File;
                string displayName = !string.IsNullOrWhiteSpace(fileItem.DisplayName)
                    ? fileItem.DisplayName
                    : Path.GetFileNameWithoutExtension(file.FileName);

                ProjectFile projectFile = new ProjectFile
                {
                    TenantId = request.TenantId,
                    ProjectId = request.ProjectId,
                    ProjectFilePackageId = packageId,
                    OwnerId = currentUser.Id,
                    FileName = file.FileName,
                    DisplayName = displayName,
                    CurrentVersionId = null,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                ProjectFileVersion firstVersion = new ProjectFileVersion
                {
                    ProjectFileId = projectFile.Id,
                    VersionNumber = 1,
                    CreatedByUserId = currentUser.Id,
                    ContentType = file.ContentType,
                    FileSizeBytes = file.Length,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                projectFile.CurrentVersionId = firstVersion.Id;

                projectFiles.Add(projectFile);
                versions.Add(firstVersion);

                if (!string.IsNullOrWhiteSpace(fileItem.Comment))
                {
                    comments.Add(new ProjectFileVersionComment
                    {
                        ProjectFileVersionId = firstVersion.Id,
                        ProjectId = request.ProjectId,
                        UserId = currentUser.Id,
                        TenantId = request.TenantId,
                        Content = fileItem.Comment!,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    });
                }
            }
        }

        private async Task UploadBlobsAsync(
            string containerName,
            string packageNameForBlob,
            CreatePackageAndUploadFilesCommand request,
            IReadOnlyList<ProjectFile> projectFiles,
            IReadOnlyList<ProjectFileVersion> versions,
            List<string> uploadedBlobPaths,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < request.Files.Count; i++)
            {
                IFormFile file = request.Files[i].File;
                ProjectFile projectFile = projectFiles[i];
                ProjectFileVersion version = versions[i];

                string fileExtension = Path.GetExtension(file.FileName);
                string blobPath = BuildBlobPath(request, packageNameForBlob, projectFile.Id, version.Id, version.VersionNumber, fileExtension);
                version.BlobFileName = $"{version.Id}{fileExtension}";
                version.BlobPath = blobPath;

                using (Stream stream = file.OpenReadStream())
                {
                    await blobStorageService.UploadAsync(containerName, blobPath, stream, file.ContentType, cancellationToken);
                }
                uploadedBlobPaths.Add(blobPath);
            }
        }

        private static string BuildBlobPath(
            CreatePackageAndUploadFilesCommand request,
            string packageNameForBlob,
            Guid fileId,
            Guid versionId,
            int versionNumber,
            string fileExtension) =>
            $"{request.TenantId}/{request.ProjectId}/{packageNameForBlob}/{fileId}/{versionNumber}/{versionId}{fileExtension}";

        private async Task CompensateBlobsAsync(
            string containerName,
            IReadOnlyCollection<string> uploadedBlobPaths,
            CancellationToken cancellationToken)
        {
            foreach (string blobPath in uploadedBlobPaths)
            {
                try
                {
                    await blobStorageService.DeleteAsync(containerName, blobPath, cancellationToken);
                }
                catch (Exception deleteEx)
                {
                    logger.LogWarning(deleteEx, "Failed to cleanup blob {BlobPath} after package creation failure", blobPath);
                }
            }
        }

        private async Task InvalidateCachesAsync(
            CreatePackageAndUploadFilesCommand request, bool hasComments, CancellationToken cancellationToken)
        {
            await projectFilesService.InvalidateProjectFilesCacheAsync(request.TenantId, request.ProjectId, cancellationToken);
            await projectFilesService.InvalidateProjectVersionsCacheAsync(request.TenantId, request.ProjectId, cancellationToken);

            if (hasComments)
            {
                await projectFilesService.InvalidateProjectCommentsCacheAsync(request.TenantId, request.ProjectId, cancellationToken);
            }
        }
    }
}
