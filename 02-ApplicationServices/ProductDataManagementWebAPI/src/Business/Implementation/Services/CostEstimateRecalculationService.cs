using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public sealed class CostEstimateRecalculationService : ICostEstimateRecalculationService
    {
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly IRepository<CostEstimateGroup> groupRepository;
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly ICostEstimateCalculationService calculationService;
        private readonly ICostEstimateCacheService cacheService;

        public CostEstimateRecalculationService(
            IRepository<CostEstimate> costEstimateRepository,
            IRepository<CostEstimateGroup> groupRepository,
            IRepository<CostEstimateItem> itemRepository,
            ICostEstimateCalculationService calculationService,
            ICostEstimateCacheService cacheService)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.groupRepository = groupRepository;
            this.itemRepository = itemRepository;
            this.calculationService = calculationService;
            this.cacheService = cacheService;
        }

        public async Task RecalculateAsync(
            Guid tenantId,
            Guid projectId,
            Guid costEstimateId,
            CancellationToken cancellationToken = default)
        {
            List<CostEstimateGroup> groups = (await groupRepository.GetBySearch(
                g => g.CostEstimateId == costEstimateId)).ToList();

            List<CostEstimateItem> items = (await itemRepository.GetBySearch(
                i => i.CostEstimateId == costEstimateId)).ToList();

            Dictionary<Guid, List<CostEstimateItem>> itemsByGroupId = items
                .GroupBy(i => i.GroupId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (CostEstimateGroup group in groups)
            {
                group.Items = itemsByGroupId.TryGetValue(group.Id, out List<CostEstimateItem>? groupItems)
                    ? groupItems
                    : new List<CostEstimateItem>();
            }

            CostEstimate costEstimate = await costEstimateRepository.GetFirstBySearch(
                c => c.Id == costEstimateId)
                ?? throw new NotFoundApiException(nameof(CostEstimate), costEstimateId.ToString());

            costEstimate.AllGroups = groups;
            costEstimate.AllItems = items;

            costEstimate.PopulateItemHierarchy();

            calculationService.RecalculateCostEstimate(costEstimate);

            await costEstimateRepository.SaveChangesAsync(cancellationToken);

            await cacheService.InvalidateCostEstimateAsync(
                costEstimateId, tenantId, projectId, cancellationToken);
        }
    }
}
