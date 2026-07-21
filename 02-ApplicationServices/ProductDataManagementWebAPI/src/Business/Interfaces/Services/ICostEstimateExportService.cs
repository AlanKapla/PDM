using Business.Interfaces.WebModels.CostEstimates;
using Entities.Models.CostEstimates;

namespace Business.Interfaces.Services
{
    public interface ICostEstimateExportService
    {
        CostEstimateExportFile Export(
            CostEstimate costEstimate,
            IReadOnlyList<CostEstimateGroup> allGroups,
            IReadOnlyList<CostEstimateItem> allItems,
            IReadOnlyList<CostEstimateAdditionalFieldWeb> additionalFields,
            string? currencyCode,
            string? currencySymbol,
            CostEstimateExportFormat format,
            DateTime? exportedAtUtc = null);
    }
}
