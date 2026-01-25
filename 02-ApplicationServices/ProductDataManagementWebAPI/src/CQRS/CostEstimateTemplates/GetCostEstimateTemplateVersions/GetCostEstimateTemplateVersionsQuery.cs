using Business.Interfaces.WebModels.CostEstimateTemplates;
using Entities.Models;

namespace CQRS.CostEstimateTemplates.GetCostEstimateTemplateVersions
{
    /// <summary>
    /// Query do pobrania historii wersji szablonu kosztorysu
    /// </summary>
    public sealed record GetCostEstimateTemplateVersionsQuery(
        Guid TemplateId
    ) : IRequestQuery<List<CostEstimateTemplateVersionHistoryItemWeb>>;
}
