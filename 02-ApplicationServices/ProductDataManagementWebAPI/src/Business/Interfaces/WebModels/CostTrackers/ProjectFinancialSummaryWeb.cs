namespace Business.Interfaces.WebModels.CostTrackers
{
    /// <summary>
    /// Zagregowane statystyki finansowe całego projektu.
    /// Nie dziedziczy po TrackerNodeWeb — jest płaskim podsumowaniem najwyższego poziomu.
    ///
    /// TotalBudgetNet          = EstimateBudgetNet + ProjectReserveBudgetNet
    /// EstimateBudgetNet       = suma(CostEstimate.BudgetNet) — z kosztorysów
    /// ProjectReserve          = Project.BudgetNet — rezerwa obciążana kosztami niespiętnymi
    /// TotalCostsNet           = LinkedCostsNet + ScheduleWorkItemCostsNet + AdditionalCostsNet
    /// LinkedCostsNet          = suma(TrackedCost.Net) WHERE CostEstimateItemId != null
    /// ScheduleWorkItemCostsNet = suma(TrackedCost.Net) WHERE WorkScheduleStageWorkId != null AND CostEstimateItemId = null
    /// AdditionalCostsNet      = suma(TrackedCost.Net) WHERE brak powiązania z kosztorysem i harmonogramem
    /// DeviationNet            = TotalBudgetNet - TotalCostsNet  (ujemna = przekroczenie)
    /// DeviationPercent        = DeviationNet / TotalBudgetNet * 100 (null gdy brak budżetu)
    /// CoveredPercent          = TotalCostsNet / TotalBudgetNet * 100 (null gdy brak budżetu)
    /// </summary>
    public sealed record ProjectFinancialSummaryWeb
    {
        public decimal? TotalBudgetNet { get; init; }
        public decimal? TotalBudgetGross { get; init; }
        public decimal? EstimateBudgetNet { get; init; }
        public decimal? EstimateBudgetGross { get; init; }
        public decimal? ProjectReserveBudgetNet { get; init; }
        public decimal? ProjectReserveBudgetGross { get; init; }
        public decimal? TotalCostsNet { get; init; }
        public decimal? TotalCostsGross { get; init; }
        public decimal? LinkedCostsNet { get; init; }
        public decimal? LinkedCostsGross { get; init; }
        public decimal? AdditionalCostsNet { get; init; }
        public decimal? AdditionalCostsGross { get; init; }
        public decimal? DeviationNet { get; init; }
        public decimal? DeviationGross { get; init; }
        public decimal? DeviationPercent { get; init; }
        public decimal? CoveredPercent { get; init; }
        public required bool IsBudgetExceeded { get; init; }
        public required FinancialStatus FinancialStatus { get; init; }
        public required int TotalCostCount { get; init; }
        public required int LinkedCostCount { get; init; }
        public required int AdditionalCostCount { get; init; }
        public required int CostEstimatesCount { get; init; }
        public required int CostEstimatesWithCostsCount { get; init; }
        public required int CostEstimatesOverBudgetCount { get; init; }
        public required int WorkSchedulesCount { get; init; }
    }
}
