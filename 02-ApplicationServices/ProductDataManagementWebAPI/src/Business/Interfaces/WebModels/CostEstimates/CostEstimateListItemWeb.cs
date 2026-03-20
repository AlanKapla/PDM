using Business.Interfaces.Constants;
using Entities.Models;
using Entities.Models.CostEstimates;

namespace Business.Interfaces.WebModels.CostEstimates
{
    /// <summary>
    /// Result DTO for cost estimate list item
    /// </summary>
    public record CostEstimateListItemWeb(
        Guid Id,
        Guid TenantId,
        Guid ProjectId,
        Guid TemplateId,
        string TemplateName,
        string Name,
        string? Description,
        CostEstimateStatus Status,
        decimal? TotalNet,
        decimal? TotalGross,
        decimal? TotalVat,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        Guid OwnerId,
        string OwnerName,
        bool IsSharedWithMe,
        bool IsSharedByMe,
        IReadOnlyList<CostEstimateShareWeb> SharedWithUsers
    );
}
