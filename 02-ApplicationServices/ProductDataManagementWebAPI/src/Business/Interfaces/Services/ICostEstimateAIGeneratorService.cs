using Business.Interfaces.WebModels.AI;
using Entities.Models.CostEstimateTemplates;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Serwis generowania podglądu kosztorysu przez Azure OpenAI.
    /// NIE zapisuje niczego do bazy danych — zwraca tylko AICostEstimatePreviewWeb.
    /// </summary>
    public interface ICostEstimateAIGeneratorService
    {
        /// <summary>
        /// Generuje podgląd struktury kosztorysu na podstawie opisu inwestycji i szablonu.
        /// W razie błędu walidacji pól (type-mismatch, zakres) wykonuje max 1 retry z feedbackiem do AI.
        /// </summary>
        Task<AICostEstimatePreviewWeb> GeneratePreviewAsync(
            AICostEstimateRequestWeb request,
            CostEstimateTemplate template,
            CancellationToken cancellationToken);
    }
}
