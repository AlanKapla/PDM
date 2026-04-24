using CQRS.CostTrackers.Shared;

namespace CQRS.CostTrackers.CreateTrackedCost
{
    /// <summary>
    /// Command do tworzenia kosztu w trackerze
    /// </summary>
    public sealed record CreateTrackedCostCommand : TrackedCostCommandBase
    {
        public Guid? WorkItemLinkId { get; init; }
        public Guid? CostEstimateItemId { get; init; }
        public Guid? WorkScheduleStageWorkId { get; init; }
    }
}
