using Business.Interfaces.Constants;

namespace CQRS.CostEstimates.AddCostEstimateGroup
{
    /// <summary>
    /// Command to add a new group to a cost estimate. Returns the created group ID.
    /// </summary>
    public sealed record AddCostEstimateGroupCommand : CostEstimateCommandBase, IRequestCommand<Guid>
    {
        public Guid? ParentGroupId { get; init; }
        public int Order { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectResourcesWrite;
    }
}
