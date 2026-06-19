using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.AddCostEstimateItem
{
    public sealed class AddCostEstimateItemCommandHandler
        : IRequestHandler<AddCostEstimateItemCommand, Guid>
    {
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ICostEstimateRecalculationService recalculationService;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly ICurrentUser currentUser;

        public AddCostEstimateItemCommandHandler(
            IRepository<CostEstimateItem> itemRepository,
            ICostEstimateCacheService cacheService,
            ICostEstimateRecalculationService recalculationService,
            ICostEstimateAccessService ceAccessService,
            ICurrentUser currentUser)
        {
            this.itemRepository = itemRepository;
            this.cacheService = cacheService;
            this.recalculationService = recalculationService;
            this.ceAccessService = ceAccessService;
            this.currentUser = currentUser;
        }

        public async Task<Guid> Handle(
            AddCostEstimateItemCommand request,
            CancellationToken cancellationToken)
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

            Guid? parentPositionIdToClear = null;

            if (request.ParentItemId.HasValue)
            {
                Dictionary<Guid, CostEstimateItem> itemsDict = await cacheService.GetItemsDictionaryAsync(
                    request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

                if (!itemsDict.TryGetValue(request.ParentItemId.Value, out CostEstimateItem? parentItem))
                {
                    throw new NotFoundApiException("ParentItem", request.ParentItemId.Value.ToString());
                }

                if (request.RelationType == ItemRelationType.Option &&
                    parentItem.RelationType == ItemRelationType.Option)
                {
                    throw new ValidationApiException(
                        "Options cannot have their own Options. Maximum nesting: Position \u2192 Component \u2192 Option.");
                }

                if (request.RelationType == ItemRelationType.Option &&
                    parentItem.RelationType == ItemRelationType.None)
                {
                    bool parentHasComponents = itemsDict.Values
                        .Any(i => i.ParentItemId == parentItem.Id && i.RelationType == ItemRelationType.Component);

                    if (parentHasComponents)
                    {
                        throw new ValidationApiException(
                            "Items with Components cannot have direct Options. " +
                            "Add Options to the Components instead.");
                    }
                }

                if (request.RelationType == ItemRelationType.Component &&
                    parentItem.RelationType != ItemRelationType.None)
                {
                    throw new ValidationApiException(
                        "Only main positions (RelationType=None) can have Components. " +
                        "Components and Options cannot have their own Components.");
                }

                if (request.RelationType == ItemRelationType.Component &&
                    parentItem.RelationType == ItemRelationType.None)
                {
                    parentPositionIdToClear = parentItem.Id;
                }
            }

            CostEstimateItem item = new CostEstimateItem
            {
                CostEstimateId = costEstimate.Id,
                Name = string.Empty,
                GroupId = request.GroupId,
                ParentItemId = request.ParentItemId,
                RelationType = request.RelationType,
                Order = request.Order,
                Quantity = 1m,
                IsSelected = true,
                IsStageWork = false,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await itemRepository.Insert(item);

            if (parentPositionIdToClear.HasValue)
            {
                CostEstimateItem parentPositionToClear = await itemRepository.GetFirstBySearch(
                    i => i.Id == parentPositionIdToClear.Value
                        && i.CostEstimateId == request.CostEstimateId)
                    ?? throw new NotFoundApiException(
                        nameof(CostEstimateItem),
                        parentPositionIdToClear.Value.ToString());

                ClearParentFinancialInputFields(parentPositionToClear);
                parentPositionToClear.UpdatedAt = DateTime.UtcNow;
            }

            await itemRepository.SaveChangesAsync(cancellationToken);

            await cacheService.InvalidateItemsAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (parentPositionIdToClear.HasValue)
            {
                await recalculationService.RecalculateAsync(
                    request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);
            }

            return item.Id;
        }

        private static void ClearParentFinancialInputFields(CostEstimateItem item)
        {
            item.Quantity = null;
            item.Unit = null;
            item.UnitPriceNet = null;
            item.UnitPriceGross = null;
            item.VatRate = null;
        }
    }
}



