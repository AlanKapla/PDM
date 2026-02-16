using Business.Interfaces.Configurations;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Helpers;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.UploadProjectFileVersion
{
    public class UploadProjectFileVersionCommandHandler : IRequestHandler<UploadProjectFileVersionCommand, Unit>
    {
        private readonly IRepository<ProjectFile> projectFileRepo;
        private readonly IRepository<ProjectFileVersion> projectFileVersionRepo;
        private readonly IRepository<ProjectFileVersionComment> commentRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly IProjectFilesService projectFilesService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UploadProjectFileVersionCommandHandler> logger;

        public UploadProjectFileVersionCommandHandler(
            IRepository<ProjectFile> projectFileRepo,
            IRepository<ProjectFileVersion> projectFileVersionRepo,
            IRepository<ProjectFileVersionComment> commentRepo,
            IBlobStorageService blobStorageService,
            IProjectFilesService projectFilesService,
            ICurrentUser currentUser,
            ILogger<UploadProjectFileVersionCommandHandler> logger)
        {
            this.projectFileRepo = projectFileRepo;
            this.projectFileVersionRepo = projectFileVersionRepo;
            this.commentRepo = commentRepo;
            this.blobStorageService = blobStorageService;
            this.projectFilesService = projectFilesService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(UploadProjectFileVersionCommand request, CancellationToken cancellationToken)
        {
            // 1. Verify file exists and belongs to the correct project/tenant
            var projectFiles = await projectFileRepo.GetBySearch(
                pf => pf.Id == request.FileId &&
                      pf.TenantId == request.TenantId &&
                      pf.ProjectId == request.ProjectId &&
                      !pf.IsDeleted,
                include => include.Include(pf => pf.Package)
                                  .Include(pf => pf.Versions.Where(v => !v.IsDeleted))
                                  .Include(pf => pf.SharedWith)
            );

            ProjectFile? projectFile = projectFiles.FirstOrDefault()
                ?? throw new NotFoundApiException(nameof(ProjectFile), request.FileId.ToString());

            // 2. Authorization check: tenant admin OR project admin OR file owner OR user with share access
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(request.TenantId, request.ProjectId, cancellationToken);
            bool isFileOwner = projectFile.OwnerId == currentUser.Id;
            bool hasShareAccess = projectFile.SharedWith.Any(sf => sf.SharedWithUserId == currentUser.Id);
            
            if (!isAdmin && !isFileOwner && !hasShareAccess)
            {
                throw new NotFoundApiException(nameof(ProjectFile), request.FileId.ToString());
            }

            // 3. Verify file extension matches original
            string originalExtension = Path.GetExtension(projectFile.FileName).ToLowerInvariant();
            string newFileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();

            if (originalExtension != newFileExtension)
            {
                throw new ValidationApiException(
                    $"The new version must have the same extension as the original. Expected: {originalExtension}, received: {newFileExtension}");
            }

            // 4. Calculate next version number
            int nextVersionNumber = projectFile.Versions.Any()
                ? projectFile.Versions.Max(v => v.VersionNumber) + 1
                : 1;

            // Store old current version ID for cache invalidation
            Guid? oldCurrentVersionId = projectFile.CurrentVersionId;

            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);
            string packageNameForBlob = FileHelper.NormalizePackageNameForBlobPath(projectFile.Package.Name);

            // 5. Create new version
            ProjectFileVersion newVersion = new ProjectFileVersion
            {
                ProjectFileId = request.FileId,
                VersionNumber = nextVersionNumber,
                CreatedByUserId = currentUser.Id,
                ContentType = request.File.ContentType,
                FileSizeBytes = request.File.Length,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            Guid versionId = newVersion.Id;
            string blobFileName = $"{versionId}{newFileExtension}";
            string blobPath = $"{request.TenantId}/{request.ProjectId}/{packageNameForBlob}/{request.FileId}/{nextVersionNumber}/{blobFileName}";

            newVersion.BlobFileName = blobFileName;
            newVersion.BlobPath = blobPath;

            try
            {
                // 6. Upload file to blob storage
                using (Stream stream = request.File.OpenReadStream())
                {
                    await blobStorageService.UploadAsync(
                        containerName,
                        blobPath,
                        stream,
                        request.File.ContentType,
                        cancellationToken);
                }

                // 7. Save version to database
                await projectFileVersionRepo.Insert(newVersion);

                // 8. Update CurrentVersionId in ProjectFile
                projectFile.CurrentVersionId = versionId;
                await projectFileRepo.Update(projectFile);

                // 9. Add comment if provided
                if (!string.IsNullOrWhiteSpace(request.Comment))
                {
                    ProjectFileVersionComment comment = new ProjectFileVersionComment
                    {
                        ProjectFileVersionId = versionId,
                        ProjectId = request.ProjectId,
                        UserId = currentUser.Id,
                        TenantId = request.TenantId,
                        Content = request.Comment.Trim(),
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    await commentRepo.Insert(comment);
                }

                // 10. Save all changes
                await projectFileRepo.SaveChangesAsync(cancellationToken);

                // 11. Invalidate cache after successful upload
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

                // Cleanup blob storage if upload failed
                try
                {
                    await blobStorageService.DeleteAsync(containerName, blobPath, cancellationToken);
                }
                catch (Exception deleteEx)
                {
                    logger.LogWarning(deleteEx, "Failed to cleanup blob {BlobPath} after upload failure", blobPath);
                }

                throw;
            }
        }
    }
}
