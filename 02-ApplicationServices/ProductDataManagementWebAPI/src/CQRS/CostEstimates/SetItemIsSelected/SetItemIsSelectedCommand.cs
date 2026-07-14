using Business.Interfaces.Constants;
using MediatR;

namespace CQRS.CostEstimates.SetItemIsSelected
{
    /// <summary>
    /// Command to change IsSelected flag on a cost estimate item.
    /// For Option items: auto-deselects other options (exclusive).
    /// For None/Component items: simple checkbox toggle for summation.
    /// </summary>
    public sealed record SetItemIsSelectedCommand : CostEstimateCommandBase, IRequestCommand<Unit>
    {
        public Guid ItemId { get; init; }
        public bool IsSelected { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
