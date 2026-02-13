using Business.Interfaces.WebModels.CostEstimateTemplates;
using Entities.Models;

namespace CQRS.CostEstimateTemplates.GetCostEstimateTemplates
{
    /// <summary>
    /// Query do pobrania listy szablonów kosztorysów użytkownika
    /// </summary>
    public record GetCostEstimateTemplatesQuery : IRequestQuery<List<CostEstimateTemplateListItemWeb>>;
}
