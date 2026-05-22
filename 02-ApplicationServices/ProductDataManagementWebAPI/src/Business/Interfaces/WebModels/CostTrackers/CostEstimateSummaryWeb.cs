namespace Business.Interfaces.WebModels.CostTrackers
{
    public sealed record CostEstimateSummaryWeb : TrackerNodeWithTimelineWeb
    {
        public required Guid CostEstimateId { get; init; }
        public required string CostEstimateName { get; init; }
        public required int TotalItemsCount { get; init; }
        public required int ItemsWithCostsCount { get; init; }
        public required int ItemsWithoutCostsCount { get; init; }
        public required int ItemsOverBudgetCount { get; init; }
        public required int ItemsNearLimitCount { get; init; }
        public Guid? LinkedWorkScheduleId { get; init; }
        public required List<TrackerGroupWeb> Groups { get; init; }

        /// <summary>Pokrycie budżetu przez koszty: (CostsNet / BudgetNet) * 100. Null gdy brak budżetu.</summary>
        public decimal? BudgetCoveredPercent { get; init; }

        /// <summary>Najwcześniejsza planowana data rozpoczęcia (z Timeline.PlannedStart).</summary>
        public DateOnly? TimelinePlannedStart { get; init; }

        /// <summary>Najpóźniejsza planowana data zakończenia (z Timeline.PlannedEnd).</summary>
        public DateOnly? TimelinePlannedEnd { get; init; }

        /// <summary>Łączna liczba planowanych dni kosztorysu (TimelinePlannedEnd - TimelinePlannedStart). Null gdy brak dat.</summary>
        public int? TimelineTotalDays { get; init; }
    }
}
