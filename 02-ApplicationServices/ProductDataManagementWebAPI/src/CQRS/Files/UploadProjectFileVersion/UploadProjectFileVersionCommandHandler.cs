using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Helpers;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.PostCommit;
using Entities.Models.Files;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.UploadProjectFileVersion
{
    public sealed class UploadProjectFileVersionCommandHandler : IRequestHandler<UploadProjectFileVersionCommand, Unit>
    {
        private readonly IRepository<ProjectFile> projectFileRepo;
        private readonly IRepository<ProjectFileVersion> projectFileVersionRepo;
        private readonly IRepository<ProjectFileVersionComment> commentRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly IProjectFilesService projectFilesService;
        private readonly IFileAccessGuard fileAccessGuard;
        private readonly IFileActivityNotificationService activityNotifications;
        private readonly IPostCommitDispatcher postCommitDispatcher;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UploadProjectFileVersionCommandHandler> logger;

        public UploadProjectFileVersionCommandHandler(
            IRepository<ProjectFile> projectFileRepo,
            IRepository<ProjectFileVersion> projectFileVersionRepo,
            IRepository<ProjectFileVersionComment> commentRepo,
            IBlobStorageService blobStorageService,
            IProjectFilesService projectFilesService,
            IFileAccessGuard fileAccessGuard,
            IFileActivityNotificationService activityNotifications,
            IPostCommitDispatcher postCommitDispatcher,
            ICurrentUser currentUser,
            ILogger<UploadProjectFileVersionCommandHandler> logger)
        {
            this.projectFileRepo = projectFileRepo;
            this.projectFileVersionRepo = projectFileVersionRepo;
            this.commentRepo = commentRepo;
            this.blobStorageService = blobStorageService;
            this.projectFilesService = projectFilesService;
            this.fileAccessGuard = fileAccessGuard;
            this.activityNotifications = activityNotifications;
            this.postCommitDispatcher = postCommitDispatcher;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(UploadProjectFileVersionCommand request, CancellationToken cancellationToken)
        {
            await fileAccessGuard.EnsureCanAccessFileAsync(
                request.TenantId, request.ProjectId, request.FileId, FileAccessKind.Write, cancellationToken);

            ProjectFile projectFile = await GetAndValidateFileAsync(request, cancellationToken);
            EnsureSameExtension(projectFile.FileName, request.File.FileName);

            int nextVersionNumber = await GetNextVersionNumberAsync(request.FileId, cancellationToken);
            Guid? oldCurrentVersionId = projectFile.CurrentVersionId;

            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);
            string newFileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            ProjectFileVersion newVersion = BuildNewVersion(request, nextVersionNumber);
            string blobPath = BuildBlobPath(request, projectFile, nextVersionNumber, newVersion.Id, newFileExtension);
            newVersion.BlobFileName = $"{newVersion.Id}{newFileExtension}";
            newVersion.BlobPath = blobPath;

            List<string> uploadedBlobPaths = new List<string>();

            try
            {
                await UploadBlobAsync(containerName, blobPath, request.File, uploadedBlobPaths, cancellationToken);

                await projectFileVersionRepo.Insert(newVersion);

                projectFile.CurrentVersionId = newVersion.Id;
                await projectFileRepo.Update(projectFile);

                if (!string.IsNullOrWhiteSpace(request.Comment))
                {
                    await commentRepo.Insert(BuildComment(request, newVersion.Id));
                }

                await InvalidateCachesAsync(request, oldCurrentVersionId, cancellationToken);

                FileActivityNotificationContext notificationContext = BuildNotificationContext(
                    request, projectFile, newVersion.Id);
                postCommitDispatcher.Enqueue(ct =>
                    activityNotifications.NotifyVersionUploadedAsync(notificationContext, ct));

                logger.LogInformation(
                    "Created new version {VersionNumber} for file {FileId} in project {ProjectId}. Blob path: {BlobPath}. Comment: {HasComment}",
                    nextVersionNumber, request.FileId, request.ProjectId, blobPath, !string.IsNullOrWhiteSpace(request.Comment));

                return Unit.Value;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to upload new version for file {FileId} in project {ProjectId}. Version number: {VersionNumber}",
                    request.FileId, request.ProjectId, nextVersionNumber);

                await CompensateBlobsAsync(containerName, uploadedBlobPaths, cancellationToken);
                throw;
            }
        }

        private async Task<ProjectFile> GetAndValidateFileAsync(
            UploadProjectFileVersionCommand request, CancellationToken cancellationToken)
        {
            ProjectFile? projectFile = await projectFileRepo.GetFirstBySearch(
                pf => pf.Id == request.FileId &&
                      pf.TenantId == request.TenantId &&
                      pf.ProjectId == request.ProjectId,
                include => include.Include(pf => pf.Package));

            if (projectFile is null)
            {
                throw new NotFoundApiException(nameof(ProjectFile), request.FileId.ToString());
            }

            return projectFile;
        }

        private static void EnsureSameExtension(string originalFileName, string newFileName)
        {
            string originalExtension = Path.GetExtension(originalFileName).ToLowerInvariant();
            string newExtension = Path.GetExtension(newFileName).ToLowerInvariant();

            if (originalExtension != newExtension)
            {
                throw new ValidationApiException(
                    $"The new version must have the same extension as the original. Expected: {originalExtension}, received: {newExtension}");
            }
        }

        private async Task<int> GetNextVersionNumberAsync(Guid fileId, CancellationToken cancellationToken)
        {
            List<int> versionNumbers = await projectFileVersionRepo.SelectAsync(
                v => v.ProjectFileId == fileId,
                v => v.VersionNumber,
                cancellationToken);

            return versionNumbers.Count > 0 ? versionNumbers.Max() + 1 : 1;
        }

        private ProjectFileVersion BuildNewVersion(
            UploadProjectFileVersionCommand request, int versionNumber) =>
            new ProjectFileVersion
            {
                ProjectFileId = request.FileId,
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                VersionNumber = versionNumber,
                CreatedByUserId = currentUser.Id,
                ContentType = request.File.ContentType,
                FileSizeBytes = request.File.Length,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

        private static string BuildBlobPath(
            UploadProjectFileVersionCommand request,
            ProjectFile projectFile,
            int versionNumber,
            Guid versionId,
            string fileExtension)
        {
            string packageNameForBlob = FileHelper.NormalizePackageNameForBlobPath(projectFile.Package.Name);
            string blobFileName = $"{versionId}{fileExtension}";
            return $"{request.TenantId}/{request.ProjectId}/{packageNameForBlob}/{request.FileId}/{versionNumber}/{blobFileName}";
        }

        private ProjectFileVersionComment BuildComment(UploadProjectFileVersionCommand request, Guid versionId) =>
            new ProjectFileVersionComment
            {
                ProjectFileVersionId = versionId,
                ProjectId = request.ProjectId,
                UserId = currentUser.Id,
                TenantId = request.TenantId,
                Content = request.Comment!.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

        private async Task UploadBlobAsync(
            string containerName,
            string blobPath,
            IFormFile file,
            List<string> uploadedBlobPaths,
            CancellationToken cancellationToken)
        {
            using Stream stream = file.OpenReadStream();
            await blobStorageService.UploadAsync(containerName, blobPath, stream, file.ContentType, cancellationToken);
            uploadedBlobPaths.Add(blobPath);
        }

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
            UploadProjectFileVersionCommand request,
            Guid? oldCurrentVersionId,
            CancellationToken cancellationToken)
        {
            await projectFilesService.InvalidateProjectFilesCacheAsync(request.TenantId, request.ProjectId, cancellationToken);
            await projectFilesService.InvalidateProjectVersionsCacheAsync(request.TenantId, request.ProjectId, cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.Comment))
            {
                await projectFilesService.InvalidateProjectCommentsCacheAsync(request.TenantId, request.ProjectId, cancellationToken);
            }

            if (oldCurrentVersionId.HasValue)
            {
                await projectFilesService.InvalidateVersionSasUriAsync(oldCurrentVersionId.Value, cancellationToken);
            }
        }

        private FileActivityNotificationContext BuildNotificationContext(
            UploadProjectFileVersionCommand request,
            ProjectFile file,
            Guid versionId) =>
            new FileActivityNotificationContext
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                FileId = file.Id,
                PackageId = file.ProjectFilePackageId,
                OwnerId = file.OwnerId,
                FileDisplayName = file.DisplayName,
                ActorName = $"{currentUser.FirstName} {currentUser.LastName}".Trim(),
                ActorUserId = currentUser.Id,
                VersionId = versionId,
            };
    }
}
