using Business.Interfaces.Constants;
using MediatR;

namespace CQRS.CostEstimates.MoveCostEstimateItem
{
    /// <summary>
    /// Command to move an item from one group to another.
    /// Changes GroupId and places the item at the last position in the target group.
    /// </summary>
    public sealed record MoveCostEstimateItemCommand : CostEstimateCommandBase, IRequestCommand<Unit>
    {
        public Guid ItemId { get; init; }
        public Guid TargetGroupId { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
