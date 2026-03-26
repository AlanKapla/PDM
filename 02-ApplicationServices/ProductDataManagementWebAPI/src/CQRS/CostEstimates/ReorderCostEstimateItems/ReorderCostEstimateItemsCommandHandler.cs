using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.ReorderCostEstimateItems
{
    public class ReorderCostEstimateItemsCommandHandler : IRequestHandler<ReorderCostEstimateItemsCommand, Unit>
    {
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly ICurrentUser currentUser;

        public ReorderCostEstimateItemsCommandHandler(
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

        public async Task<Unit> Handle(ReorderCostEstimateItemsCommand request, CancellationToken cancellationToken)
        {
            var costEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());


            var accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            if (accessLevel == CostEstimateAccessLevel.None)
                throw new ForbiddenApiException("Access to this cost estimate is not allowed.");

            if (accessLevel is CostEstimateAccessLevel.Restricted or CostEstimateAccessLevel.ReadOnly)
                throw new ForbiddenApiException("Shared users cannot modify the cost estimate structure.");

            var groupsDict = await cacheService.GetGroupsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (!groupsDict.ContainsKey(request.GroupId))
            {
                throw new NotFoundApiException(nameof(CostEstimateGroup), request.GroupId.ToString());
            }

            // Validate items exist using cached dictionary
            var itemsDict = await cacheService.GetItemsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            foreach (var dto in request.Items)
            {
                if (!itemsDict.TryGetValue(dto.ItemId, out var item) || item.GroupId != request.GroupId)
                {
                    throw new NotFoundApiException(nameof(CostEstimateItem), dto.ItemId.ToString());
                }
            }

            // Load tracked entities from DB for update
            var requestedItemIds = request.Items.Select(i => i.ItemId).ToHashSet();

            var items = await itemRepository.GetBySearch(
                i => i.CostEstimateId == request.CostEstimateId &&
                     i.GroupId == request.GroupId &&
                     requestedItemIds.Contains(i.Id) &&
                     !i.IsDeleted);

            var trackedItemsById = items.ToDictionary(i => i.Id);
            var now = DateTime.UtcNow;

            foreach (var dto in request.Items)
            {
                var item = trackedItemsById[dto.ItemId];
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
