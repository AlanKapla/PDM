using Business.Interfaces.WebModels.AI;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Serwis generowania podglądu kosztorysu przez Azure OpenAI.
    /// NIE zapisuje niczego do bazy danych — zwraca tylko AICostEstimatePreviewWeb.
    /// Generuje kosztorys z podstawowymi polami systemowymi (Name, Quantity, Unit, ceny, VAT).
    /// </summary>
    public interface ICostEstimateAIGeneratorService
    {
        /// <summary>
        /// Generuje podgląd struktury kosztorysu na podstawie opisu inwestycji.
        /// Używa podstawowych pól systemowych (9 standardowych pól dla pozycji).
        /// W razie błędu walidacji pól (type-mismatch, zakres) wykonuje max 1 retry z feedbackiem do AI.
        /// </summary>
        Task<AICostEstimatePreviewWeb> GeneratePreviewAsync(
            AICostEstimateRequestWeb request,
            CancellationToken cancellationToken);
    }
}
