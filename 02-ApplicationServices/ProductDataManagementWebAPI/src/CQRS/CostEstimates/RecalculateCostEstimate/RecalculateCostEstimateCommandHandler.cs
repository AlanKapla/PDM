using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.RecalculateCostEstimate
{
    public class RecalculateCostEstimateCommandHandler
        : IRequestHandler<RecalculateCostEstimateCommand, Unit>
    {
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly IRepository<CostEstimateGroup> groupRepository;
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly IRepository<CostEstimateItemFieldValue> itemFieldValueRepository;
        private readonly ICostEstimateCalculationService calculationService;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly ICurrentUser currentUser;

        public RecalculateCostEstimateCommandHandler(
            IRepository<CostEstimate> costEstimateRepository,
            IRepository<CostEstimateGroup> groupRepository,
            IRepository<CostEstimateItem> itemRepository,
            IRepository<CostEstimateItemFieldValue> itemFieldValueRepository,
            ICostEstimateCalculationService calculationService,
            ICostEstimateCacheService cacheService,
            ICostEstimateAccessService ceAccessService,
            ICurrentUser currentUser)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.groupRepository = groupRepository;
            this.itemRepository = itemRepository;
            this.itemFieldValueRepository = itemFieldValueRepository;
            this.calculationService = calculationService;
            this.cacheService = cacheService;
            this.ceAccessService = ceAccessService;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(RecalculateCostEstimateCommand request, CancellationToken cancellationToken)
        {
            var cachedCostEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());


            var accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            if (accessLevel == CostEstimateAccessLevel.None)
                throw new ForbiddenApiException("Only the owner or an admin or user with share can recalculate this cost estimate.");

            if (accessLevel == CostEstimateAccessLevel.ReadOnly)
                throw new ForbiddenApiException("Read-only access does not allow recalculation.");

            // Get template from cache (needed for CalculatedFieldDefinitions + SystemFieldDefinitions)
            var template = await cacheService.GetTemplateAsync(cachedCostEstimate.TemplateId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), cachedCostEstimate.TemplateId.ToString());

            // Load tracked entities for recalculation and save
            var groups = (await groupRepository.GetBySearch(
                g => g.CostEstimateId == request.CostEstimateId)).ToList();

            var items = (await itemRepository.GetBySearch(
                i => i.CostEstimateId == request.CostEstimateId)).ToList();

            var itemIds = items.Select(i => i.Id).ToHashSet();

            var fieldValues = (await itemFieldValueRepository.GetBySearch(
                fv => itemIds.Contains(fv.ItemId),
                q => q.Include(fv => fv.FieldDefinition))).ToList();

            // Assemble entity graph for calculation service
            var fieldValuesByItemId = fieldValues
                .GroupBy(fv => fv.ItemId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var itemsByGroupId = items
                .GroupBy(i => i.GroupId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var item in items)
            {
                item.FieldValues = fieldValuesByItemId.TryGetValue(item.Id, out var fvs)
                    ? fvs
                    : new List<CostEstimateItemFieldValue>();
            }

            foreach (var group in groups)
            {
                group.Items = itemsByGroupId.TryGetValue(group.Id, out var groupItems)
                    ? groupItems
                    : new List<CostEstimateItem>();
            }

            // Build a temporary CostEstimate graph for the calculation service
            var costEstimateForCalculation = await costEstimateRepository.GetFirstBySearch(
                c => c.Id == request.CostEstimateId)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            costEstimateForCalculation.Template = template;
            costEstimateForCalculation.AllGroups = groups;
            costEstimateForCalculation.AllItems = items;

            // Populate Options/Components hierarchy
            costEstimateForCalculation.PopulateItemHierarchy();

            // Run recalculation (mutates entities in-place)
            calculationService.RecalculateCostEstimate(costEstimateForCalculation);

            // All entities are already tracked by EF (loaded via GetBySearch/GetFirstBySearch).
            // Change tracking detects property mutations automatically.
            // Do NOT call Update/UpdateRange — it traverses navigation properties
            // (including _childItems set by PopulateItemHierarchy) and causes duplicate key errors.
            await costEstimateRepository.SaveChangesAsync(cancellationToken);

            // Invalidate all cache after recalculation
            await cacheService.InvalidateCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            return Unit.Value;
        }
    }
}
