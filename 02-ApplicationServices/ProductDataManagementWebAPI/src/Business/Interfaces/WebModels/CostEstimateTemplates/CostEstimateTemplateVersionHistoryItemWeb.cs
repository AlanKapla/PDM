using Entities.Models;
using Entities.Models.CostEstimates;

namespace Business.Interfaces.WebModels.CostEstimateTemplates
{
    /// <summary>
    /// Item historii wersji szablonu kosztorysu
    /// </summary>
    public record CostEstimateTemplateVersionHistoryItemWeb(
        Guid Id,
        Guid TemplateId,
        int VersionNumber,
        string? VersionName,
        TemplateVersionStatus Status,
        DateTime CreatedAt,
        DateTime? ApprovedAt,
        Guid? ApprovedById,
        string? ApprovedByUserName,
        DateTime? DeprecatedAt
    );
}
