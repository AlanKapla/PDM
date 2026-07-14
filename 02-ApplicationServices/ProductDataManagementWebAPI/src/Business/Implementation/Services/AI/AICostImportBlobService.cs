using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Entities.Models.Costs;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services.AI
{
    public sealed class AICostImportBlobService : IAICostImportBlobService
    {
        private readonly IBlobStorageService blobStorageService;
        private readonly IRepository<BaseCostAttachment> attachmentRepository;
        private readonly ILogger<AICostImportBlobService> logger;

        private static readonly string PendingContainerName =
            BlobStorageSettings.GetContainerName(BlobContainerNames.AICostImport);

        private static readonly string CostContainerName =
            BlobStorageSettings.GetContainerName(BlobContainerNames.CostTrackers);

        public AICostImportBlobService(
            IBlobStorageService blobStorageService,
            IRepository<BaseCostAttachment> attachmentRepository,
            ILogger<AICostImportBlobService> logger)
        {
            this.blobStorageService = blobStorageService;
            this.attachmentRepository = attachmentRepository;
            this.logger = logger;
        }

        public async Task<string> UploadPendingAsync(
            Guid tenantId,
            Guid projectId,
            Guid itemId,
            Stream content,
            string fileName,
            string contentType,
            CancellationToken cancellationToken)
        {
            string blobPath = BuildPendingBlobPath(tenantId, projectId, itemId, fileName);
            await blobStorageService.UploadAsync(
                PendingContainerName, blobPath, content, contentType, cancellationToken);
            return blobPath;
        }

        public Task<BlobDownload> DownloadPendingAsync(
            string blobPath,
            CancellationToken cancellationToken)
        {
            return blobStorageService.DownloadAsync(PendingContainerName, blobPath, cancellationToken);
        }

        public Task DeletePendingAsync(
            string blobPath,
            CancellationToken cancellationToken)
        {
            return blobStorageService.DeleteAsync(PendingContainerName, blobPath, cancellationToken);
        }

        public string GeneratePendingPreviewUrl(string blobPath, string fileName)
        {
            return blobStorageService
                .GenerateSasUri(PendingContainerName, blobPath, fileName)
                .ToString();
        }

        public async Task<BaseCostAttachment> MoveToCostAttachmentAsync(
            BaseCost cost,
            string pendingBlobPath,
            string originalFileName,
            string contentType,
            long fileSizeBytes,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            string costBlobName = BuildCostBlobPath(tenantId, projectId, cost.Id, originalFileName);

            BlobDownload download = await blobStorageService.DownloadAsync(
                PendingContainerName, pendingBlobPath, cancellationToken);

            await using (download.Content)
            {
                await blobStorageService.UploadAsync(
                    CostContainerName, costBlobName, download.Content, contentType, cancellationToken);
            }

            try
            {
                await blobStorageService.DeleteAsync(PendingContainerName, pendingBlobPath, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to delete pending blob {BlobPath} after move to cost {CostId}",
                    pendingBlobPath, cost.Id);
            }

            DateTime now = DateTime.UtcNow;
            BaseCostAttachment attachment = new BaseCostAttachment
            {
                CostId = cost.Id,
                TenantId = tenantId,
                ProjectId = projectId,
                OriginalFileName = originalFileName,
                BlobName = costBlobName,
                ContentType = contentType,
                FileSize = fileSizeBytes,
                CreatedAt = now
            };

            await attachmentRepository.Insert(attachment);
            return attachment;
        }

        private static string BuildPendingBlobPath(
            Guid tenantId, Guid projectId, Guid itemId, string fileName)
        {
            string safeFileName = Path.GetFileName(fileName);
            return $"{tenantId}/{projectId}/{itemId}/{safeFileName}";
        }

        private static string BuildCostBlobPath(
            Guid tenantId, Guid projectId, Guid costId, string fileName)
        {
            string safeFileName = Path.GetFileName(fileName);
            return $"{tenantId}/{projectId}/{costId}/{safeFileName}";
        }
    }
}
