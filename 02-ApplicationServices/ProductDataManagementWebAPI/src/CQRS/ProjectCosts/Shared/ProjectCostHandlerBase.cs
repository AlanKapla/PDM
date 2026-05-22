using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Entities.Models.Costs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.Shared
{
    public abstract class ProjectCostHandlerBase
    {
        private readonly IBlobStorageService blobStorageService;
        private readonly IRepository<BaseCostAttachment> attachmentRepository;
        private readonly ILogger<ProjectCostHandlerBase> logger;

        private static readonly string ContainerName =
            BlobStorageSettings.GetContainerName(BlobContainerNames.CostTrackers);

        protected ProjectCostHandlerBase(
            IBlobStorageService blobStorageService,
            IRepository<BaseCostAttachment> attachmentRepository,
            ILogger<ProjectCostHandlerBase> logger)
        {
            this.blobStorageService = blobStorageService;
            this.attachmentRepository = attachmentRepository;
            this.logger = logger;
        }

        protected async Task<BaseCostAttachment> UploadDocumentToCostAsync(
            ProjectCost projectCost,
            IFormFile document,
            CancellationToken cancellationToken)
        {
            BaseCostAttachment attachment = await UploadBlobAndBuildAttachmentAsync(projectCost, document, cancellationToken);

            await attachmentRepository.Insert(attachment);
            await attachmentRepository.SaveChangesAsync(cancellationToken);

            return attachment;
        }

        /// <summary>
        /// Uploads the document to blob storage and returns an in-memory <see cref="BaseCostAttachment"/>
        /// without persisting it to the database. Use when DB writes must occur in a specific order
        /// (e.g. after the parent <see cref="ProjectCost"/> insert).
        /// </summary>
        protected async Task<BaseCostAttachment> UploadBlobAndBuildAttachmentAsync(
            ProjectCost projectCost,
            IFormFile document,
            CancellationToken cancellationToken)
        {
            string fileExtension = Path.GetExtension(document.FileName).ToLowerInvariant();
            string blobName = $"{projectCost.TenantId}/{projectCost.ProjectId}/{projectCost.Id}/{Guid.NewGuid()}{fileExtension}";

            using (Stream stream = document.OpenReadStream())
            {
                await blobStorageService.UploadAsync(ContainerName, blobName, stream, document.ContentType, cancellationToken);
            }

            return new BaseCostAttachment
            {
                CostId = projectCost.Id,
                TenantId = projectCost.TenantId,
                ProjectId = projectCost.ProjectId,
                OriginalFileName = document.FileName,
                BlobName = blobName,
                ContentType = document.ContentType,
                FileSize = document.Length,
                CreatedAt = DateTime.UtcNow
            };
        }

        protected async Task PersistAttachmentAsync(
            BaseCostAttachment attachment,
            CancellationToken cancellationToken)
        {
            await attachmentRepository.Insert(attachment);
            await attachmentRepository.SaveChangesAsync(cancellationToken);
        }

        protected async Task RemoveAttachmentsAsync(
            Guid costId,
            CancellationToken cancellationToken)
        {
            List<BaseCostAttachment> attachments = (await attachmentRepository.GetBySearch(
                a => a.CostId == costId)).ToList();

            foreach (BaseCostAttachment attachment in attachments)
            {
                try
                {
                    await blobStorageService.DeleteAsync(ContainerName, attachment.BlobName, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Blob deletion failure is non-fatal; the attachment row is soft-deleted regardless,
                    // but we still want visibility into orphaned blobs.
                    logger.LogWarning(ex,
                        "Failed to delete blob {BlobName} for cost attachment {AttachmentId}",
                        attachment.BlobName, attachment.Id);
                }

                attachment.IsDeleted = true;
                attachment.DeletedAt = DateTime.UtcNow;
                await attachmentRepository.Update(attachment);
            }

            if (attachments.Count > 0)
            {
                await attachmentRepository.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
