namespace Business.Interfaces.WebModels.CostTrackers
{
    /// <summary>
    /// Zbiorcze podsumowanie kosztów wszystkich harmonogramów projektu.
    /// TotalSchedulesCostsNet  = suma TotalWorkItemsCostsNet ze wszystkich harmonogramów.
    /// TotalSchedulesCostsGross = suma TotalWorkItemsCostsGross ze wszystkich harmonogramów.
    /// SchedulesWithCostsCount  = liczba harmonogramów z TotalWorkItemsCostsNet > 0.
    /// SchedulesWithoutCostsCount = harmonogramy bez żadnych kosztów.
    /// </summary>
    public sealed record ScheduleCostSummaryWeb
    {
        public required decimal TotalSchedulesCostsNet { get; init; }
        public required decimal TotalSchedulesCostsGross { get; init; }
        public required int SchedulesWithCostsCount { get; init; }
        public required int SchedulesWithoutCostsCount { get; init; }
    }
}
