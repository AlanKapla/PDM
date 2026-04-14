namespace Business.Interfaces.WebModels.CostTrackers
{
    public record TrackerItemWeb : TrackerNodeWeb
    {
        public required Guid CostEstimateItemId { get; init; }
        public required string Name { get; init; }
        public required List<TrackedCostWeb> Costs { get; init; }
    }
}
