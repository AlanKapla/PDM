using CQRS.CostTrackers.Shared;

namespace CQRS.CostTrackers.UpdateTrackedCost
{
    /// <summary>
    /// Command do aktualizacji kosztu w trackerze (pełne nadpisanie)
    /// </summary>
    public sealed record UpdateTrackedCostCommand : TrackedCostCommandBase
    {
        public Guid CostId { get; init; }
        public Guid? CostEstimateItemId { get; init; }
        public Guid? WorkScheduleStageWorkId { get; init; }
        public IReadOnlyList<Guid>? ExistingAttachmentIds { get; init; }
        public bool ClearAllAttachments { get; init; }
    }
}
