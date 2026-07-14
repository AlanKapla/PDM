using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimates;
using Entities.Models.CostEstimates;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.ReorderCostEstimateItemChildren
{
    public sealed class ReorderCostEstimateItemChildrenCommandHandler : IRequestHandler<ReorderCostEstimateItemChildrenCommand, Unit>
    {
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly ICurrentUser currentUser;

        public ReorderCostEstimateItemChildrenCommandHandler(
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

        public async Task<Unit> Handle(ReorderCostEstimateItemChildrenCommand request, CancellationToken cancellationToken)
        {
            CostEstimate costEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());


            CostEstimateAccessLevel accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            accessLevel.EnsureCanModifyStructure();

            // Validate parent item exists and belongs to the cost estimate
            Dictionary<Guid, CostEstimateItem> itemsDict = await cacheService.GetItemsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (!itemsDict.TryGetValue(request.ParentItemId, out CostEstimateItem? parentItem) || parentItem.CostEstimateId != request.CostEstimateId)
            {
                throw new NotFoundApiException(nameof(CostEstimateItem), request.ParentItemId.ToString());
            }

            // Validate all requested child items exist and belong to the parent
            foreach (ReorderItemChildDto dto in request.Items)
            {
                if (!itemsDict.TryGetValue(dto.ItemId, out CostEstimateItem? item) || item.ParentItemId != request.ParentItemId)
                {
                    throw new NotFoundApiException(nameof(CostEstimateItem), dto.ItemId.ToString());
                }
            }

            // Load tracked entities from DB for update
            HashSet<Guid> requestedItemIds = request.Items.Select(i => i.ItemId).ToHashSet();

            IEnumerable<CostEstimateItem> items = await itemRepository.GetBySearch(
                i => i.CostEstimateId == request.CostEstimateId &&
                     i.ParentItemId == request.ParentItemId &&
                     requestedItemIds.Contains(i.Id));

            Dictionary<Guid, CostEstimateItem> trackedItemsById = items.ToDictionary(i => i.Id);
            DateTime now = DateTime.UtcNow;

            foreach (ReorderItemChildDto dto in request.Items)
            {
                CostEstimateItem item = trackedItemsById[dto.ItemId];
                item.Order = dto.Order;
                item.UpdatedAt = now;
            }

            await itemRepository.UpdateRange(items);
            await itemRepository.SaveChangesAsync(cancellationToken);

            // Invalidate cache
            await cacheService.InvalidateItemsAsync(request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            return Unit.Value;
        }
    }
}
