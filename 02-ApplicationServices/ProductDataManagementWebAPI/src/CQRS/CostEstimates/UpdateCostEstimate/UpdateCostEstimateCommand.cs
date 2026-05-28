using Business.Interfaces.Constants;
using MediatR;

namespace CQRS.CostEstimates.UpdateCostEstimate
{
    public sealed record UpdateCostEstimateCommand : CostEstimateCommandBase, IRequestCommand<Unit>
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
