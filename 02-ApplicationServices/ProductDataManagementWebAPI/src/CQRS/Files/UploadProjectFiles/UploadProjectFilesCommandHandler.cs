using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Helpers;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.Files;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.UploadProjectFiles
{
    public sealed class UploadProjectFilesCommandHandler : IRequestHandler<UploadProjectFilesCommand, Unit>
    {
        private readonly IRepository<ProjectFile> projectFileRepo;
        private readonly IRepository<ProjectFileVersion> projectFileVersionRepo;
        private readonly IRepository<ProjectFileVersionComment> commentRepo;
        private readonly IRepository<ProjectFilePackage> projectFilePackageRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly IProjectFilesService projectFilesService;
        private readonly IFileAccessGuard fileAccessGuard;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UploadProjectFilesCommandHandler> logger;

        public UploadProjectFilesCommandHandler(
            IRepository<ProjectFile> projectFileRepo,
            IRepository<ProjectFileVersion> projectFileVersionRepo,
            IRepository<ProjectFileVersionComment> commentRepo,
            IRepository<ProjectFilePackage> projectFilePackageRepo,
            IBlobStorageService blobStorageService,
            IProjectFilesService projectFilesService,
            IFileAccessGuard fileAccessGuard,
            ICurrentUser currentUser,
            ILogger<UploadProjectFilesCommandHandler> logger)
        {
            this.projectFileRepo = projectFileRepo;
            this.projectFileVersionRepo = projectFileVersionRepo;
            this.commentRepo = commentRepo;
            this.projectFilePackageRepo = projectFilePackageRepo;
            this.blobStorageService = blobStorageService;
            this.projectFilesService = projectFilesService;
            this.fileAccessGuard = fileAccessGuard;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(UploadProjectFilesCommand request, CancellationToken cancellationToken)
        {
            await fileAccessGuard.EnsureCanAccessPackageAsync(
                request.TenantId, request.ProjectId, request.ProjectFilePackageId, FileAccessKind.Write, cancellationToken);

            ProjectFilePackage package = await GetAndValidatePackageAsync(request, cancellationToken);

            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);
            string packageNameForBlob = FileHelper.NormalizePackageNameForBlobPath(package.Name);

            List<ProjectFile> projectFiles = new List<ProjectFile>();
            List<ProjectFileVersion> versions = new List<ProjectFileVersion>();
            List<ProjectFileVersionComment> comments = new List<ProjectFileVersionComment>();
            List<string> uploadedBlobPaths = new List<string>();

            try
            {
                foreach (FileUploadItem fileItem in request.Files)
                {
                    await UploadSingleFileAsync(
                        request, package, containerName, packageNameForBlob,
                        fileItem, projectFiles, versions, comments, uploadedBlobPaths,
                        cancellationToken);
                }

                // Insert files first with CurrentVersionId = null and versions in a separate SaveChanges
                // to avoid a circular FK dependency (ProjectFile.CurrentVersionId <-> ProjectFileVersion.ProjectFileId).
                // The surrounding TransactionBehavior wraps everything in a DB transaction, so atomicity is preserved.
                Dictionary<Guid, Guid> fileIdToCurrentVersionId = new Dictionary<Guid, Guid>(projectFiles.Count);
                foreach (ProjectFile pf in projectFiles)
                {
                    if (pf.CurrentVersionId is Guid versionId)
                    {
                        fileIdToCurrentVersionId[pf.Id] = versionId;
                        pf.CurrentVersionId = null;
                    }
                }

                await projectFileRepo.InsertRange(projectFiles);
                await projectFileVersionRepo.InsertRange(versions);
                if (comments.Count > 0)
                {
                    await commentRepo.InsertRange(comments);
                }

                await projectFileRepo.SaveChangesAsync(cancellationToken);

                foreach (ProjectFile pf in projectFiles)
                {
                    if (fileIdToCurrentVersionId.TryGetValue(pf.Id, out Guid versionId))
                    {
                        pf.CurrentVersionId = versionId;
                    }
                }

                await InvalidateCachesAsync(request, comments.Count > 0, cancellationToken);

                logger.LogInformation(
                    "Uploaded {FileCount} files to package {PackageName} (ID: {PackageId}) in project {ProjectId} by user {UserId}",
                    projectFiles.Count, package.Name, package.Id, request.ProjectId, currentUser.Id);

                return Unit.Value;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to upload files to package {PackageId} in project {ProjectId}; compensating {BlobCount} blob(s)",
                    request.ProjectFilePackageId, request.ProjectId, uploadedBlobPaths.Count);

                await CompensateBlobsAsync(containerName, uploadedBlobPaths, cancellationToken);
                throw;
            }
        }

        private async Task<ProjectFilePackage> GetAndValidatePackageAsync(
            UploadProjectFilesCommand request, CancellationToken cancellationToken)
        {
            ProjectFilePackage? package = await projectFilePackageRepo.GetFirstBySearch(
                pfp => pfp.Id == request.ProjectFilePackageId &&
                       pfp.ProjectId == request.ProjectId &&
                       pfp.TenantId == request.TenantId);

            return package is null
                ? throw new NotFoundApiException(nameof(ProjectFilePackage), request.ProjectFilePackageId.ToString())
                : package;
        }

        private async Task UploadSingleFileAsync(
            UploadProjectFilesCommand request,
            ProjectFilePackage package,
            string containerName,
            string packageNameForBlob,
            FileUploadItem fileItem,
            List<ProjectFile> projectFiles,
            List<ProjectFileVersion> versions,
            List<ProjectFileVersionComment> comments,
            List<string> uploadedBlobPaths,
            CancellationToken cancellationToken)
        {
            IFormFile file = fileItem.File;
            string displayName = !string.IsNullOrWhiteSpace(fileItem.DisplayName)
                ? fileItem.DisplayName
                : Path.GetFileNameWithoutExtension(file.FileName);

            ProjectFile projectFile = BuildProjectFile(request, package.Id, file, displayName);
            ProjectFileVersion version = BuildVersion(request, projectFile.Id, file);

            string fileExtension = Path.GetExtension(file.FileName);
            string blobPath = BuildBlobPath(request, packageNameForBlob, projectFile.Id, version.Id, version.VersionNumber, fileExtension);
            version.BlobFileName = $"{version.Id}{fileExtension}";
            version.BlobPath = blobPath;

            using (Stream stream = file.OpenReadStream())
            {
                await blobStorageService.UploadAsync(containerName, blobPath, stream, file.ContentType, cancellationToken);
            }
            uploadedBlobPaths.Add(blobPath);

            projectFile.CurrentVersionId = version.Id;

            projectFiles.Add(projectFile);
            versions.Add(version);

            if (!string.IsNullOrWhiteSpace(fileItem.Comment))
            {
                comments.Add(BuildComment(request, version.Id, fileItem.Comment!));
            }
        }

        private ProjectFile BuildProjectFile(
            UploadProjectFilesCommand request, Guid packageId, IFormFile file, string displayName) =>
            new ProjectFile
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

        private ProjectFileVersion BuildVersion(
            UploadProjectFilesCommand request, Guid fileId, IFormFile file) =>
            new ProjectFileVersion
            {
                ProjectFileId = fileId,
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                VersionNumber = 1,
                CreatedByUserId = currentUser.Id,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

        private static string BuildBlobPath(
            UploadProjectFilesCommand request,
            string packageNameForBlob,
            Guid fileId,
            Guid versionId,
            int versionNumber,
            string fileExtension) =>
            $"{request.TenantId}/{request.ProjectId}/{packageNameForBlob}/{fileId}/{versionNumber}/{versionId}{fileExtension}";

        private ProjectFileVersionComment BuildComment(
            UploadProjectFilesCommand request, Guid versionId, string content) =>
            new ProjectFileVersionComment
            {
                ProjectFileVersionId = versionId,
                ProjectId = request.ProjectId,
                UserId = currentUser.Id,
                TenantId = request.TenantId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

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
                    logger.LogWarning(deleteEx, "Failed to cleanup blob {BlobPath} after upload failure", blobPath);
                }
            }
        }

        private async Task InvalidateCachesAsync(
            UploadProjectFilesCommand request, bool hasComments, CancellationToken cancellationToken)
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
