namespace Business.Interfaces.WebModels.CostTrackers
{
    /// <summary>
    /// Opisuje typ powiązania pozycji w trackerze kosztów.
    /// LinkedWorkItem — koszt wspólny:
    ///   CostEstimateItemId i WorkScheduleStageWorkId oba wypełnione.
    /// EstimateItem — koszt tylko przy pozycji kosztorysu:
    ///   CostEstimateItemId wypełnione, WorkScheduleStageWorkId null.
    /// ScheduleWorkItem — koszt tylko przy zakresie pracy:
    ///   WorkScheduleStageWorkId wypełnione, CostEstimateItemId null.
    /// </summary>
    public enum WorkItemType
    {
        LinkedWorkItem = 0,
        EstimateItem = 1,
        ScheduleWorkItem = 2
    }

    /// <summary>
    /// Reprezentuje pozycję w trackerze kosztów.
    /// Może być powiązana z pozycją kosztorysu, zakresem pracy,
    /// oboma (koszt wspólny) lub żadnym (koszt dodatkowy projektu).
    /// Typ powiązania określa WorkItemType.
    /// Dziedziczy po TrackerNodeWithTimelineWeb.
    /// </summary>
    public sealed record WorkItemLinkWeb : TrackerNodeWithTimelineWeb
    {
        public required string DisplayName { get; init; }
        public required int Order { get; init; }
        public required WorkItemType WorkItemType { get; init; }

        /// <summary>
        /// ID pozycji kosztorysu.
        /// Wypełnione gdy WorkItemType = EstimateItem lub LinkedWorkItem.
        /// Null gdy WorkItemType = ScheduleWorkItem.
        /// </summary>
        public Guid? CostEstimateItemId { get; init; }

        /// <summary>
        /// ID zakresu pracy harmonogramu.
        /// Wypełnione gdy WorkItemType = ScheduleWorkItem lub LinkedWorkItem.
        /// Null gdy WorkItemType = EstimateItem.
        /// </summary>
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
