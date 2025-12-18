using Entities.Models;

namespace CQRS.CostEstimates.GetCostEstimates
{
    /// <summary>
    /// Query do pobrania listy kosztorysów dla projektu
    /// </summary>
    public record GetCostEstimatesQuery(
        Guid ProjectId
    ) : IRequestQuery<List<CostEstimateListItem>>
    {
        public Guid TenantId { get; init; }
    }
    
    /// <summary>
    /// Result DTO for cost estimate list item
    /// </summary>
    public record CostEstimateListItem(
        Guid Id,
        Guid TenantId,
        Guid ProjectId,
        string ProjectName,
        Guid TemplateId,
        string TemplateName,
        string Name,
        string? Description,
        CostEstimateStatus Status,
        decimal? TotalNet,
        decimal? TotalGross,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        Guid OwnerId,
        string OwnerName
    );
}
