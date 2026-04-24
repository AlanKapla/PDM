namespace Business.Interfaces.WebModels.CostTrackers
{
    /// <summary>
    /// Korzeń hierarchii węzłów dashboardu.
    /// Zawiera oba statusy — finansowy i czasowy.
    /// TimelineStatus = NoSchedule dla węzłów bez powiązania z harmonogramem.
    /// </summary>
    public abstract record StatusedNodeWeb
    {
        /// <summary>Status finansowy: stan budżetu względem kosztów.</summary>
        public required FinancialStatus FinancialStatus { get; init; }

        /// <summary>Status czasowy: stan realizacji względem harmonogramu.</summary>
        public required TimelineStatus TimelineStatus { get; init; }
    }
}
