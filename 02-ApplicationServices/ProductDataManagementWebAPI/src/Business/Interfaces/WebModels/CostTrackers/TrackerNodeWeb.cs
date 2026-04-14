namespace Business.Interfaces.WebModels.CostTrackers
{
    public abstract record TrackerNodeWeb
    {
        public decimal? BudgetNet { get; init; }
        public decimal? BudgetGross { get; init; }
        public decimal? CostsNet { get; init; }
        public decimal? CostsGross { get; init; }
        public decimal? DeviationNet { get; init; }
        public decimal? DeviationPercent { get; init; }
        public required bool IsBudgetExceeded { get; init; }
        public required int Status { get; init; }
        public required int CostCount { get; init; }
        public decimal? CoveredPercent { get; init; }
    }
}
