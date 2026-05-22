namespace Business.Interfaces.WebModels.CostTrackers
{
    /// <summary>
    /// Zagregowane statystyki czasowe całego projektu — wszystkie harmonogramy łącznie.
    ///
    /// ProgressPercent = CompletedCount / TotalWorkCount * 100
    /// DelayDays = max(DelayDays) spośród wszystkich opóźnionych zakresów pracy
    /// OverallStatus agregowany wg priorytetu: Delayed > CompletedLate > InProgress
    ///              > NotStarted > Completed > NoSchedule
    /// </summary>
    public sealed record ProjectTimelineSummaryWeb
    {
        public DateTime? EarliestStart { get; init; }
        public DateTime? LatestEnd { get; init; }
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
        public required int WorkSchedulesCount { get; init; }
        public required int ActiveSchedulesCount { get; init; }
        public required int CompletedSchedulesCount { get; init; }
    }
}
