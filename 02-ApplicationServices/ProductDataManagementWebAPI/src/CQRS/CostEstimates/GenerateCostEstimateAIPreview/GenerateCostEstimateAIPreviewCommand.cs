using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.AI;

namespace CQRS.CostEstimates.GenerateCostEstimateAIPreview
{
    /// <summary>
    /// Generuje podgląd struktury kosztorysu przez AI na podstawie opisu inwestycji.
    /// Nie zapisuje niczego do bazy danych — zwraca AICostEstimatePreviewWeb.
    /// Użytkownik przegląda podgląd i zatwierdza przez CreateCostEstimateFromAIPreview.
    /// </summary>
    public sealed record GenerateCostEstimateAIPreviewCommand : CostEstimateRequestBase, IRequestCommand<AICostEstimatePreviewWeb>
    {
        /// <summary>Dane wejściowe od użytkownika (opis inwestycji, szablon, budżet itp.)</summary>
        public AICostEstimateRequestWeb Request { get; init; } = default!;

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
