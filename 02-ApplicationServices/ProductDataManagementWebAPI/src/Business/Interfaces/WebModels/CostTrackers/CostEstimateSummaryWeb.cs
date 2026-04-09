namespace Business.Interfaces.WebModels.CostTrackers
{
    public record CostEstimateSummaryWeb : CostTrackerSummaryBaseWeb
    {
        public required Guid CostEstimateId { get; init; }
        public required string CostEstimateName { get; init; }
        public required int TotalItemsCount { get; init; }
        public required int ItemsWithCostsCount { get; init; }
        public required int ItemsWithoutCostsCount { get; init; }
        public required int ItemsOverBudgetCount { get; init; }
        public required int ItemsNearLimitCount { get; init; }
        public required List<TrackerGroupWeb> Groups { get; init; }
        public required TrackerAdditionalCostsWeb AdditionalCosts { get; init; }
    }
}
