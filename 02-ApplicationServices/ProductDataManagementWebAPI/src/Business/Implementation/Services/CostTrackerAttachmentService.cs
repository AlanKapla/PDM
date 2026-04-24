using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Entities.Models.CostTrackers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public sealed class CostTrackerAttachmentService : ICostTrackerAttachmentService
    {
        private readonly IBlobStorageService blobStorageService;
        private readonly IRepository<TrackedCostAttachment> attachmentRepository;
        private readonly ILogger<CostTrackerAttachmentService> logger;

        private static readonly string ContainerName =
            BlobStorageSettings.GetContainerName(BlobContainerNames.CostTrackers);

        public CostTrackerAttachmentService(
            IBlobStorageService blobStorageService,
            IRepository<TrackedCostAttachment> attachmentRepository,
            ILogger<CostTrackerAttachmentService> logger)
        {
            this.blobStorageService = blobStorageService;
            this.attachmentRepository = attachmentRepository;
            this.logger = logger;
        }

        public async Task<List<TrackedCostAttachment>> SyncAttachmentsAsync(
            TrackedCost cost,
            IReadOnlyList<IFormFile>? newFiles,
            IReadOnlyList<Guid>? existingAttachmentIds,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            var currentAttachments = (await attachmentRepository.GetBySearch(
                a => a.TrackedCostId == cost.Id)).ToList();

            if (existingAttachmentIds is not null)
            {
                var toDelete = currentAttachments
                    .Where(a => !existingAttachmentIds.Contains(a.Id))
                    .ToList();

                foreach (var attachment in toDelete)
                {
                    attachment.IsDeleted = true;
                    attachment.DeletedAt = DateTime.UtcNow;
                    await attachmentRepository.Update(attachment);

                    try
                    {
                        await blobStorageService.DeleteAsync(ContainerName, attachment.BlobName, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex,
                            "Failed to delete blob {BlobName} for attachment {AttachmentId}. Record soft-deleted, blob may remain.",
                            attachment.BlobName, attachment.Id);
                    }
                }
            }

            var now = DateTime.UtcNow;
            var created = new List<TrackedCostAttachment>();

            foreach (var file in newFiles ?? [])
            {
                var blobName = BuildBlobName(cost, tenantId, projectId, file.FileName);

                await using var stream = file.OpenReadStream();
                await blobStorageService.UploadAsync(ContainerName, blobName, stream, file.ContentType, cancellationToken);

                var attachment = new TrackedCostAttachment
                {
                    TrackedCostId = cost.Id,
                    OriginalFileName = file.FileName,
                    BlobName = blobName,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    CreatedAt = now
                };

                await attachmentRepository.Insert(attachment);
                created.Add(attachment);
            }

            var retained = existingAttachmentIds is not null
                ? currentAttachments.Where(a => existingAttachmentIds.Contains(a.Id)).ToList()
                : currentAttachments.ToList();

            retained.AddRange(created);
            return retained;
        }

        public string GenerateFileUrl(TrackedCostAttachment attachment)
        {
            return blobStorageService
                .GenerateSasUri(ContainerName, attachment.BlobName, attachment.OriginalFileName)
                .ToString();
        }

        private static string BuildBlobName(TrackedCost cost, Guid tenantId, Guid projectId, string fileName)
        {
            var safeFileName = Path.GetFileName(fileName);
            return $"{tenantId}/{projectId}/{cost.Id}/{safeFileName}";
        }
    }
}
