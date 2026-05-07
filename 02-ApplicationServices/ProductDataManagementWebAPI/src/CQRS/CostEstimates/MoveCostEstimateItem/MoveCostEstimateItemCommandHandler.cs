using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.MoveCostEstimateItem
{
    public class MoveCostEstimateItemCommandHandler
        : IRequestHandler<MoveCostEstimateItemCommand, Unit>
    {
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly ICurrentUser currentUser;

        public MoveCostEstimateItemCommandHandler(
            IRepository<CostEstimateItem> itemRepository,
            ICostEstimateCacheService cacheService,
            ICostEstimateAccessService ceAccessService,
            ICurrentUser currentUser)
        {
            this.itemRepository = itemRepository;
            this.cacheService = cacheService;
            this.ceAccessService = ceAccessService;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(MoveCostEstimateItemCommand request, CancellationToken cancellationToken)
        {
            var costEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());


            var accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            if (accessLevel == CostEstimateAccessLevel.None)
                throw new ForbiddenApiException("Access to this cost estimate is not allowed.");

            if (accessLevel == CostEstimateAccessLevel.Restricted)
                throw new ForbiddenApiException("Shared users cannot modify the cost estimate structure.");

            if (accessLevel == CostEstimateAccessLevel.ReadOnly)
                throw new ForbiddenApiException("Read-only access does not allow modifying the cost estimate structure.");

            // Validate item exists
            var itemsDict = await cacheService.GetItemsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (!itemsDict.TryGetValue(request.ItemId, out var cachedItem))
            {
                throw new NotFoundApiException(nameof(CostEstimateItem), request.ItemId.ToString());
            }

            // Only main items (RelationType.None) can be moved between groups
            if (cachedItem.RelationType != ItemRelationType.None)
            {
                throw new ValidationApiException(
                    "Only main items (RelationType=None) can be moved between groups. " +
                    "Options and Components are bound to their parent item.");
            }

            // Validate target group exists
            var groupsDict = await cacheService.GetGroupsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (!groupsDict.ContainsKey(request.TargetGroupId))
            {
                throw new NotFoundApiException(nameof(CostEstimateGroup), request.TargetGroupId.ToString());
            }

            // No-op if already in target group
            if (cachedItem.GroupId == request.TargetGroupId)
            {
                return Unit.Value;
            }

            // Collect IDs to move (main item + direct children from cache)
            var itemIdsToMove = itemsDict.Values
                .Where(i => i.ParentItemId == request.ItemId)
                .Select(i => i.Id)
                .ToHashSet();

            itemIdsToMove.Add(request.ItemId);

            // Calculate last position in target group
            int lastOrder = itemsDict.Values
                .Where(i => i.GroupId == request.TargetGroupId &&
                            i.RelationType == ItemRelationType.None)
                .Select(i => i.Order)
                .DefaultIfEmpty(-1)
                .Max() + 1;

            // Load tracked entities in a single query
            var trackedItems = (await itemRepository.GetBySearch(
                i => itemIdsToMove.Contains(i.Id))).ToList();

            var now = DateTime.UtcNow;

            foreach (var trackedItem in trackedItems)
            {
                trackedItem.GroupId = request.TargetGroupId;
                trackedItem.UpdatedAt = now;
            }

            // Set the moved item's order as last position
            var movedItem = trackedItems.First(i => i.Id == request.ItemId);
            movedItem.Order = lastOrder;

            await itemRepository.UpdateRange(trackedItems);
            await itemRepository.SaveChangesAsync(cancellationToken);

            // Invalidate items cache
            await cacheService.InvalidateItemsAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            return Unit.Value;
        }
    }
}
