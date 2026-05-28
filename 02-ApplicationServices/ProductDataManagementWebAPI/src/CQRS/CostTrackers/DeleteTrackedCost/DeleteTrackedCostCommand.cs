using Business.Interfaces.Constants;
using CQRS.CostTrackers.Shared;
using MediatR;

namespace CQRS.CostTrackers.DeleteTrackedCost
{
    /// <summary>
    /// Command do usunięcia kosztu z trackera (soft-delete).
    /// </summary>
    public sealed record DeleteTrackedCostCommand : CostTrackerCommandBase, IRequestCommand<Unit>
    {
        public required Guid CostId { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectDashboardTracker;
    }
}
