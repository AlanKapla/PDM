namespace Business.Interfaces.WebModels.CostTrackers
{
    /// <summary>
    /// Status finansowy węzła — stan budżetu względem kosztów.
    /// Obliczany na każdym poziomie drzewa (pozycja, grupa, kosztorys, projekt).
    /// </summary>
    public enum FinancialStatus
    {
        /// <summary>Brak zdefiniowanego budżetu (BudgetNet = null).</summary>
        NoBudget   = 0,
        /// <summary>Budżet zdefiniowany, brak zarejestrowanych kosztów.</summary>
        NoCosts    = 1,
        /// <summary>Koszty > 0 i <= 85% budżetu.</summary>
        InProgress = 2,
        /// <summary>Koszty > 85% i <= 100% budżetu.</summary>
        NearLimit  = 3,
        /// <summary>Koszty > budżet.</summary>
        OverBudget = 4,
    }
}
