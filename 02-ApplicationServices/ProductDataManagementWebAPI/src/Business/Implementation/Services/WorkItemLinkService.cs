using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models;
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
using Entities.Models.WorkItemLinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public sealed class WorkItemLinkService : IWorkItemLinkService
    {
        private readonly IRepository<CostEstimateWorkScheduleLink> workScheduleLinkRepository;
        private readonly IRepository<CostEstimateGroupWorkScheduleStageLink> groupStageLinkRepository;
        private readonly IRepository<CostEstimateItemWorkScheduleStageWorkLink> workItemLinkRepository;
        private readonly IRepository<TrackedCost> trackedCostRepository;
        private readonly IRepository<TrackedCostAttachment> trackedCostAttachmentRepository;
        private readonly IBlobStorageService blobStorageService;
        private readonly IReadRepository<CostEstimateItem> itemRepository;
        private readonly IReadRepository<WorkScheduleStageWork> stageWorkRepository;
        private readonly IReadRepository<WorkScheduleStage> stageRepository;
        private readonly ILogger<WorkItemLinkService> logger;

        private static readonly string TrackedCostContainerName =
            BlobStorageSettings.GetContainerName(BlobContainerNames.CostTrackers);

        public WorkItemLinkService(
            IRepository<CostEstimateWorkScheduleLink> workScheduleLinkRepository,
            IRepository<CostEstimateGroupWorkScheduleStageLink> groupStageLinkRepository,
            IRepository<CostEstimateItemWorkScheduleStageWorkLink> workItemLinkRepository,
            IRepository<TrackedCost> trackedCostRepository,
            IRepository<TrackedCostAttachment> trackedCostAttachmentRepository,
            IBlobStorageService blobStorageService,
            IReadRepository<CostEstimateItem> itemRepository,
            IReadRepository<WorkScheduleStageWork> stageWorkRepository,
            IReadRepository<WorkScheduleStage> stageRepository,
            ILogger<WorkItemLinkService> logger)
        {
            this.workScheduleLinkRepository = workScheduleLinkRepository;
            this.groupStageLinkRepository = groupStageLinkRepository;
            this.workItemLinkRepository = workItemLinkRepository;
            this.trackedCostRepository = trackedCostRepository;
            this.trackedCostAttachmentRepository = trackedCostAttachmentRepository;
            this.blobStorageService = blobStorageService;
            this.itemRepository = itemRepository;
            this.stageWorkRepository = stageWorkRepository;
            this.stageRepository = stageRepository;
            this.logger = logger;
        }

        public async Task<CostEstimateWorkScheduleLink?> GetWorkScheduleLinkAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken)
        {
            return await workScheduleLinkRepository.GetFirstBySearch(
                l => l.WorkScheduleId == workScheduleId);
        }

        public async Task<IReadOnlyList<CostEstimateGroupWorkScheduleStageLink>> GetGroupStageLinksForWorkScheduleLinkAsync(
            Guid workScheduleLinkId,
            CancellationToken cancellationToken)
        {
            return (await groupStageLinkRepository.GetBySearch(
                l => l.WorkScheduleLinkId == workScheduleLinkId
                     && l.CostEstimateGroupId != null
                     && l.WorkScheduleStageId != null))
                .ToList();
        }

        public async Task<CostEstimateWorkScheduleLink> CreateWorkScheduleLinkAsync(
            Guid workScheduleId,
            Guid? costEstimateId,
            CancellationToken cancellationToken)
        {
            CostEstimateWorkScheduleLink link = new CostEstimateWorkScheduleLink
            {
                CostEstimateId = costEstimateId,
                WorkScheduleId = workScheduleId
            };

            await workScheduleLinkRepository.Insert(link);
            await workScheduleLinkRepository.SaveChangesAsync(cancellationToken);
            return link;
        }

        public async Task CreateGroupStageLinkForScheduleStageAsync(
            Guid workScheduleId,
            Guid stageId,
            Guid? costEstimateGroupId,
            CancellationToken cancellationToken)
        {
            CostEstimateWorkScheduleLink? workScheduleLink = await workScheduleLinkRepository
                .GetFirstBySearch(l => l.WorkScheduleId == workScheduleId);

            if (workScheduleLink == null)
            {
                return;
            }

            CostEstimateGroupWorkScheduleStageLink groupStageLink = new CostEstimateGroupWorkScheduleStageLink
            {
                WorkScheduleLinkId = workScheduleLink.Id,
                CostEstimateGroupId = costEstimateGroupId,
                WorkScheduleStageId = stageId
            };

            await groupStageLinkRepository.Insert(groupStageLink);
            await groupStageLinkRepository.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteWorkItemLinkForWorkAsync(
            Guid workScheduleStageWorkId,
            CancellationToken cancellationToken)
        {
            List<Guid> linkIds = (await workItemLinkRepository.SelectAsync(
                l => l.WorkScheduleStageWorkId == workScheduleStageWorkId,
                l => l.Id,
                cancellationToken)).ToList();

            await SoftDeleteTrackedCostsForWorkItemLinksAsync(linkIds, cancellationToken);

            await workItemLinkRepository.ExecuteDeleteAsync(
                l => l.WorkScheduleStageWorkId == workScheduleStageWorkId,
                cancellationToken);
        }

        public async Task DeleteWorkItemLinksForWorksAsync(
            IReadOnlyCollection<Guid> workIds,
            CancellationToken cancellationToken)
        {
            if (workIds.Count == 0)
                return;

            List<Guid> linkIds = (await workItemLinkRepository.SelectAsync(
                l => l.WorkScheduleStageWorkId.HasValue && workIds.Contains(l.WorkScheduleStageWorkId.Value),
                l => l.Id,
                cancellationToken)).ToList();

            await SoftDeleteTrackedCostsForWorkItemLinksAsync(linkIds, cancellationToken);

            await workItemLinkRepository.ExecuteDeleteAsync(
                l => l.WorkScheduleStageWorkId.HasValue && workIds.Contains(l.WorkScheduleStageWorkId.Value),
                cancellationToken);
        }

        public async Task DeleteWorkItemLinksForItemsAsync(
            IReadOnlyCollection<Guid> costEstimateItemIds,
            CancellationToken cancellationToken)
        {
            if (costEstimateItemIds.Count == 0)
                return;

            List<Guid> linkIds = (await workItemLinkRepository.SelectAsync(
                l => l.CostEstimateItemId.HasValue && costEstimateItemIds.Contains(l.CostEstimateItemId.Value),
                l => l.Id,
                cancellationToken)).ToList();

            await SoftDeleteTrackedCostsForWorkItemLinksAsync(linkIds, cancellationToken);

            await workItemLinkRepository.ExecuteDeleteAsync(
                l => l.CostEstimateItemId.HasValue && costEstimateItemIds.Contains(l.CostEstimateItemId.Value),
                cancellationToken);
        }

        public async Task DeleteGroupStageLinksForStagesAsync(
            IReadOnlyCollection<Guid> stageIds,
            CancellationToken cancellationToken)
        {
            if (stageIds.Count == 0)
                return;

            await groupStageLinkRepository.ExecuteDeleteAsync(
                l => l.WorkScheduleStageId.HasValue && stageIds.Contains(l.WorkScheduleStageId.Value),
                cancellationToken);
        }

        public async Task DeleteGroupStageLinksForGroupsAsync(
            IReadOnlyCollection<Guid> costEstimateGroupIds,
            CancellationToken cancellationToken)
        {
            if (costEstimateGroupIds.Count == 0)
                return;

            await groupStageLinkRepository.ExecuteDeleteAsync(
                l => l.CostEstimateGroupId.HasValue && costEstimateGroupIds.Contains(l.CostEstimateGroupId.Value),
                cancellationToken);
        }

        public async Task DeleteAllLinksForScheduleAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken)
        {
            List<Guid> stageIds = (await stageRepository
                .SelectAsync(s => s.WorkScheduleId == workScheduleId, s => s.Id, cancellationToken))
                .ToList();

            if (stageIds.Count > 0)
            {
                List<Guid> workIds = (await stageWorkRepository
                    .SelectAsync(w => stageIds.Contains(w.WorkScheduleStageId), w => w.Id, cancellationToken))
                    .ToList();

                if (workIds.Count > 0)
                {
                    List<Guid> linkIds = (await workItemLinkRepository.SelectAsync(
                        l => l.WorkScheduleStageWorkId.HasValue && workIds.Contains(l.WorkScheduleStageWorkId.Value),
                        l => l.Id,
                        cancellationToken)).ToList();

                    await SoftDeleteTrackedCostsForWorkItemLinksAsync(linkIds, cancellationToken);

                    await workItemLinkRepository.ExecuteDeleteAsync(
                        l => l.WorkScheduleStageWorkId.HasValue && workIds.Contains(l.WorkScheduleStageWorkId.Value),
                        cancellationToken);
                }

                await groupStageLinkRepository.ExecuteDeleteAsync(
                    l => l.WorkScheduleStageId.HasValue && stageIds.Contains(l.WorkScheduleStageId.Value),
                    cancellationToken);
            }

            await workScheduleLinkRepository.ExecuteDeleteAsync(
                l => l.WorkScheduleId == workScheduleId,
                cancellationToken);
        }

        public async Task DeleteAllLinksForEstimateAsync(
            Guid costEstimateId,
            CancellationToken cancellationToken)
        {
            CostEstimateWorkScheduleLink? scheduleLink = await workScheduleLinkRepository
                .GetFirstBySearch(l => l.CostEstimateId == costEstimateId);

            if (scheduleLink == null)
                return;

            List<CostEstimateGroupWorkScheduleStageLink> groupStageLinks = (await groupStageLinkRepository
                .GetBySearch(l => l.WorkScheduleLinkId == scheduleLink.Id))
                .ToList();

            if (groupStageLinks.Count > 0)
            {
                List<Guid> groupStageLinkIds = groupStageLinks.Select(l => l.Id).ToList();

                List<Guid> linkIds = (await workItemLinkRepository.SelectAsync(
                    l => l.GroupStageLinkId.HasValue && groupStageLinkIds.Contains(l.GroupStageLinkId.Value),
                    l => l.Id,
                    cancellationToken)).ToList();

                await SoftDeleteTrackedCostsForWorkItemLinksAsync(linkIds, cancellationToken);

                await workItemLinkRepository.ExecuteDeleteAsync(
                    l => l.GroupStageLinkId.HasValue && groupStageLinkIds.Contains(l.GroupStageLinkId.Value),
                    cancellationToken);

                await groupStageLinkRepository.ExecuteDeleteAsync(
                    l => l.WorkScheduleLinkId == scheduleLink.Id,
                    cancellationToken);
            }

            await workScheduleLinkRepository.ExecuteDeleteAsync(
                l => l.CostEstimateId == costEstimateId,
                cancellationToken);
        }

        public async Task SyncPlannedDatesForStageWorkAsync(
            Guid workScheduleStageWorkId,
            DateTime? plannedStart,
            DateTime? plannedEnd,
            bool isWorkClosed,
            CancellationToken cancellationToken)
        {
            CostEstimateItemWorkScheduleStageWorkLink? link = await workItemLinkRepository.GetFirstBySearch(
                l => l.WorkScheduleStageWorkId == workScheduleStageWorkId);

            if (link == null)
                return;

            link.PlannedStart = plannedStart;
            link.PlannedEnd = plannedEnd;
            link.IsWorkClosed = isWorkClosed;
            await workItemLinkRepository.Update(link);
            await workItemLinkRepository.SaveChangesAsync(cancellationToken);
        }

        public async Task SyncWorkItemLinkAsync(
            Guid? workItemLinkId,
            Guid? costEstimateItemId,
            Guid? workScheduleStageWorkId,
            CancellationToken cancellationToken)
        {
            CostEstimateItemWorkScheduleStageWorkLink link = await ResolveWorkItemLinkAsync(
                workItemLinkId, costEstimateItemId, workScheduleStageWorkId, cancellationToken);

            bool updated = false;

            if (link.CostEstimateItemId.HasValue)
            {
                CostEstimateItem? item = await itemRepository.GetFirstBySearch(
                    i => i.Id == link.CostEstimateItemId.Value && !i.IsDeleted);

                if (item != null)
                {
                    link.BudgetNet = item.NetValue;
                    link.BudgetGross = item.GrossValue;

                    if (link.WorkScheduleStageWorkId == null)
                    {
                        link.DisplayName = item.Name;
                    }

                    updated = true;
                }
            }

            if (link.WorkScheduleStageWorkId.HasValue)
            {
                WorkScheduleStageWork? work = await stageWorkRepository.GetFirstBySearch(
                    w => w.Id == link.WorkScheduleStageWorkId.Value);

                if (work != null)
                {
                    link.PlannedStart = work.PlannedStartDate;
                    link.PlannedEnd = work.PlannedEndDate;

                    if (link.CostEstimateItemId == null)
                    {
                        link.DisplayName = work.Name;
                    }

                    updated = true;
                }
            }

            if (updated)
            {
                await workItemLinkRepository.Update(link);
                await workItemLinkRepository.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task UpsertWorkItemLinkAsync(
            Guid projectId,
            Guid groupStageLinkId,
            Guid costEstimateItemId,
            Guid workScheduleStageWorkId,
            string displayName,
            decimal? budgetNet,
            decimal? budgetGross,
            int order,
            CancellationToken cancellationToken)
        {
            CostEstimateItemWorkScheduleStageWorkLink? existing = await workItemLinkRepository.GetFirstBySearch(
                l => l.WorkScheduleStageWorkId == workScheduleStageWorkId);

            if (existing != null)
            {
                existing.GroupStageLinkId = groupStageLinkId;
                existing.CostEstimateItemId = costEstimateItemId;
                existing.DisplayName = displayName;
                existing.BudgetNet = budgetNet;
                existing.BudgetGross = budgetGross;
                existing.Order = order;
                await workItemLinkRepository.Update(existing);
            }
            else
            {
                CostEstimateItemWorkScheduleStageWorkLink link = new CostEstimateItemWorkScheduleStageWorkLink
                {
                    ProjectId = projectId,
                    GroupStageLinkId = groupStageLinkId,
                    CostEstimateItemId = costEstimateItemId,
                    WorkScheduleStageWorkId = workScheduleStageWorkId,
                    DisplayName = displayName,
                    BudgetNet = budgetNet,
                    BudgetGross = budgetGross,
                    Order = order
                };
                await workItemLinkRepository.Insert(link);
            }

            await workItemLinkRepository.SaveChangesAsync(cancellationToken);
        }

        private async Task<CostEstimateItemWorkScheduleStageWorkLink> ResolveWorkItemLinkAsync(
            Guid? workItemLinkId,
            Guid? costEstimateItemId,
            Guid? workScheduleStageWorkId,
            CancellationToken cancellationToken)
        {
            if (workItemLinkId.HasValue)
            {
                return await workItemLinkRepository.GetFirstBySearch(
                    l => l.Id == workItemLinkId.Value)
                    ?? throw new NotFoundApiException(nameof(CostEstimateItemWorkScheduleStageWorkLink), workItemLinkId.Value.ToString());
            }

            if (costEstimateItemId.HasValue)
            {
                return await workItemLinkRepository.GetFirstBySearch(
                    l => l.CostEstimateItemId == costEstimateItemId.Value)
                    ?? throw new NotFoundApiException(nameof(CostEstimateItemWorkScheduleStageWorkLink), costEstimateItemId.Value.ToString());
            }

            if (workScheduleStageWorkId.HasValue)
            {
                return await workItemLinkRepository.GetFirstBySearch(
                    l => l.WorkScheduleStageWorkId == workScheduleStageWorkId.Value)
                    ?? throw new NotFoundApiException(nameof(CostEstimateItemWorkScheduleStageWorkLink), workScheduleStageWorkId.Value.ToString());
            }

            throw new ValidationApiException(
                "At least one of WorkItemLinkId, CostEstimateItemId or WorkScheduleStageWorkId must be provided.");
        }

        private async Task SoftDeleteTrackedCostsForWorkItemLinksAsync(
            IReadOnlyCollection<Guid> workItemLinkIds,
            CancellationToken cancellationToken)
        {
            if (workItemLinkIds.Count == 0)
                return;

            List<TrackedCost> trackedCosts = (await trackedCostRepository.GetBySearch(
                c => c.WorkItemLinkId.HasValue
                     && workItemLinkIds.Contains(c.WorkItemLinkId.Value)
                     && !c.IsDeleted))
                .ToList();

            if (trackedCosts.Count == 0)
                return;

            List<Guid> costIds = trackedCosts.Select(c => c.Id).ToList();
            List<TrackedCostAttachment> attachments = (await trackedCostAttachmentRepository.GetBySearch(
                a => costIds.Contains(a.TrackedCostId) && !a.IsDeleted))
                .ToList();

            DateTime now = DateTime.UtcNow;

            foreach (TrackedCostAttachment attachment in attachments)
            {
                attachment.IsDeleted = true;
                attachment.DeletedAt = now;
                await trackedCostAttachmentRepository.Update(attachment);

                try
                {
                    await blobStorageService.DeleteAsync(TrackedCostContainerName, attachment.BlobName, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "Failed to delete blob {BlobName} for attachment {AttachmentId} during work item link cleanup",
                        attachment.BlobName, attachment.Id);
                }
            }

            foreach (TrackedCost cost in trackedCosts)
            {
                cost.IsDeleted = true;
                cost.DeletedAt = now;
                await trackedCostRepository.Update(cost);
            }

            await trackedCostRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Soft-deleted {CostCount} TrackedCosts and {AttachmentCount} attachments for {LinkCount} deleted work item links",
                trackedCosts.Count, attachments.Count, workItemLinkIds.Count);
        }
    }
}
