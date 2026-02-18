using Business.Interfaces.WebModels.CostEstimates;

namespace Business.Interfaces.Services;

/// <summary>
/// Service for managing CostEstimate lifecycle
/// </summary>
public interface ICostEstimateService
{
    /// <summary>
    /// Creates a new CostEstimate (empty, without groups/items)
    /// Groups and items should be added via UpdateAsync
    /// </summary>
    Task<Guid> CreateAsync(
        Guid tenantId,
        Guid projectId,
        Guid templateId,
        Guid selectedCurrencyId,
        string name,
        string? description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates CostEstimate with full hierarchy (groups, items, options, components)
    /// Validates structure and recalculates totals
    /// </summary>
    Task UpdateAsync(
        CostEstimateUpdateDto dto,
        CancellationToken cancellationToken = default);
}


