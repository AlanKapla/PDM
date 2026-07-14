using Entities.Models.Costs;

namespace Business.Interfaces.Services
{
    public interface IAICostImportBlobService
    {
        Task<string> UploadPendingAsync(
            Guid tenantId,
            Guid projectId,
            Guid itemId,
            Stream content,
            string fileName,
            string contentType,
            CancellationToken cancellationToken);

        Task<BlobDownload> DownloadPendingAsync(
            string blobPath,
            CancellationToken cancellationToken);

        Task DeletePendingAsync(
            string blobPath,
            CancellationToken cancellationToken);

        string GeneratePendingPreviewUrl(
            string blobPath,
            string fileName);

        Task<BaseCostAttachment> MoveToCostAttachmentAsync(
            BaseCost cost,
            string pendingBlobPath,
            string originalFileName,
            string contentType,
            long fileSizeBytes,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken);
    }
}
