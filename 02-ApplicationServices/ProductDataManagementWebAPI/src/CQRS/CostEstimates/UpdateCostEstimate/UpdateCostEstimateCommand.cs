using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.CostEstimates;
using MediatR;

namespace CQRS.CostEstimates.UpdateCostEstimate
{
    public sealed record UpdateCostEstimateCommand : CostEstimateCommandBase, IRequestCommand<Unit>
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }

        /// <summary>
        /// Grupy kosztorysu z pozycjami (opcjonalne — pełna struktura do zastąpienia)
        /// </summary>
        public List<CostEstimateGroupDto>? Groups { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
