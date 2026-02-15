using Business.Interfaces.WebModels.CostEstimateTemplates;

namespace Business.Interfaces.Services;

/// <summary>
/// Service for managing CostEstimateTemplate lifecycle
/// </summary>
public interface ICostEstimateTemplateService
{
    /// <summary>
    /// Creates a new CostEstimateTemplate with basic properties only
    /// Full structure (fields, currencies, units) should be added via UpdateAsync
    /// </summary>
    Task<Guid> CreateAsync(
        string name,
        string? description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates CostEstimateTemplate with full structure
    /// Handles currencies, units, field definitions, and recalculation of dependent cost estimates
    /// </summary>
    Task UpdateAsync(
        CostEstimateTemplateUpdateDto dto,
        CancellationToken cancellationToken = default);
}


