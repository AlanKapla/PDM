using Business.Interfaces.WebModels.CostEstimateTemplates;

namespace CQRS.CostEstimateTemplates.GetDefaultCostEstimateTemplates
{
    /// <summary>
    /// Query to get a list of all available default (system) cost estimate templates
    /// </summary>
    public record GetDefaultCostEstimateTemplatesQuery : IRequestQuery<List<DefaultCostEstimateTemplateListItemWeb>>;
}
