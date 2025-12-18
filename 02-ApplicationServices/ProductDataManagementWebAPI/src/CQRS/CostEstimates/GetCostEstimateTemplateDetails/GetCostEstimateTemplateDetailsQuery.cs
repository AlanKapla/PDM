using Entities.Models.CostEstimateTemplateDefinitions;

namespace CQRS.CostEstimates.GetCostEstimateTemplateDetails
{
    /// <summary>
    /// Query do pobrania szczegółów szablonu kosztorysu
    /// </summary>
    public record GetCostEstimateTemplateDetailsQuery(
        Guid TemplateId
    ) : IRequestQuery<CostEstimateTemplateDetails>;
    
    /// <summary>
    /// Result DTO for template details
    /// </summary>
    public record CostEstimateTemplateDetails(
        Guid Id,
        string Name,
        string? Description,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        Guid OwnerId,
        string OwnerName,
        CostEstimateTemplateStructure TemplateStructure
    );
}
