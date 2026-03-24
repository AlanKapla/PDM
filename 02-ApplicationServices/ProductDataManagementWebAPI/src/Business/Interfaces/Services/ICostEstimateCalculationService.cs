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
        /// Oblicza tylko te pola, które są zdefiniowane w szablonie (CalculatedFieldDefinitions)
        /// </summary>
        /// <param name="costEstimate">Załadowany kosztorys z Template.CalculatedFieldDefinitions, AllGroups → Items → FieldValues</param>
        void RecalculateCostEstimate(CostEstimate costEstimate);
       
    }
}
