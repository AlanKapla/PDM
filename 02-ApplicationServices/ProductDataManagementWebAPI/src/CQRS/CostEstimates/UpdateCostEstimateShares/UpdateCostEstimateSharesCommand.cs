using Business.Interfaces.Constants;
using MediatR;

namespace CQRS.CostEstimates.UpdateCostEstimateShares
{
    /// <summary>
    /// Sets the desired share state for a cost estimate.
    /// Users in UserIds that are not yet shared — will be added.
    /// Users currently shared that are NOT in UserIds — will be removed.
    /// </summary>
    public sealed record UpdateCostEstimateSharesCommand : CostEstimateCommandBase, IRequestCommand<Unit>
    {
        /// <summary>
        /// Full desired list of users who should have access.
        /// Empty list removes all shares.
        /// </summary>
        public List<Guid> UserIds { get; init; } = [];

        public override string PermissionCode => PermissionCodes.ProjectResourcesShare;
    }
}
