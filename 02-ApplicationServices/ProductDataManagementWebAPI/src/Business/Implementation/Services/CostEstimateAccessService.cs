using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public sealed class CostEstimateAccessService : ICostEstimateAccessService
    {
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);

        private readonly ICacheService cacheService;
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly IReadRepository<SharedCostEstimate> sharedCeRepository;
        private readonly ILogger<CostEstimateAccessService> logger;

        public CostEstimateAccessService(
            ICacheService cacheService,
            IReadRepository<CostEstimate> costEstimateRepository,
            IReadRepository<SharedCostEstimate> sharedCeRepository,
            ILogger<CostEstimateAccessService> logger)
        {
            this.cacheService = cacheService;
            this.costEstimateRepository = costEstimateRepository;
            this.sharedCeRepository = sharedCeRepository;
            this.logger = logger;
        }

        public async Task<HashSet<Guid>> GetAccessibleCostEstimateIdsAsync(
            ICurrentUser currentUser,
            Guid tenantId,
            Guid projectId,
            ResourceScope scope,
            CancellationToken cancellationToken = default)
        {
            string cacheKey = $"ce:access:{tenantId}:{projectId}:ids:{currentUser.Id}:{scope}";

            HashSet<Guid>? result = await cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    logger.LogDebug(
                        "Loading accessible cost estimate IDs for user {UserId}, project {ProjectId}, scope {Scope}",
                        currentUser.Id, projectId, scope);

                    return scope switch
                    {
                        ResourceScope.All => (await costEstimateRepository.GetIdsBySearchAsync(
                            ce => ce.ProjectId == projectId && ce.TenantId == tenantId && !ce.IsDeleted,
                            cancellationToken)).ToHashSet(),

                        ResourceScope.Mine => (await costEstimateRepository.GetIdsBySearchAsync(
                            ce => ce.ProjectId == projectId && ce.TenantId == tenantId
                                  && ce.OwnerId == currentUser.Id && !ce.IsDeleted,
                            cancellationToken)).ToHashSet(),

                        ResourceScope.Shared => await sharedCeRepository.SelectToHashSetAsync(
                            s => s.ProjectId == projectId && s.TenantId == tenantId
                                 && s.SharedWithUserId == currentUser.Id,
                            s => s.CostEstimateId,
                            cancellationToken),

                        _ => new HashSet<Guid>()
                    };
                },
                CacheExpiration,
                cancellationToken);

            return result ?? [];
        }

        public async Task<CostEstimateAccessLevel> GetAccessLevelAsync(
            ICurrentUser currentUser,
            Guid tenantId,
            Guid projectId,
            Guid costEstimateId,
            CancellationToken cancellationToken = default)
        {
            string cacheKey = $"ce:access:{tenantId}:{projectId}:level:{currentUser.Id}:{costEstimateId}";

            IntWrapper? result = await cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    logger.LogDebug(
                        "Resolving access level for user {UserId}, cost estimate {CostEstimateId}",
                        currentUser.Id, costEstimateId);

                    // TenantAdmin and ProjectAdmin always get full access.
                    // For SuperAdmin this covers the case where they are also a TenantAdmin
                    // or have been explicitly assigned the ProjectAdmin role.
                    bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(tenantId, projectId, cancellationToken);
                    if (isAdmin)
                    {
                        return new IntWrapper((int)CostEstimateAccessLevel.Full);
                    }

                    var ce = await costEstimateRepository.GetFirstBySearch(
                        c => c.Id == costEstimateId && c.TenantId == tenantId
                             && c.ProjectId == projectId && !c.IsDeleted,
                        cancellationToken: cancellationToken);

                    if (ce == null)
                    {
                        return new IntWrapper((int)CostEstimateAccessLevel.None);
                    }

                    if (ce.OwnerId == currentUser.Id)
                    {
                        return new IntWrapper((int)CostEstimateAccessLevel.Full);
                    }

                    bool isShared = await sharedCeRepository.AnyAsync(
                        s => s.CostEstimateId == costEstimateId
                             && s.SharedWithUserId == currentUser.Id,
                        cancellationToken);

                    if (isShared)
                    {
                        return new IntWrapper((int)CostEstimateAccessLevel.Restricted);
                    }

                    // SuperAdmin always has fallback read-only permissions in every project,
                    // regardless of whether they hold a project membership.
                    // A SuperAdmin who is a ProjectViewer or has no membership at all can
                    // still read the cost estimate but must not be able to modify it.
                    if (currentUser.IsSuperAdmin)
                    {
                        return new IntWrapper((int)CostEstimateAccessLevel.ReadOnly);
                    }

                    return new IntWrapper((int)CostEstimateAccessLevel.None);
                },
                CacheExpiration,
                cancellationToken);

            return result != null
                ? (CostEstimateAccessLevel)result.Value
                : CostEstimateAccessLevel.None;
        }

        public async Task<List<Guid>> GetSharedWithUserIdsAsync(
            Guid tenantId,
            Guid projectId,
            Guid costEstimateId,
            CancellationToken cancellationToken = default)
        {
            string cacheKey = $"ce:access:{tenantId}:{projectId}:shares:{costEstimateId}";

            GuidListWrapper? result = await cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    logger.LogDebug(
                        "Loading shared user IDs for cost estimate {CostEstimateId}",
                        costEstimateId);

                    IEnumerable<SharedCostEstimate> shares = await sharedCeRepository.GetBySearch(
                        s => s.CostEstimateId == costEstimateId);

                    return new GuidListWrapper(shares.Select(s => s.SharedWithUserId).ToList());
                },
                CacheExpiration,
                cancellationToken);

            return result?.Values ?? [];
        }

        public async Task InvalidateAccessCacheAsync(
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            await cacheService.RemoveCacheContainsAsync(
                $"ce:access:{tenantId}:{projectId}:ids:*",
                cancellationToken);

            logger.LogDebug("Invalidated cost estimate access IDs cache for project {ProjectId}", projectId);
        }

        public async Task InvalidateCostEstimateAccessCacheAsync(
            Guid tenantId,
            Guid projectId,
            Guid costEstimateId,
            CancellationToken cancellationToken = default)
        {
            await cacheService.RemoveCacheContainsAsync(
                $"ce:access:{tenantId}:{projectId}:level:*:{costEstimateId}",
                cancellationToken);

            await cacheService.RemoveCacheByKeyAsync(
                $"ce:access:{tenantId}:{projectId}:shares:{costEstimateId}",
                cancellationToken);

            logger.LogDebug(
                "Invalidated cost estimate access cache for cost estimate {CostEstimateId}",
                costEstimateId);
        }

        private sealed class IntWrapper
        {
            public int Value { get; }
            public IntWrapper(int value) => Value = value;
        }

        private sealed class GuidListWrapper
        {
            public List<Guid> Values { get; }
            public GuidListWrapper(List<Guid> values) => Values = values;
        }
    }
}
