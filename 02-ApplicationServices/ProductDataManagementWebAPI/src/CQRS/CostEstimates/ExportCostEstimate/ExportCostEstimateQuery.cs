using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.CostEstimates;

namespace CQRS.CostEstimates.ExportCostEstimate
{
    public sealed record ExportCostEstimateQuery : CostEstimateCommandBase, IRequestQuery<CostEstimateExportFile>
    {
        public required CostEstimateExportFormat Format { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
