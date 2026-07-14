using Entities.Models.CostEstimates;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Cache service for cost estimate data stored in Redis.
    /// Cache keys include tenantId and projectId for multi-tenant isolation.
    /// </summary>
    public interface ICostEstimateCacheService
    {
        /// <summary>
        /// Gets cost estimate from cache or loads from DB.
        /// Includes Owner navigation property.
        /// </summary>
        Task<CostEstimate?> GetCostEstimateAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all non-deleted groups for a cost estimate as a dictionary keyed by group ID.
        /// Includes AdditionalFieldValues navigation property.
        /// </summary>
        Task<Dictionary<Guid, CostEstimateGroup>> GetGroupsDictionaryAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all non-deleted items for a cost estimate as a dictionary keyed by item ID.
        /// Includes AdditionalFieldValues and Files navigation properties.
        /// </summary>
        Task<Dictionary<Guid, CostEstimateItem>> GetItemsDictionaryAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates all cache entries for a cost estimate.
        /// </summary>
        Task InvalidateCostEstimateAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates only groups cache for a cost estimate.
        /// </summary>
        Task InvalidateGroupsAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates only items cache for a cost estimate.
        /// </summary>
        Task InvalidateItemsAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default);
    }
}
