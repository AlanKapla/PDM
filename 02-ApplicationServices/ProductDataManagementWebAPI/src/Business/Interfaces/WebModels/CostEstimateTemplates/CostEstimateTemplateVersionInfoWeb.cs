using Entities.Models;
using Entities.Models.CostEstimates;

namespace Business.Interfaces.WebModels.CostEstimateTemplates
{
    /// <summary>
    /// Version info embedded in template details
    /// Podstawowe informacje o wersji szablonu
    /// Szczegółowe definicje pól pobierane przez dedykowane endpointy
    /// </summary>
    public record CostEstimateTemplateVersionInfoWeb(
        Guid Id,
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
