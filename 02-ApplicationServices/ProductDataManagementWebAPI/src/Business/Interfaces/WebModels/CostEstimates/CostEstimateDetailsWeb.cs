using Business.Interfaces.Constants;
using Entities.Models.CostEstimates;

namespace Business.Interfaces.WebModels.CostEstimates
{
    /// <summary>
    /// Result DTO for cost estimate details
    /// Zawiera pełne dane kosztorysu wraz z hierarchią grup i pozycji
    /// </summary>
    public sealed record CostEstimateDetailsWeb(
        Guid Id,
        Guid TenantId,
        Guid ProjectId,
        string? SelectedCurrencyCode,
        string? SelectedCurrencySymbol,
        string Name,
        string? Description,
        CostEstimateStatus Status,
        Guid? WorkScheduleId,
        List<CostEstimateGroupWeb> RootGroups,
        IReadOnlyList<CostEstimateFieldSchemaWeb> FieldSchemas,
        IReadOnlyList<CostEstimateAdditionalFieldWeb> AdditionalFields,
        decimal? TotalNet,
        decimal? TotalGross,
        decimal? TotalVat,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        DateTime? LastCalculatedAt,
        Guid OwnerId,
        string OwnerName,
        CostEstimateAccessLevel AccessLevel,
        IReadOnlyList<CostEstimateShareWeb> SharedWithUsers
    );
}
