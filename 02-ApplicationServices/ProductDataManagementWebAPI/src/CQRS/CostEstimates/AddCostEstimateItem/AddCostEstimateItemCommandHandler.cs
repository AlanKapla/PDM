using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.AddCostEstimateItem
{
    public class AddCostEstimateItemCommandHandler
        : IRequestHandler<AddCostEstimateItemCommand, Guid>
    {
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ICurrentUser currentUser;

        public AddCostEstimateItemCommandHandler(
            IRepository<CostEstimateItem> itemRepository,
            ICostEstimateCacheService cacheService,
            ICurrentUser currentUser)
        {
            this.itemRepository = itemRepository;
            this.cacheService = cacheService;
            this.currentUser = currentUser;
        }

        public async Task<Guid> Handle(
            AddCostEstimateItemCommand request,
            CancellationToken cancellationToken)
        {
            var costEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, currentUser.Id, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            var groupsDict = await cacheService.GetGroupsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (!groupsDict.ContainsKey(request.GroupId))
            {
                throw new NotFoundApiException(nameof(CostEstimateGroup), request.GroupId.ToString());
            }

            if (request.ParentItemId.HasValue)
            {
                var itemsDict = await cacheService.GetItemsDictionaryAsync(
                    request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

                if (!itemsDict.TryGetValue(request.ParentItemId.Value, out var parentItem))
                {
                    throw new NotFoundApiException("ParentItem", request.ParentItemId.Value.ToString());
                }

                if (request.RelationType == ItemRelationType.Option &&
                    parentItem.RelationType == ItemRelationType.Option)
                {
                    throw new ValidationApiException(
                        "Options cannot have their own Options. Maximum nesting: Position \u2192 Component \u2192 Option.");
                }

                if (request.RelationType == ItemRelationType.Component &&
                    parentItem.RelationType != ItemRelationType.None)
                {
                    throw new ValidationApiException(
                        "Only main positions (RelationType=None) can have Components. " +
                        "Components and Options cannot have their own Components.");
                }
            }

            // Validate template exists
            _ = await cacheService.GetTemplateAsync(costEstimate.TemplateId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), costEstimate.TemplateId.ToString());

            var item = new CostEstimateItem
            {
                CostEstimateId = costEstimate.Id,
                GroupId = request.GroupId,
                ParentItemId = request.ParentItemId,
                RelationType = request.RelationType,
                Order = request.Order,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await itemRepository.Insert(item);

            await cacheService.InvalidateItemsAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            return item.Id;
        }
    }
}
