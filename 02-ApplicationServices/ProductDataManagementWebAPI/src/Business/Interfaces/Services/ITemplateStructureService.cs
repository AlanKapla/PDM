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
        /// Builds template structure with all fields and configuration
        /// </summary>
        Task<CostEstimateTemplateStructureWeb> BuildTemplateStructureAsync(
            CostEstimateTemplate template,
            CancellationToken cancellationToken = default);
    }
}
