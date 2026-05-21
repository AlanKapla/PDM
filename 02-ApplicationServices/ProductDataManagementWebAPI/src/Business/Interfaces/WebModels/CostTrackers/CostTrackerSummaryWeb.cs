namespace Business.Interfaces.WebModels.CostTrackers
{
    public sealed record CostTrackerSummaryWeb : CostTrackerSummaryBaseWeb
    {
        public required int CostEstimatesCount { get; init; }
        public required int CostEstimatesWithCostsCount { get; init; }
    }
}
