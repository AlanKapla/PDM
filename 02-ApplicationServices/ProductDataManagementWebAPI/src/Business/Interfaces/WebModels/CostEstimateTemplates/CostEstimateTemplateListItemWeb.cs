using Entities.Models;
using Entities.Models.CostEstimates;

namespace Business.Interfaces.WebModels.CostEstimateTemplates
{
    /// <summary>
    /// Result DTO for template list item
    /// </summary>
    public record CostEstimateTemplateListItemWeb(
        Guid Id,
        string Name,
        string? Description,
        string? Category,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        int? LatestVersionNumber,
        TemplateVersionStatus? LatestVersionStatus,
        Guid OwnerId,
        string OwnerName
    );
}
