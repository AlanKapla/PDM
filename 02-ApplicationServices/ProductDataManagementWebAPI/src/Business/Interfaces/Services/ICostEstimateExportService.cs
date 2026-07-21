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
            IReadOnlyList<CostEstimateFieldSchemaWeb> fieldSchemas,
            string? currencyCode,
            string? currencySymbol,
            CostEstimateExportFormat format,
            DateTime? exportedAtUtc = null);
    }
}
