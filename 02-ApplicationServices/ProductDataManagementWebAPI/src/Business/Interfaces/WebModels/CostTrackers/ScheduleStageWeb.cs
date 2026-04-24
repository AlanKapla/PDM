namespace Business.Interfaces.WebModels.CostTrackers
{
    /// <summary>
    /// Etap harmonogramu (WorkScheduleStage).
    /// Dziedziczy po TrackerNodeWithTimelineWeb.
    /// BudgetNet null gdy etap nie ma spięcia z kosztorysem.
    ///
    /// BudgetNet = suma(CostEstimateItemWorkScheduleStageWorkLink.BudgetNet) w etapie + suma(ChildStages.BudgetNet)
    /// CostsNet  = suma(CostEstimateItemWorkScheduleStageWorkLink.CostsNet) + suma(ChildStages.CostsNet)
    /// Timeline  = agregat WorkScheduleStageWork w etapie i podetapach (never null)
    /// </summary>
    public sealed record ScheduleStageWeb : TrackerNodeWithTimelineWeb
    {
        public required Guid StageId { get; init; }
        public required string StageName { get; init; }
        public required int Order { get; init; }
        public required int TotalWorkItemsCount { get; init; }
        public required int CompletedWorkItemsCount { get; init; }
        public required int DelayedWorkItemsCount { get; init; }
        public required List<WorkItemLinkWeb> WorkItems { get; init; }
        public required List<ScheduleStageWeb> ChildStages { get; init; }

        /// <summary>
        /// Suma CostsNet bezpośrednich pozycji (WorkItems) w tym etapie.
        /// Nie uwzględnia podetapów. Null gdy żaden workItem nie ma kosztu.
        /// </summary>
        public decimal? TotalWorkItemsCostsNet { get; init; }

        /// <summary>
        /// Suma CostsGross bezpośrednich pozycji (WorkItems) w tym etapie.
        /// Nie uwzględnia podetapów. Null gdy żaden workItem nie ma kosztu.
        /// </summary>
        public decimal? TotalWorkItemsCostsGross { get; init; }
    }
}
