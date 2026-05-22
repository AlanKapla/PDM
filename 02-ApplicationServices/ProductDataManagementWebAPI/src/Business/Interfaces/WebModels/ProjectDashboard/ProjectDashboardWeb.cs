using Business.Interfaces.WebModels.CostTrackers;

namespace Business.Interfaces.WebModels.ProjectDashboard
{
    public sealed record ProjectDashboardWeb
    {
        public required Guid ProjectId { get; init; }
        public required DateTime GeneratedAt { get; init; }
        public required DateTime ReferenceDate { get; init; }
        public string? SelectedCurrencyCode { get; init; }
        public string? SelectedCurrencySymbol { get; init; }
        public required ProjectFinancialSummaryWeb FinancialSummary { get; init; }
        public required ProjectTimelineSummaryWeb TimelineSummary { get; init; }
        public required List<CostEstimateSummaryWeb> CostEstimateSummaries { get; init; }
        public required List<ScheduleSummaryWeb> ScheduleSummaries { get; init; }
        public required ProjectAdditionalCostsWeb ProjectAdditionalCosts { get; init; }
        public required List<TrackedCostWeb> AllCosts { get; init; }
    }
}
