using Business.Interfaces.Constants;
using Business.Interfaces.Model;

namespace Business.Interfaces.Services
{
    public interface ICostEstimateAccessService
    {
        /// <summary>
        /// Returns IDs of cost estimates visible to the user per ResourceScope.
        /// Cache key: ce:access:{tenantId}:{projectId}:ids:{userId}:{scope}  TTL: 15 min
        /// </summary>
        Task<HashSet<Guid>> GetAccessibleCostEstimateIdsAsync(
            ICurrentUser currentUser,
            Guid tenantId,
            Guid projectId,
            ResourceScope scope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the access level of the user for a specific cost estimate.
        /// Cache key: ce:access:{tenantId}:{projectId}:level:{userId}:{costEstimateId}  TTL: 15 min
        /// </summary>
        Task<CostEstimateAccessLevel> GetAccessLevelAsync(
            ICurrentUser currentUser,
            Guid tenantId,
            Guid projectId,
            Guid costEstimateId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns user IDs that have been granted access to a specific cost estimate.
        /// Cache key: ce:access:{tenantId}:{projectId}:shares:{costEstimateId}  TTL: 15 min
        /// </summary>
        Task<List<Guid>> GetSharedWithUserIdsAsync(
            Guid tenantId,
            Guid projectId,
            Guid costEstimateId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates all access cache entries for a project (IDs per scope, level per user).
        /// </summary>
        Task InvalidateAccessCacheAsync(
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates access cache entries for a specific cost estimate (level + shares).
        /// </summary>
        Task InvalidateCostEstimateAccessCacheAsync(
            Guid tenantId,
            Guid projectId,
            Guid costEstimateId,
            CancellationToken cancellationToken = default);
    }
}
