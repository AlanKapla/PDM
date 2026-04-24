namespace Business.Interfaces.WebModels.CostTrackers
{
    /// <summary>
    /// Opisuje źródło pozycji w trackerze kosztów.
    /// Link  — pozycja pochodzi z CostEstimateItemWorkScheduleStageWorkLink (ma oba: CostEstimateItem i WorkScheduleStageWork).
    /// Estimate — pozycja pochodzi wyłącznie z CostEstimateItem (brak powiązanego WorkScheduleStageWork).
    /// Schedule — pozycja pochodzi wyłącznie z WorkScheduleStageWork (brak powiązanego CostEstimateItem).
    /// </summary>
    public enum WorkItemType
    {
        Link = 0,
        Estimate = 1,
        Schedule = 2
    }

    /// <summary>
    /// Reprezentuje CostEstimateItemWorkScheduleStageWorkLink — pojedynczą pozycję łączącą CostEstimateItem
    /// z WorkScheduleStageWork. Może istnieć tylko po jednej stronie.
    /// Dziedziczy po TrackerNodeWithTimelineWeb.
    /// </summary>
    public sealed record WorkItemLinkWeb : TrackerNodeWithTimelineWeb
    {
        public Guid? WorkItemLinkId { get; init; }
        public required string DisplayName { get; init; }
        public required int Order { get; init; }
        public required WorkItemType WorkItemType { get; init; }

        /// <summary>Null gdy link nie ma powiązanego CostEstimateItem.</summary>
        public Guid? CostEstimateItemId { get; init; }

        /// <summary>Null gdy link nie ma powiązanego WorkScheduleStageWork.</summary>
        public Guid? WorkScheduleStageWorkId { get; init; }

        public required List<TrackedCostWeb> Costs { get; init; }

        /// <summary>Pokrycie budżetu przez koszty: (CostsNet / BudgetNet) * 100. Null gdy brak budżetu.</summary>
        public decimal? BudgetCoveredPercent { get; init; }

        /// <summary>Planowana data rozpoczęcia (z Timeline). Null gdy brak harmonogramu.</summary>
        public DateOnly? TimelinePlannedStart { get; init; }

        /// <summary>Planowana data zakończenia (z Timeline). Null gdy brak harmonogramu.</summary>
        public DateOnly? TimelinePlannedEnd { get; init; }

        /// <summary>Łączna liczba planowanych dni (TimelinePlannedEnd - TimelinePlannedStart). Null gdy brak dat.</summary>
        public int? TimelineTotalDays { get; init; }
    }
}
