using Business.Interfaces.WebModels.CostEstimateTemplates;
using Entities.Models;

namespace CQRS.CostEstimateTemplates.GetCostEstimateTemplateDetails
{
    /// <summary>
    /// Query do pobrania szczegółów szablonu kosztorysu z pełną strukturą
    /// </summary>
    public record GetCostEstimateTemplateDetailsQuery(
        Guid TemplateId
    ) : IRequestQuery<CostEstimateTemplateDetailsWeb>;
}
