using Entities.Models;
using Entities.Models.CostEstimateData;

namespace CQRS.CostEstimates.GetCostEstimateDetails
{
    /// <summary>
    /// Query do pobrania szczegółów kosztorysu
    /// </summary>
    public record GetCostEstimateDetailsQuery(
        Guid CostEstimateId
    ) : IRequestQuery<CostEstimateDetails>
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
    }
    
    /// <summary>
    /// Result DTO for cost estimate details
    /// </summary>
    public record CostEstimateDetails(
        Guid Id,
        Guid TenantId,
        Guid ProjectId,
        string ProjectName,
        Guid TemplateId,
        string TemplateName,
        string Name,
        string? Description,
        CostEstimateStatus Status,
        CostEstimateDataModel Data,
        decimal? TotalNet,
        decimal? TotalGross,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        DateTime? LastCalculatedAt,
        Guid OwnerId,
        string OwnerName
    );
}
