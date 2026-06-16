using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
using Entities.Models.WorkSchedules;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.DeleteCostEstimateGroup
{
    public sealed class DeleteCostEstimateGroupCommandHandler : IRequestHandler<DeleteCostEstimateGroupCommand, Unit>
    {
        private readonly IRepository<CostEstimateGroup> groupRepository;
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly IRepository<CostEstimateItemFile> itemFileRepository;
        private readonly IRepository<WorkScheduleStage> stageRepository;
        private readonly IRepository<WorkScheduleStageWork> stageWorkRepository;
        private readonly IRepository<TrackedCost> trackedCostRepository;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<DeleteCostEstimateGroupCommandHandler> logger;

        public DeleteCostEstimateGroupCommandHandler(
            IRepository<CostEstimateGroup> groupRepository,
            IRepository<CostEstimateItem> itemRepository,
            IRepository<CostEstimateItemFile> itemFileRepository,
            IRepository<WorkScheduleStage> stageRepository,
            IRepository<WorkScheduleStageWork> stageWorkRepository,
            IRepository<TrackedCost> trackedCostRepository,
            ICostEstimateCacheService cacheService,
            ICostEstimateAccessService ceAccessService,
            ICurrentUser currentUser,
            ILogger<DeleteCostEstimateGroupCommandHandler> logger)
        {
            this.groupRepository = groupRepository;
            this.itemRepository = itemRepository;
            this.itemFileRepository = itemFileRepository;
            this.stageRepository = stageRepository;
            this.stageWorkRepository = stageWorkRepository;
            this.trackedCostRepository = trackedCostRepository;
            this.cacheService = cacheService;
            this.ceAccessService = ceAccessService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(DeleteCostEstimateGroupCommand request, CancellationToken cancellationToken)
        {
            CostEstimate costEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            CostEstimateAccessLevel accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            accessLevel.EnsureCanModifyStructure();

            Dictionary<Guid, CostEstimateGroup> groupsDict = await cacheService.GetGroupsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (!groupsDict.ContainsKey(request.GroupId))
            {
                throw new NotFoundApiException(nameof(CostEstimateGroup), request.GroupId.ToString());
            }

            DateTime now = DateTime.UtcNow;

            HashSet<Guid> allGroupIds = CollectDescendantGroupIds(groupsDict, request.GroupId);
            allGroupIds.Add(request.GroupId);

            Dictionary<Guid, CostEstimateItem> itemsDict = await cacheService.GetItemsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            HashSet<Guid> allItemIds = itemsDict.Values
                .Where(i => allGroupIds.Contains(i.GroupId))
                .Select(i => i.Id)
                .ToHashSet();

            await SoftDeleteItemFilesAsync(request.CostEstimateId, allItemIds, now, cancellationToken);
            await SoftDeleteItemsAsync(allItemIds, now, cancellationToken);
            await SoftDeleteGroupsAsync(allGroupIds, now, cancellationToken);
            await NullifyWorkScheduleReferencesAsync(allGroupIds, allItemIds, cancellationToken);

            await cacheService.InvalidateCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            return Unit.Value;
        }

        private async Task SoftDeleteItemFilesAsync(
            Guid costEstimateId,
            HashSet<Guid> allItemIds,
            DateTime now,
            CancellationToken cancellationToken)
        {
            if (allItemIds.Count == 0)
            {
                return;
            }

            List<CostEstimateItemFile> filesToDelete = (await itemFileRepository.GetBySearch(
                f => f.CostEstimateId == costEstimateId && allItemIds.Contains(f.ItemId) && !f.IsDeleted)).ToList();

            if (filesToDelete.Count == 0)
            {
                return;
            }

            HashSet<Guid> fileIds = filesToDelete.Select(f => f.Id).ToHashSet();
            await itemFileRepository.ExecuteUpdateAsync(
                f => fileIds.Contains(f.Id),
                f => f.SetProperty(p => p.IsDeleted, true)
                      .SetProperty(p => p.DeletedAt, now),
                cancellationToken);

            logger.LogInformation(
                "Soft-deleted {FileCount} files for deleted groups in cost estimate {CostEstimateId}",
                filesToDelete.Count, costEstimateId);
        }

        private async Task SoftDeleteItemsAsync(HashSet<Guid> allItemIds, DateTime now, CancellationToken cancellationToken)
        {
            if (allItemIds.Count == 0)
            {
                return;
            }

            List<CostEstimateItem> itemsToDelete = (await itemRepository.GetBySearch(
                i => allItemIds.Contains(i.Id))).ToList();

            foreach (CostEstimateItem item in itemsToDelete)
            {
                item.IsDeleted = true;
                item.DeletedAt = now;
            }

            await itemRepository.UpdateRange(itemsToDelete);
        }

        private async Task SoftDeleteGroupsAsync(HashSet<Guid> allGroupIds, DateTime now, CancellationToken cancellationToken)
        {
            List<CostEstimateGroup> groupsToDelete = (await groupRepository.GetBySearch(
                g => allGroupIds.Contains(g.Id))).ToList();

            foreach (CostEstimateGroup g in groupsToDelete)
            {
                g.IsDeleted = true;
                g.DeletedAt = now;
            }

            await groupRepository.UpdateRange(groupsToDelete);
            await groupRepository.SaveChangesAsync(cancellationToken);
        }

        private async Task NullifyWorkScheduleReferencesAsync(
            HashSet<Guid> allGroupIds,
            HashSet<Guid> allItemIds,
            CancellationToken cancellationToken)
        {
            await stageRepository.ExecuteUpdateAsync(
                s => allGroupIds.Contains(s.CostEstimateGroupId!.Value),
                s => s.SetProperty(st => st.CostEstimateGroupId, (Guid?)null),
                cancellationToken);

            if (allItemIds.Count == 0)
            {
                return;
            }

            await stageWorkRepository.ExecuteUpdateAsync(
                w => allItemIds.Contains(w.CostEstimateItemId!.Value),
                s => s.SetProperty(w => w.CostEstimateItemId, (Guid?)null),
                cancellationToken);

            await trackedCostRepository.ExecuteUpdateAsync(
                tc => allItemIds.Contains(tc.CostEstimateItemId!.Value),
                s => s.SetProperty(tc => tc.CostEstimateItemId, (Guid?)null),
                cancellationToken);
        }

        private static HashSet<Guid> CollectDescendantGroupIds(
            Dictionary<Guid, CostEstimateGroup> groupsDict,
            Guid rootGroupId)
        {
            Dictionary<Guid, List<Guid>> childrenByParentId = groupsDict.Values
                .Where(g => g.ParentGroupId.HasValue)
                .GroupBy(g => g.ParentGroupId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

            HashSet<Guid> result = [];
            Queue<Guid> queue = new();

            if (!childrenByParentId.TryGetValue(rootGroupId, out List<Guid>? directChildren))
            {
                return result;
            }

            foreach (Guid id in directChildren)
            {
                queue.Enqueue(id);
            }

            while (queue.Count > 0)
            {
                Guid id = queue.Dequeue();
                result.Add(id);
                if (childrenByParentId.TryGetValue(id, out List<Guid>? children))
                {
                    foreach (Guid childId in children)
                    {
                        queue.Enqueue(childId);
                    }
                }
            }

            return result;
        }
    }
}
