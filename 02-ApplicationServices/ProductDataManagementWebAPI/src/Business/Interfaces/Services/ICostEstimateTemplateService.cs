using Business.Interfaces.WebModels.CostEstimateTemplates;
using Entities.Models.CostEstimateTemplates;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Service for managing cost estimate templates: CRUD, structure building and caching
    /// </summary>
    public interface ICostEstimateTemplateService
    {
        /// <summary>
        /// Creates a new template with default configuration
        /// </summary>
        Task<Guid> CreateTemplateAsync(
            Guid ownerId,
            string name,
            string? description,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates template metadata, currencies, units and optionally field structure.
        /// Recalculates related cost estimates when structure changes.
        /// Invalidates Redis cache after update.
        /// </summary>
        Task UpdateTemplateAsync(
            CostEstimateTemplate template,
            string name,
            string? description,
            string? category,
            bool canAddGroups,
            bool canBranchGroups,
            int? maxGroupLevel,
            bool autoNumberGroups,
            string? groupNumberFormat,
            bool updateStructure,
            List<CurrencyDto>? currencies,
            List<UnitDto>? units,
            List<FieldDefinitionDto>? groupHeaderFields,
            List<FieldDefinitionDto>? systemFields,
            List<FieldDefinitionDto>? calculatedFields,
            List<FieldDefinitionDto>? genericFields,
            UiConfigurationDto? uiConfiguration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft-deletes a template and invalidates its cache
        /// </summary>
        Task DeleteTemplateAsync(
            CostEstimateTemplate template,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Builds template structure with all fields and configuration (no cache)
        /// </summary>
        Task<CostEstimateTemplateStructureWeb> BuildTemplateStructureAsync(
            CostEstimateTemplate template,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets template structure from Redis cache or builds and caches it
        /// Cache key: platform:template:{templateId}
        /// </summary>
        Task<CostEstimateTemplateStructureWeb> GetTemplateStructureCachedAsync(
            CostEstimateTemplate template,
            CancellationToken cancellationToken = default);
    }
}
