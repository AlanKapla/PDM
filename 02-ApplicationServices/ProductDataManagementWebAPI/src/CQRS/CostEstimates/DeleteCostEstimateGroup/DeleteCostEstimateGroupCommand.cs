using Business.Interfaces.Constants;
using MediatR;

namespace CQRS.CostEstimates.DeleteCostEstimateGroup
{
    /// <summary>
    /// Command to soft-delete a group from a cost estimate.
    /// Deletes the group and all its child groups and items.
    /// </summary>
    public sealed record DeleteCostEstimateGroupCommand : CostEstimateCommandBase, IRequestCommand<Unit>
    {
        public Guid GroupId { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
