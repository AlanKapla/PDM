using Business.Interfaces.Constants;
using MediatR;

namespace CQRS.CostEstimates.DeleteCostEstimate
{
    /// <summary>
    /// Command do usunięcia kosztorysu (soft delete).
    /// </summary>
    public sealed record DeleteCostEstimateCommand : CostEstimateCommandBase, IRequestCommand<Unit>
    {
        public override string PermissionCode => PermissionCodes.ProjectResourcesWrite;
    }
}
