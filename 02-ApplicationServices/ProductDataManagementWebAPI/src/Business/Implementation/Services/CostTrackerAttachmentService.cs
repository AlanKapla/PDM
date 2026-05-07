using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Entities.Models.Costs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public sealed class CostTrackerAttachmentService : ICostTrackerAttachmentService
    {
        private readonly IBlobStorageService blobStorageService;
        private readonly IRepository<BaseCostAttachment> attachmentRepository;
        private readonly ILogger<CostTrackerAttachmentService> logger;

        private static readonly string ContainerName =
            BlobStorageSettings.GetContainerName(BlobContainerNames.CostTrackers);

        public CostTrackerAttachmentService(
            IBlobStorageService blobStorageService,
            IRepository<BaseCostAttachment> attachmentRepository,
            ILogger<CostTrackerAttachmentService> logger)
        {
            this.blobStorageService = blobStorageService;
            this.attachmentRepository = attachmentRepository;
            this.logger = logger;
        }

        public async Task<List<BaseCostAttachment>> SyncAttachmentsAsync(
            BaseCost cost,
            IReadOnlyList<IFormFile>? newFiles,
            IReadOnlyList<Guid>? existingAttachmentIds,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            List<BaseCostAttachment> currentAttachments = (await attachmentRepository.GetBySearch(
                a => a.CostId == cost.Id)).ToList();

            if (existingAttachmentIds is not null)
            {
                List<BaseCostAttachment> toDelete = currentAttachments
                    .Where(a => !existingAttachmentIds.Contains(a.Id))
                    .ToList();

                foreach (BaseCostAttachment attachment in toDelete)
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

            DateTime now = DateTime.UtcNow;
            List<BaseCostAttachment> created = new List<BaseCostAttachment>();

            foreach (IFormFile file in newFiles ?? [])
            {
                string blobName = BuildBlobName(cost, tenantId, projectId, file.FileName);

                await using Stream stream = file.OpenReadStream();
                await blobStorageService.UploadAsync(ContainerName, blobName, stream, file.ContentType, cancellationToken);

                BaseCostAttachment attachment = new BaseCostAttachment
                {
                    CostId = cost.Id,
                    TenantId = tenantId,
                    ProjectId = projectId,
                    OriginalFileName = file.FileName,
                    BlobName = blobName,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    CreatedAt = now
                };

                await attachmentRepository.Insert(attachment);
                created.Add(attachment);
            }

            List<BaseCostAttachment> retained = existingAttachmentIds is not null
                ? currentAttachments.Where(a => existingAttachmentIds.Contains(a.Id)).ToList()
                : currentAttachments.ToList();

            retained.AddRange(created);
            return retained;
        }

        public string GenerateFileUrl(BaseCostAttachment attachment)
        {
            return blobStorageService
                .GenerateSasUri(ContainerName, attachment.BlobName, attachment.OriginalFileName)
                .ToString();
        }

        private static string BuildBlobName(BaseCost cost, Guid tenantId, Guid projectId, string fileName)
        {
            string safeFileName = Path.GetFileName(fileName);
            return $"{tenantId}/{projectId}/{cost.Id}/{safeFileName}";
        }
    }
}
