using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.CostEstimates;
using MediatR;

namespace CQRS.CostEstimates.ReorderCostEstimateItemChildren
{
    /// <summary>
    /// Command to reorder child items (options or components) within a parent item.
    /// </summary>
    public sealed record ReorderCostEstimateItemChildrenCommand : CostEstimateCommandBase, IRequestCommand<Unit>
    {
        public Guid ParentItemId { get; init; }
        public List<ReorderItemChildDto> Items { get; init; } = new();

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
