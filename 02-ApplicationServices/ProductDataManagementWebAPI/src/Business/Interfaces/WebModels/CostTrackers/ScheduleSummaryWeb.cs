namespace Business.Interfaces.WebModels.CostTrackers
{
    /// <summary>
    /// Podsumowanie harmonogramu.
    /// Dziedziczy po TrackerNodeWithTimelineWeb.
    /// BudgetNet null gdy harmonogram nie ma spięcia z kosztorysem.
    /// Timeline never null (harmonogram zawsze ma statystyki czasowe).
    ///
    /// BudgetNet = suma(Stage.BudgetNet) (null gdy HasLinkedEstimate = false)
    /// CostsNet  = suma(Stage.CostsNet) + AdditionalCosts.TotalNet
    /// </summary>
    public sealed record ScheduleSummaryWeb : TrackerNodeWithTimelineWeb
    {
        public required Guid WorkScheduleId { get; init; }
        public required string WorkScheduleName { get; init; }

        /// <summary>True gdy harmonogram ma spięty co najmniej jeden kosztorys.</summary>
        public required bool HasLinkedEstimate { get; init; }

        /// <summary>Id powiązanego kosztorysu. Null gdy HasLinkedEstimate = false.</summary>
        public Guid? LinkedCostEstimateId { get; init; }

        public required int TotalWorkItemsCount { get; init; }
        public required int WorkItemsWithCostsCount { get; init; }
        public required int WorkItemsOverBudgetCount { get; init; }
        public required int WorkItemsNearLimitCount { get; init; }
        public required int WorkItemsDelayedCount { get; init; }

        public required List<ScheduleStageWeb> Stages { get; init; }

        /// <summary>
        /// Suma CostsNet wszystkich pozycji (WorkItems) we wszystkich etapach harmonogramu.
        /// Odpowiada CostsNet harmonogramu. Null gdy brak kosztów.
        /// </summary>
        public decimal? TotalWorkItemsCostsNet { get; init; }

        /// <summary>
        /// Suma CostsGross wszystkich pozycji (WorkItems) we wszystkich etapach harmonogramu.
        /// Odpowiada CostsGross harmonogramu. Null gdy brak kosztów.
        /// </summary>
        public decimal? TotalWorkItemsCostsGross { get; init; }
    }
}
