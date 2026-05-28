using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.CostEstimates;
using MediatR;

namespace CQRS.CostEstimates.ReorderCostEstimateGroups
{
    /// <summary>
    /// Command to reorder groups within a cost estimate.
    /// </summary>
    public sealed record ReorderCostEstimateGroupsCommand : CostEstimateCommandBase, IRequestCommand<Unit>
    {
        public List<ReorderGroupDto> Groups { get; init; } = new();

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
