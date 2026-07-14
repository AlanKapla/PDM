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

namespace CQRS.CostEstimates.DeleteCostEstimateItem
{
    public sealed class DeleteCostEstimateItemCommandHandler : IRequestHandler<DeleteCostEstimateItemCommand, Unit>
    {
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly IRepository<CostEstimateItemFile> itemFileRepository;
        private readonly IRepository<WorkScheduleStageWork> stageWorkRepository;
        private readonly IRepository<TrackedCost> trackedCostRepository;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<DeleteCostEstimateItemCommandHandler> logger;

        public DeleteCostEstimateItemCommandHandler(
            IRepository<CostEstimateItem> itemRepository,
            IRepository<CostEstimateItemFile> itemFileRepository,
            IRepository<WorkScheduleStageWork> stageWorkRepository,
            IRepository<TrackedCost> trackedCostRepository,
            ICostEstimateCacheService cacheService,
            ICostEstimateAccessService ceAccessService,
            ICurrentUser currentUser,
            ILogger<DeleteCostEstimateItemCommandHandler> logger)
        {
            this.itemRepository = itemRepository;
            this.itemFileRepository = itemFileRepository;
            this.stageWorkRepository = stageWorkRepository;
            this.trackedCostRepository = trackedCostRepository;
            this.cacheService = cacheService;
            this.ceAccessService = ceAccessService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(DeleteCostEstimateItemCommand request, CancellationToken cancellationToken)
        {
            CostEstimate costEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            CostEstimateAccessLevel accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            accessLevel.EnsureCanModifyStructure();

            Dictionary<Guid, CostEstimateItem> itemsDict = await cacheService.GetItemsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (!itemsDict.ContainsKey(request.ItemId))
            {
                throw new NotFoundApiException(nameof(CostEstimateItem), request.ItemId.ToString());
            }

            DateTime now = DateTime.UtcNow;

            HashSet<Guid> allItemIds = CollectDescendantItemIds(itemsDict, request.ItemId);
            allItemIds.Add(request.ItemId);

            await SoftDeleteItemFilesAsync(request.CostEstimateId, allItemIds, now, cancellationToken);
            await SoftDeleteItemsAsync(allItemIds, now, cancellationToken);
            await NullifyWorkScheduleReferencesAsync(allItemIds, cancellationToken);

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
            List<CostEstimateItemFile> filesToDelete = (await itemFileRepository.GetBySearch(
                f => f.CostEstimateId == costEstimateId && allItemIds.Contains(f.ItemId) && !f.IsDeleted)).ToList();

            if (filesToDelete.Count == 0)
            {
                return;
            }

            foreach (CostEstimateItemFile file in filesToDelete)
            {
                file.IsDeleted = true;
                file.DeletedAt = now;
            }

            await itemFileRepository.UpdateRange(filesToDelete);

            logger.LogInformation(
                "Soft-deleted {FileCount} files for deleted items in cost estimate {CostEstimateId}",
                filesToDelete.Count, costEstimateId);
        }

        private async Task SoftDeleteItemsAsync(HashSet<Guid> allItemIds, DateTime now, CancellationToken cancellationToken)
        {
            List<CostEstimateItem> itemsToDelete = (await itemRepository.GetBySearch(
                i => allItemIds.Contains(i.Id))).ToList();

            foreach (CostEstimateItem item in itemsToDelete)
            {
                item.IsDeleted = true;
                item.DeletedAt = now;
            }

            await itemRepository.UpdateRange(itemsToDelete);
            await itemRepository.SaveChangesAsync(cancellationToken);
        }

        private async Task NullifyWorkScheduleReferencesAsync(
            HashSet<Guid> allItemIds,
            CancellationToken cancellationToken)
        {
            await stageWorkRepository.ExecuteUpdateAsync(
                w => allItemIds.Contains(w.CostEstimateItemId!.Value),
                s => s.SetProperty(w => w.CostEstimateItemId, (Guid?)null),
                cancellationToken);

            await trackedCostRepository.ExecuteUpdateAsync(
                tc => allItemIds.Contains(tc.CostEstimateItemId!.Value),
                s => s.SetProperty(tc => tc.CostEstimateItemId, (Guid?)null),
                cancellationToken);
        }

        private static HashSet<Guid> CollectDescendantItemIds(
            Dictionary<Guid, CostEstimateItem> itemsDict,
            Guid parentItemId)
        {
            HashSet<Guid> result = new HashSet<Guid>();

            foreach (KeyValuePair<Guid, CostEstimateItem> kvp in itemsDict)
            {
                if (kvp.Value.ParentItemId == parentItemId)
                {
                    result.Add(kvp.Key);
                    result.UnionWith(CollectDescendantItemIds(itemsDict, kvp.Key));
                }
            }

            return result;
        }
    }
}
