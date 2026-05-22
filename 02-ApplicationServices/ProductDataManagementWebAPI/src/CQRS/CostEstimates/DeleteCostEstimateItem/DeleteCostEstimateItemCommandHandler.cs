using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
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
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.DeleteCostEstimateItem
{
    public sealed class DeleteCostEstimateItemCommandHandler : IRequestHandler<DeleteCostEstimateItemCommand, Unit>
    {
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly IRepository<CostEstimateItemFieldValue> itemFieldValueRepository;
        private readonly IRepository<CostEstimateFieldFile> fieldFileRepository;
        private readonly IRepository<WorkScheduleStageWork> stageWorkRepository;
        private readonly IRepository<TrackedCost> trackedCostRepository;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<DeleteCostEstimateItemCommandHandler> logger;

        public DeleteCostEstimateItemCommandHandler(
            IRepository<CostEstimateItem> itemRepository,
            IRepository<CostEstimateItemFieldValue> itemFieldValueRepository,
            IRepository<CostEstimateFieldFile> fieldFileRepository,
            IRepository<WorkScheduleStageWork> stageWorkRepository,
            IRepository<TrackedCost> trackedCostRepository,
            ICostEstimateCacheService cacheService,
            ICostEstimateAccessService ceAccessService,
            ICurrentUser currentUser,
            ILogger<DeleteCostEstimateItemCommandHandler> logger)
        {
            this.itemRepository = itemRepository;
            this.itemFieldValueRepository = itemFieldValueRepository;
            this.fieldFileRepository = fieldFileRepository;
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

            // Collect all descendant item IDs from cached dictionary
            HashSet<Guid> allItemIds = CollectDescendantItemIds(itemsDict, request.ItemId);
            allItemIds.Add(request.ItemId);

            // Soft-delete files + delete blobs
            List<CostEstimateFieldFile> filesToDelete = (await fieldFileRepository.GetBySearch(
                f => f.CostEstimateId == request.CostEstimateId &&
                     allItemIds.Contains(f.FieldValue.ItemId))).ToList();

            if (filesToDelete.Count > 0)
            {
                foreach (CostEstimateFieldFile file in filesToDelete)
                {
                    file.IsDeleted = true;
                    file.DeletedAt = now;
                }

                await fieldFileRepository.UpdateRange(filesToDelete);

                logger.LogInformation(
                    "Soft-deleted {FileCount} files for deleted items in cost estimate {CostEstimateId}",
                    filesToDelete.Count, request.CostEstimateId);
            }

            // Delete item field values (hard delete — no IsDeleted column)
            await itemFieldValueRepository.ExecuteDeleteAsync(
                fv => allItemIds.Contains(fv.ItemId), cancellationToken);

            // Soft-delete items
            List<CostEstimateItem> itemsToDelete = (await itemRepository.GetBySearch(
                i => allItemIds.Contains(i.Id))).ToList();

            foreach (CostEstimateItem item in itemsToDelete)
            {
                item.IsDeleted = true;
                item.DeletedAt = now;
            }

            await itemRepository.UpdateRange(itemsToDelete);
            await itemRepository.SaveChangesAsync(cancellationToken);

            await stageWorkRepository.ExecuteUpdateAsync(
                w => allItemIds.Contains(w.CostEstimateItemId!.Value),
                s => s.SetProperty(w => w.CostEstimateItemId, (Guid?)null),
                cancellationToken);

            await trackedCostRepository.ExecuteUpdateAsync(
                tc => allItemIds.Contains(tc.CostEstimateItemId!.Value),
                s => s.SetProperty(tc => tc.CostEstimateItemId, (Guid?)null),
                cancellationToken);

            // Invalidate cache
            await cacheService.InvalidateCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            return Unit.Value;
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
