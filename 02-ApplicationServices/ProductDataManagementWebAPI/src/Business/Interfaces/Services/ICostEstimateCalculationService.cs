using Entities.Models.CostEstimates;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Serwis do obliczania sum w kosztorysach (TotalNet, TotalGross, TotalVat)
    /// Nie pobiera danych - przyjmuje załadowane obiekty jako parametry
    /// </summary>
    public interface ICostEstimateCalculationService
    {
        /// <summary>
        /// Przelicza sumy dla całego kosztorysu (wszystkie grupy i pozycje)
        /// Aktualizuje TotalNet, TotalGross, TotalVat w kosztorysie i grupach
        /// </summary>
        /// <param name="costEstimate">Załadowany kosztorys z AllGroups → Items → FieldValues</param>
        void RecalculateCostEstimate(CostEstimate costEstimate);
        
        /// <summary>
        /// Przelicza sumy dla pojedynczej grupy i jej podgrup rekursywnie
        /// </summary>
        /// <param name="group">Grupa do przeliczenia</param>
        /// <param name="allGroups">Wszystkie grupy w kosztorysie (dla hierarchii)</param>
        /// <returns>Tuple (TotalNet, TotalGross, TotalVat) dla grupy</returns>
        (decimal Net, decimal Gross, decimal Vat) RecalculateGroup(CostEstimateGroup group, List<CostEstimateGroup> allGroups);
        
        /// <summary>
        /// Oblicza wartości dla pojedynczej pozycji na podstawie jej pól
        /// Zwraca (Net, Gross, Vat)
        /// </summary>
        /// <param name="item">Pozycja z załadowanymi FieldValues</param>
        (decimal? Net, decimal? Gross, decimal? Vat) CalculateItemValues(CostEstimateItem item);
    }
}
