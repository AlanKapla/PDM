using Business.Interfaces.WebModels.CostEstimateTemplates;

namespace CQRS.CostEstimateTemplates.GetDefaultCostEstimateTemplateDetails
{
    /// <summary>
    /// Query to get full structure details of a default (system) template by slug
    /// </summary>
    public record GetDefaultCostEstimateTemplateDetailsQuery(
        string Slug
    ) : IRequestQuery<CostEstimateTemplateStructureWeb>;
}
