using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Entities.Models.Costs;
using Microsoft.AspNetCore.Http;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.Shared
{
    public abstract class ProjectCostHandlerBase
    {
        private readonly IBlobStorageService blobStorageService;
        private readonly IRepository<BaseCostAttachment> attachmentRepository;

        private static readonly string ContainerName =
            BlobStorageSettings.GetContainerName(BlobContainerNames.CostTrackers);

        protected ProjectCostHandlerBase(
            IBlobStorageService blobStorageService,
            IRepository<BaseCostAttachment> attachmentRepository)
        {
            this.blobStorageService = blobStorageService;
            this.attachmentRepository = attachmentRepository;
        }

        protected async Task<BaseCostAttachment> UploadDocumentToCostAsync(
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

            BaseCostAttachment attachment = new BaseCostAttachment
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

            await attachmentRepository.Insert(attachment);
            await attachmentRepository.SaveChangesAsync(cancellationToken);

            return attachment;
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
                catch
                {
                    // blob deletion failure is non-fatal; record is soft-deleted regardless
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
