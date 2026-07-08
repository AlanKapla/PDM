using Business.Interfaces.WebModels.CostEstimates;
using Business.Interfaces.WebModels.CostTrackers;
using Entities.Models.CostEstimates;
using Entities.Models.Costs;
using Entities.Models.CostTrackers;

namespace Business.Interfaces.Services
{
    public interface ICostTrackerFinancialService
    {
        /// <summary>
        /// Uzupełnia brakujące pola finansowe na podstawie podanej kombinacji wartości.
        /// Obsługiwane kombinacje: net+gross, net+vatRate, net+vatAmount, gross+vatRate, samo net, sam gross.
        /// Nigdy nie rzuca wyjątku.
        /// </summary>
        (decimal? Net, decimal? Gross) Calculate(decimal? net, decimal? gross);

        /// <summary>
        /// Oblicza status finansowy węzła na podstawie budżetu i rzeczywistych kosztów.
        /// </summary>
        FinancialStatus ComputeItemStatus(decimal? budgetNet, decimal? costsNet, int costsCount);

        /// <summary>
        /// Oblicza status finansowy węzła agregatowego (etap, harmonogram) na podstawie budżetu i kosztów.
        /// Nie wymaga liczby kosztów — NoCosts gdy costsNet = null.
        /// </summary>
        FinancialStatus ComputeFinancialStatus(decimal? budgetNet, decimal? costsNet);

        /// <summary>
        /// Agreguje dane ze wszystkich CostEstimateSummaryWeb i kosztów projektowych w jeden widok projektu.
        /// Pola BudgetNet i BudgetGross są addytywne względem budżetów z kosztorysów.
        /// </summary>
        CostTrackerSummaryWeb ComputeProjectSummary(
            IReadOnlyCollection<CostEstimateSummaryWeb> costEstimateSummaries,
            ProjectAdditionalCostsWeb projectAdditionalCosts,
            decimal? budgetNet,
            decimal? budgetGross);

        /// <summary>
        /// Oblicza summary budżetowe wyłącznie na podstawie kosztów dodatkowych projektu i budżetu trackera.
        /// </summary>
        CostTrackerBudgetSummary ComputeBudgetSummary(
            ProjectAdditionalCostsWeb projectAdditionalCosts,
            decimal? budgetNet,
            decimal? budgetGross);

        /// <summary>
        /// Oblicza wszystkie wskaźniki dla jednego kosztorysu: sumy, odchylenia, coverage, liczniki pozycji wg statusu.
        /// </summary>
        CostEstimateSummaryWeb ComputeEstimateSummary(
            CostEstimate costEstimate,
            IReadOnlyCollection<CostEstimateItem> budgetItems,
            ILookup<Guid, BaseCost> costsByItemId,
            decimal? additionalCostsNet,
            decimal? additionalCostsGross,
            int additionalCostsCount);
    }
}
