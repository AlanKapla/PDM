using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using Entities.Models;
using Entities.Models.CostEstimates;

namespace Business.Interfaces.WebModels.CostEstimates
{
    /// <summary>
    /// Result DTO for cost estimate details
    /// Zawiera pełne dane kosztorysu wraz ze strukturą szablonu użytego do jego utworzenia
    /// </summary>
    public record CostEstimateDetailsWeb(
        Guid Id,
        Guid TenantId,
        Guid ProjectId,
        Guid TemplateId,
        string TemplateName,
        Guid SelectedCurrencyId,
        string SelectedCurrencyCode,
        string? SelectedCurrencySymbol,
        string Name,
        string? Description,
        CostEstimateStatus Status,
        Guid? WorkScheduleId,
        List<CostEstimateGroupWeb> RootGroups,
        decimal? TotalNet,
        decimal? TotalGross,
        decimal? TotalVat,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        DateTime? LastCalculatedAt,
        Guid OwnerId,
        string OwnerName,
        CostEstimateTemplateStructureWeb TemplateStructure,
        CostEstimateAccessLevel AccessLevel,
        IReadOnlyList<CostEstimateShareWeb> SharedWithUsers
    );
}
