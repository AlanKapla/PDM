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
            List<UnitDto>? units,
            List<CategoryDto>? categories,
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

        /// <summary>
        /// Returns a list of all available default (system) templates loaded from embedded JSON resources
        /// </summary>
        List<DefaultCostEstimateTemplateListItemWeb> GetDefaultTemplates();

        /// <summary>
        /// Returns the full structure of a default template by its slug identifier
        /// </summary>
        CostEstimateTemplateStructureWeb? GetDefaultTemplateDetails(string slug);

        /// <summary>
        /// Creates a new user template by copying the full structure from a default (system) template.
        /// Generates new fieldName GUIDs server-side. Returns the new template ID.
        /// </summary>
        Task<Guid> CreateTemplateFromDefaultAsync(
            Guid ownerId,
            string slug,
            string name,
            string? description,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Duplicates an existing user template with all its structure (fields, currencies, units).
        /// Generates new fieldName GUIDs. Returns the new template ID.
        /// </summary>
        Task<Guid> DuplicateTemplateAsync(
            CostEstimateTemplate sourceTemplate,
            Guid ownerId,
            string name,
            string? description,
            CancellationToken cancellationToken = default);
    }
}
