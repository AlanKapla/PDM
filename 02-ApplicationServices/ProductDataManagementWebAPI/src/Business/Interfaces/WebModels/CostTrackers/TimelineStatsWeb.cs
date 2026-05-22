namespace Business.Interfaces.WebModels.CostTrackers
{
    /// <summary>
    /// Zagregowane statystyki czasowe dla węzła z harmonogramem.
    /// Używany jako kompozycja (property Timeline) — nie dziedziczy po StatusedNodeWeb.
    ///
    /// ProgressPercent = CompletedCount / TotalWorkCount * 100
    /// DelayDays = max opóźnienia spośród dzieci (dni po PlannedEnd dla Delayed/CompletedLate)
    /// </summary>
    public sealed record TimelineStatsWeb
    {
        public DateTime? PlannedStart { get; init; }
        public DateTime? PlannedEnd { get; init; }
        public double? TotalPlannedDays { get; init; }
        public required int TotalWorkCount { get; init; }
        public required int CompletedCount { get; init; }
        public required int CompletedLateCount { get; init; }
        public required int InProgressCount { get; init; }
        public required int NotStartedCount { get; init; }
        public required int DelayedCount { get; init; }
        public decimal? ProgressPercent { get; init; }
        public double? DelayDays { get; init; }
        public required TimelineStatus OverallStatus { get; init; }
        public required bool IsDelayed { get; init; }
        public required bool IsCompleted { get; init; }
    }
}
