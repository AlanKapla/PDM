namespace Business.Interfaces.WebModels.CostTrackers
{
    public record ProjectDashboardWeb
    {
        public required Guid ProjectId { get; init; }
        public required DateTime GeneratedAt { get; init; }
        public required DateTime ReferenceDate { get; init; }
        public required ProjectFinancialSummaryWeb FinancialSummary { get; init; }
        public required ProjectTimelineSummaryWeb TimelineSummary { get; init; }
        public required List<CostEstimateSummaryWeb> CostEstimateSummaries { get; init; }
        public required List<ScheduleSummaryWeb> ScheduleSummaries { get; init; }
        public required ProjectAdditionalCostsWeb ProjectAdditionalCosts { get; init; }
        public required List<TrackedCostWeb> AllCosts { get; init; }
    }
}
