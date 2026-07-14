using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.RecalculateCostEstimate
{
    public sealed class RecalculateCostEstimateCommandHandler
        : IRequestHandler<RecalculateCostEstimateCommand, Unit>
    {
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly IRepository<CostEstimateGroup> groupRepository;
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly ICostEstimateCalculationService calculationService;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly ICurrentUser currentUser;

        public RecalculateCostEstimateCommandHandler(
            IRepository<CostEstimate> costEstimateRepository,
            IRepository<CostEstimateGroup> groupRepository,
            IRepository<CostEstimateItem> itemRepository,
            ICostEstimateCalculationService calculationService,
            ICostEstimateCacheService cacheService,
            ICostEstimateAccessService ceAccessService,
            ICurrentUser currentUser)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.groupRepository = groupRepository;
            this.itemRepository = itemRepository;
            this.calculationService = calculationService;
            this.cacheService = cacheService;
            this.ceAccessService = ceAccessService;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(RecalculateCostEstimateCommand request, CancellationToken cancellationToken)
        {
            CostEstimate cachedCostEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            CostEstimateAccessLevel accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            if (accessLevel == CostEstimateAccessLevel.None)
            {
                throw new ForbiddenApiException("Only the owner or an admin or user with share can recalculate this cost estimate.");
            }

            if (accessLevel == CostEstimateAccessLevel.ReadOnly)
            {
                throw new ForbiddenApiException("Read-only access does not allow recalculation.");
            }

            List<CostEstimateGroup> groups = (await groupRepository.GetBySearch(
                g => g.CostEstimateId == request.CostEstimateId)).ToList();

            List<CostEstimateItem> items = (await itemRepository.GetBySearch(
                i => i.CostEstimateId == request.CostEstimateId)).ToList();

            Dictionary<Guid, List<CostEstimateItem>> itemsByGroupId = items
                .GroupBy(i => i.GroupId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (CostEstimateGroup group in groups)
            {
                group.Items = itemsByGroupId.TryGetValue(group.Id, out List<CostEstimateItem>? groupItems)
                    ? groupItems
                    : new List<CostEstimateItem>();
            }

            CostEstimate costEstimateForCalculation = await costEstimateRepository.GetFirstBySearch(
                c => c.Id == request.CostEstimateId)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            costEstimateForCalculation.AllGroups = groups;
            costEstimateForCalculation.AllItems = items;

            costEstimateForCalculation.PopulateItemHierarchy();

            calculationService.RecalculateCostEstimate(costEstimateForCalculation);

            await costEstimateRepository.SaveChangesAsync(cancellationToken);

            await cacheService.InvalidateCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            return Unit.Value;
        }
    }
}
