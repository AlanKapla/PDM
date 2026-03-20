using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Cache service for cost estimate data stored in Redis.
    /// Cache keys include tenantId and projectId for multi-tenant isolation.
    /// ownerId is used for post-fetch validation only (not in cache key).
    /// </summary>
    public interface ICostEstimateCacheService
    {
        /// <summary>
        /// Gets cost estimate from cache or loads from DB.
        /// Includes Owner and SelectedCurrency navigation properties.
        /// </summary>
        Task<CostEstimate?> GetCostEstimateAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets cost estimate template with all field definitions and currencies from cache or loads from DB.
        /// </summary>
        Task<CostEstimateTemplate?> GetTemplateAsync(
            Guid templateId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all non-deleted groups for a cost estimate as a dictionary keyed by group ID.
        /// </summary>
        Task<Dictionary<Guid, CostEstimateGroup>> GetGroupsDictionaryAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all non-deleted items for a cost estimate as a dictionary keyed by item ID.
        /// </summary>
        Task<Dictionary<Guid, CostEstimateItem>> GetItemsDictionaryAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all group field values for a cost estimate as a dictionary keyed by field value ID.
        /// Includes FieldDefinition navigation property.
        /// </summary>
        Task<Dictionary<Guid, CostEstimateGroupFieldValue>> GetGroupFieldValuesDictionaryAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all item field values for a cost estimate as a dictionary keyed by field value ID.
        /// Includes FieldDefinition navigation property and non-deleted Files.
        /// </summary>
        Task<Dictionary<Guid, CostEstimateItemFieldValue>> GetItemFieldValuesDictionaryAsync(
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

        /// <summary>
        /// Invalidates group field values cache for a cost estimate.
        /// </summary>
        Task InvalidateGroupFieldValuesAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates item field values cache for a cost estimate.
        /// </summary>
        Task InvalidateItemFieldValuesAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates the template cache entry so subsequent reads reload from DB.
        /// Must be called whenever template structure, currencies or units are modified.
        /// </summary>
        Task InvalidateTemplateAsync(
            Guid templateId,
            CancellationToken cancellationToken = default);
    }
}
