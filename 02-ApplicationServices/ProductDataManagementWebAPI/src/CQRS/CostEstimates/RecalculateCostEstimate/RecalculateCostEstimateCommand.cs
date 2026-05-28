using Business.Interfaces.Constants;
using MediatR;

namespace CQRS.CostEstimates.RecalculateCostEstimate
{
    /// <summary>
    /// Command to recalculate all totals (Net, Gross, VAT) for a cost estimate.
    /// Recalculates item values, group totals and cost estimate totals.
    /// </summary>
    public sealed record RecalculateCostEstimateCommand : CostEstimateCommandBase, IRequestCommand<Unit>
    {
        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
