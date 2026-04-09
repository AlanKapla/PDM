namespace Business.Interfaces.WebModels.CostTrackers
{
    public record CostTrackerSummaryWeb : CostTrackerSummaryBaseWeb
    {
        public required int CostEstimatesCount { get; init; }
        public required int CostEstimatesWithCostsCount { get; init; }
    }
}
