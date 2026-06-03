using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.AI;

namespace CQRS.CostEstimates.CreateCostEstimateFromAIPreview
{
    /// <summary>
    /// Zapisuje kosztorys zatwierdzony przez użytkownika z podglądu AI.
    /// Atomowo tworzy: CostEstimate → Groups → Items → FieldValues.
    /// Zwraca ID nowo utworzonego kosztorysu.
    /// </summary>
    public sealed record CreateCostEstimateFromAIPreviewCommand : CostEstimateRequestBase, IRequestCommand<Guid>
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public AICostEstimatePreviewWeb Preview { get; init; } = default!;

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
