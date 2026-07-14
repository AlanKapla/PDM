using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.CostEstimates;
using MediatR;

namespace CQRS.CostEstimates.ReorderCostEstimateItems
{
    /// <summary>
    /// Command to reorder items within a cost estimate group.
    /// </summary>
    public sealed record ReorderCostEstimateItemsCommand : CostEstimateCommandBase, IRequestCommand<Unit>
    {
        public Guid GroupId { get; init; }
        public List<ReorderItemDto> Items { get; init; } = new();

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
