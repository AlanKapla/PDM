using Business.Implementation.CacheKeys;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public class CostEstimateCacheService : ICostEstimateCacheService
    {
        private readonly ICacheService cacheService;
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly IReadRepository<CostEstimateGroup> groupRepository;
        private readonly IReadRepository<CostEstimateItem> itemRepository;
        private readonly ILogger<CostEstimateCacheService> logger;

        public CostEstimateCacheService(
            ICacheService cacheService,
            IReadRepository<CostEstimate> costEstimateRepository,
            IReadRepository<CostEstimateGroup> groupRepository,
            IReadRepository<CostEstimateItem> itemRepository,
            ILogger<CostEstimateCacheService> logger)
        {
            this.cacheService = cacheService;
            this.costEstimateRepository = costEstimateRepository;
            this.groupRepository = groupRepository;
            this.itemRepository = itemRepository;
            this.logger = logger;
        }

        public async Task<CostEstimate?> GetCostEstimateAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            CostEstimate? costEstimate = await cacheService.GetOrAddAsync(
                CostEstimateCacheKeys.CostEstimate(tenantId, projectId, costEstimateId),
                async () =>
                {
                    CostEstimate? ce = await costEstimateRepository.GetFirstBySearch(
                        c => c.Id == costEstimateId &&
                             c.TenantId == tenantId &&
                             c.ProjectId == projectId &&
                             !c.IsDeleted,
                        q => q.Include(c => c.Owner));
                    return ce!;
                },
                CostEstimateCacheKeys.Ttl,
                cancellationToken);

            return costEstimate;
        }

        public async Task<Dictionary<Guid, CostEstimateGroup>> GetGroupsDictionaryAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            return await cacheService.GetOrAddAsync(
                CostEstimateCacheKeys.Groups(tenantId, projectId, costEstimateId),
                async () =>
                {
                    IEnumerable<CostEstimateGroup> groups = await groupRepository.GetBySearch(
                        g => g.CostEstimateId == costEstimateId && !g.IsDeleted,
                        q => q.Include(g => g.AdditionalFieldValues));
                    return groups.ToDictionary(g => g.Id);
                },
                CostEstimateCacheKeys.Ttl,
                cancellationToken) ?? new Dictionary<Guid, CostEstimateGroup>();
        }

        public async Task<Dictionary<Guid, CostEstimateItem>> GetItemsDictionaryAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            return await cacheService.GetOrAddAsync(
                CostEstimateCacheKeys.Items(tenantId, projectId, costEstimateId),
                async () =>
                {
                    IEnumerable<CostEstimateItem> items = await itemRepository.GetBySearch(
                        i => i.CostEstimateId == costEstimateId && !i.IsDeleted,
                        q => q.Include(i => i.AdditionalFieldValues),
                        q => q.Include(i => i.Files.Where(f => !f.IsDeleted)));
                    return items.ToDictionary(i => i.Id);
                },
                CostEstimateCacheKeys.Ttl,
                cancellationToken) ?? new Dictionary<Guid, CostEstimateItem>();
        }

        public async Task InvalidateCostEstimateAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            await Task.WhenAll(
                cacheService.RemoveCacheByKeyAsync(CostEstimateCacheKeys.CostEstimate(tenantId, projectId, costEstimateId), cancellationToken),
                cacheService.RemoveCacheByKeyAsync(CostEstimateCacheKeys.Groups(tenantId, projectId, costEstimateId), cancellationToken),
                cacheService.RemoveCacheByKeyAsync(CostEstimateCacheKeys.Items(tenantId, projectId, costEstimateId), cancellationToken));

            logger.LogDebug("Invalidated all cache for cost estimate {CostEstimateId}", costEstimateId);
        }

        public async Task InvalidateGroupsAsync(Guid costEstimateId, Guid tenantId, Guid projectId, CancellationToken cancellationToken)
        {
            await cacheService.RemoveCacheByKeyAsync(CostEstimateCacheKeys.Groups(tenantId, projectId, costEstimateId), cancellationToken);
        }

        public async Task InvalidateItemsAsync(Guid costEstimateId, Guid tenantId, Guid projectId, CancellationToken cancellationToken)
        {
            await cacheService.RemoveCacheByKeyAsync(CostEstimateCacheKeys.Items(tenantId, projectId, costEstimateId), cancellationToken);
        }
    }
}
