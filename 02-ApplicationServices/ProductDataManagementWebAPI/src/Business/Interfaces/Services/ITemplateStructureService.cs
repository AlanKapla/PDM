using Business.Interfaces.WebModels.CostEstimateTemplates;
using Entities.Models.CostEstimateTemplates;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Service for building template structure DTOs
    /// </summary>
    public interface ITemplateStructureService
    {
        /// <summary>
        /// Builds template version structure for version details endpoint
        /// </summary>
        Task<CostEstimateTemplateVersionStructureWeb> BuildTemplateVersionStructureAsync(
            CostEstimateTemplate template,
            CostEstimateTemplateVersion version,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Builds full template structure for template details endpoint
        /// </summary>
        Task<CostEstimateTemplateStructureWeb> BuildCostEstimateTemplateStructureAsync(
            CostEstimateTemplate template,
            CostEstimateTemplateVersion version,
            CancellationToken cancellationToken = default);
    }
}
