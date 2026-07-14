using Business.Interfaces.Constants;
using Entities.Models.CostEstimates;

namespace CQRS.CostEstimates.AddCostEstimateItem
{
    /// <summary>
    /// Command to add a new item to a cost estimate group. Returns the created item ID.
    /// </summary>
    public sealed record AddCostEstimateItemCommand : CostEstimateCommandBase, IRequestCommand<Guid>
    {
        public Guid GroupId { get; init; }
        public Guid? ParentItemId { get; init; }
        public ItemRelationType RelationType { get; init; }
        public int Order { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
