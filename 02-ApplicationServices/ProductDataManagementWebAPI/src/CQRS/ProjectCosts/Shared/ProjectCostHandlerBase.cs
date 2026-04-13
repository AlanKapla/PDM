using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models;
using Entities.Models.CostTrackers;
using Microsoft.AspNetCore.Http;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.Shared
{
    public abstract class ProjectCostHandlerBase
    {
        private readonly IReadRepository<CostTracker> costTrackerRepository;
        private readonly IRepository<TrackedCost> trackedCostRepository;
        private readonly IRepository<ProjectCostTrackedCostLink> projectCostLinkRepository;
        private readonly IBlobStorageService blobStorageService;
        private readonly IRepository<TrackedCostAttachment> attachmentRepository;

        protected ProjectCostHandlerBase(
            IReadRepository<CostTracker> costTrackerRepository,
            IRepository<TrackedCost> trackedCostRepository,
            IRepository<ProjectCostTrackedCostLink> projectCostLinkRepository,
            IBlobStorageService blobStorageService,
            IRepository<TrackedCostAttachment> attachmentRepository)
        {
            this.costTrackerRepository = costTrackerRepository;
            this.trackedCostRepository = trackedCostRepository;
            this.projectCostLinkRepository = projectCostLinkRepository;
            this.blobStorageService = blobStorageService;
            this.attachmentRepository = attachmentRepository;
        }

        protected async Task CreateTrackerLinkAsync(
            ProjectCost projectCost,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            ProjectCostTrackedCostLink? existingLink = await GetLinkByProjectCostAsync(projectCost.Id);

            if (existingLink != null)
            {
                return;
            }

            CostTracker tracker = await costTrackerRepository.GetFirstBySearch(
                t => t.TenantId == tenantId && t.ProjectId == projectId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostTracker), projectId.ToString());

            TrackedCost trackedCost = new TrackedCost
            {
                TrackerId = tracker.Id,
                Name = projectCost.Name,
                Description = projectCost.Description,
                Net = projectCost.NetAmount,
                Gross = projectCost.GrossAmount,
                Contractor = projectCost.Place,
                Date = projectCost.Date,
                CreatedAt = DateTime.UtcNow
            };

            await trackedCostRepository.Insert(trackedCost);

            ProjectCostTrackedCostLink link = new ProjectCostTrackedCostLink
            {
                ProjectCostId = projectCost.Id,
                TrackedCostId = trackedCost.Id,
                LinkedAt = DateTime.UtcNow
            };

            await projectCostLinkRepository.Insert(link);

            if (projectCost.HasDocument)
            {
                await CopyDocumentAsAttachmentAsync(projectCost, trackedCost, cancellationToken);
            }
        }

        protected async Task RemoveTrackerLinkAsync(Guid projectCostId, CancellationToken cancellationToken)
        {
            ProjectCostTrackedCostLink? link = await GetLinkByProjectCostAsync(projectCostId);

            if (link == null)
            {
                return;
            }

            TrackedCost? trackedCost = await GetTrackedCostByIdAsync(link.TrackedCostId);

            if (trackedCost != null)
            {
                trackedCost.IsDeleted = true;
                trackedCost.DeletedAt = DateTime.UtcNow;
                await trackedCostRepository.Update(trackedCost);

                await DeleteTrackedCostAttachmentsAsync(link.TrackedCostId, cancellationToken);
            }

            await projectCostLinkRepository.Delete(link);
        }

        protected async Task SyncTrackerCostAsync(ProjectCost projectCost, CancellationToken cancellationToken)
        {
            ProjectCostTrackedCostLink? link = await GetLinkByProjectCostAsync(projectCost.Id);

            if (link == null)
            {
                return;
            }

            TrackedCost? trackedCost = await GetTrackedCostByIdAsync(link.TrackedCostId);

            if (trackedCost == null)
            {
                return;
            }

            trackedCost.Name = projectCost.Name;
            trackedCost.Description = projectCost.Description;
            trackedCost.Net = projectCost.NetAmount;
            trackedCost.Gross = projectCost.GrossAmount;
            trackedCost.Contractor = projectCost.Place;
            trackedCost.Date = projectCost.Date;
            trackedCost.UpdatedAt = DateTime.UtcNow;

            await trackedCostRepository.Update(trackedCost);

            await SyncDocumentAttachmentAsync(projectCost, trackedCost, cancellationToken);
        }

        private async Task SyncDocumentAttachmentAsync(
            ProjectCost projectCost,
            TrackedCost trackedCost,
            CancellationToken cancellationToken)
        {
            List<TrackedCostAttachment> existingAttachments = await GetActiveAttachmentsAsync(trackedCost.Id);

            if (!projectCost.HasDocument || string.IsNullOrWhiteSpace(projectCost.DocumentBlobPath))
            {
                if (existingAttachments.Count > 0)
                {
                    await DeleteTrackedCostAttachmentsAsync(trackedCost.Id, cancellationToken);
                }

                return;
            }

            bool isInSync = existingAttachments.Count == 1 &&
                existingAttachments[0].OriginalFileName == projectCost.DocumentFileName &&
                existingAttachments[0].FileSize == (projectCost.DocumentSizeBytes ?? 0) &&
                existingAttachments[0].ContentType == (projectCost.DocumentContentType ?? "application/octet-stream");

            if (isInSync)
            {
                return;
            }

            if (existingAttachments.Count > 0)
            {
                await DeleteTrackedCostAttachmentsAsync(trackedCost.Id, cancellationToken);
            }

            await CopyDocumentAsAttachmentAsync(projectCost, trackedCost, cancellationToken);
        }

        private async Task CopyDocumentAsAttachmentAsync(
            ProjectCost projectCost,
            TrackedCost trackedCost,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(projectCost.DocumentBlobPath) || string.IsNullOrWhiteSpace(projectCost.DocumentFileName))
            {
                return;
            }

            string sourceContainer = BlobStorageSettings.GetContainerName(BlobContainerNames.ProjectCosts);
            string targetContainer = BlobStorageSettings.GetContainerName(BlobContainerNames.CostTrackers);
            string safeFileName = Path.GetFileName(projectCost.DocumentFileName);
            string targetBlobName = $"{projectCost.TenantId}/{projectCost.ProjectId}/{trackedCost.TrackerId}/{trackedCost.Id}/{safeFileName}";

            BlobDownload download = await blobStorageService.DownloadAsync(sourceContainer, projectCost.DocumentBlobPath, cancellationToken);
            await using (download.Content)
            {
                await blobStorageService.UploadAsync(
                    targetContainer,
                    targetBlobName,
                    download.Content,
                    projectCost.DocumentContentType,
                    cancellationToken);
            }

            TrackedCostAttachment attachment = new TrackedCostAttachment
            {
                TrackedCostId = trackedCost.Id,
                OriginalFileName = projectCost.DocumentFileName,
                BlobName = targetBlobName,
                ContentType = projectCost.DocumentContentType ?? "application/octet-stream",
                FileSize = projectCost.DocumentSizeBytes ?? 0,
                CreatedAt = DateTime.UtcNow
            };

            await attachmentRepository.Insert(attachment);
        }

        private async Task DeleteTrackedCostAttachmentsAsync(Guid trackedCostId, CancellationToken cancellationToken)
        {
            List<TrackedCostAttachment> attachments = await GetActiveAttachmentsAsync(trackedCostId);
            string container = BlobStorageSettings.GetContainerName(BlobContainerNames.CostTrackers);

            foreach (TrackedCostAttachment attachment in attachments)
            {
                attachment.IsDeleted = true;
                attachment.DeletedAt = DateTime.UtcNow;
                await attachmentRepository.Update(attachment);

                try
                {
                    await blobStorageService.DeleteAsync(container, attachment.BlobName, cancellationToken);
                }
                catch
                {
                    // Blob cleanup is best-effort; record is already soft-deleted
                }
            }
        }

        private async Task<ProjectCostTrackedCostLink?> GetLinkByProjectCostAsync(Guid projectCostId)
        {
            return await projectCostLinkRepository.GetFirstBySearch(l => l.ProjectCostId == projectCostId);
        }

        private async Task<TrackedCost?> GetTrackedCostByIdAsync(Guid trackedCostId)
        {
            return await trackedCostRepository.GetFirstBySearch(tc => tc.Id == trackedCostId);
        }

        private async Task<List<TrackedCostAttachment>> GetActiveAttachmentsAsync(Guid trackedCostId)
        {
            IEnumerable<TrackedCostAttachment> attachments = await attachmentRepository.GetBySearch(
                a => a.TrackedCostId == trackedCostId && !a.IsDeleted);
            return attachments.ToList();
        }

        protected async Task UploadDocumentToCostAsync(
            ProjectCost projectCost,
            IFormFile document,
            Guid tenantId,
            Guid costId,
            CancellationToken cancellationToken)
        {
            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.ProjectCosts);
            string fileExtension = Path.GetExtension(document.FileName).ToLowerInvariant();
            string blobFileName = $"{costId}{fileExtension}";
            string blobPath = $"{tenantId}/{projectCost.ProjectId}/{projectCost.UserId}/{costId}/{blobFileName}";

            using (Stream stream = document.OpenReadStream())
            {
                await blobStorageService.UploadAsync(containerName, blobPath, stream, document.ContentType, cancellationToken);
            }

            projectCost.HasDocument = true;
            projectCost.DocumentFileName = document.FileName;
            projectCost.DocumentBlobPath = blobPath;
            projectCost.DocumentContentType = document.ContentType;
            projectCost.DocumentSizeBytes = document.Length;
        }
    }
}
