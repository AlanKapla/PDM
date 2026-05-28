using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Entities.Models.CostEstimates;

namespace Business.Interfaces.WebModels.CostEstimates
{
    /// <summary>
    /// Result DTO for cost estimate details
    /// Zawiera pełne dane kosztorysu wraz ze strukturą szablonu użytego do jego utworzenia
    /// </summary>
    public sealed record CostEstimateDetailsWeb(
        Guid Id,
        Guid TenantId,
        Guid ProjectId,
        Guid TemplateId,
        string TemplateName,
        string? SelectedCurrencyCode,
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
