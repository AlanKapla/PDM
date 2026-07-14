using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.CostEstimates;

namespace CQRS.CostEstimates.GetCostEstimateDetails
{
    /// <summary>
    /// Query do pobrania szczegółów kosztorysu.
    /// </summary>
    public sealed record GetCostEstimateDetailsQuery : CostEstimateCommandBase, IRequestQuery<CostEstimateDetailsWeb>
    {
        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
