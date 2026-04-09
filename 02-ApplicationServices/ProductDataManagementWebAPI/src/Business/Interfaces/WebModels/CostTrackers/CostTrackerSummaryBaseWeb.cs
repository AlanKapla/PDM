namespace Business.Interfaces.WebModels.CostTrackers
{
    public abstract record CostTrackerSummaryBaseWeb
    {
        public decimal? TotalCostsNet { get; init; }
        public decimal? TotalCostsGross { get; init; }
        public decimal? TotalBudgetNet { get; init; }
        public decimal? TotalBudgetGross { get; init; }
        public decimal? TotalDeviationNet { get; init; }
        public decimal? TotalDeviationGross { get; init; }
        public decimal? TotalDeviationPercent { get; init; }
        public required bool IsBudgetExceeded { get; init; }
        public decimal? AdditionalCostsNet { get; init; }
        public decimal? AdditionalCostsGross { get; init; }
        public required int AdditionalCostsCount { get; init; }
        public required int CostCount { get; init; }
        public decimal? CoveredPercent { get; init; }
    }
}
