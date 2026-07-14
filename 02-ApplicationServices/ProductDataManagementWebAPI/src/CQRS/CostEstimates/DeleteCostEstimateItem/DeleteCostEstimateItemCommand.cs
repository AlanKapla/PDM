using Business.Interfaces.Constants;
using MediatR;

namespace CQRS.CostEstimates.DeleteCostEstimateItem
{
    /// <summary>
    /// Command to soft-delete an item from a cost estimate.
    /// Also deletes all child items (options, components).
    /// </summary>
    public sealed record DeleteCostEstimateItemCommand : CostEstimateCommandBase, IRequestCommand<Unit>
    {
        public Guid ItemId { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
