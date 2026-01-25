using Business.Interfaces.WebModels.CostEstimateTemplates;
using Entities.Models;

namespace CQRS.CostEstimateTemplates.GetCostEstimateTemplateDetails
{
    /// <summary>
    /// Query do pobrania szczegółów szablonu kosztorysu
    /// </summary>
    public record GetCostEstimateTemplateDetailsQuery(
        Guid TemplateId
    ) : IRequestQuery<CostEstimateTemplateDetailsWeb>
    {
        /// <summary>
        /// Optional: Version ID to view a specific template version
        /// If null, returns the latest version
        /// </summary>
        public Guid? VersionId { get; init; }
    }
}
