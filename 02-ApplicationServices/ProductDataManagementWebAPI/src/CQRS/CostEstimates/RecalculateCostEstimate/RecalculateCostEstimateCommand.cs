using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.CostEstimates.RecalculateCostEstimate
{
    /// <summary>
    /// Command to recalculate all totals (Net, Gross, VAT) for a cost estimate.
    /// Recalculates item values, group totals and cost estimate totals.
    /// </summary>
    public sealed record RecalculateCostEstimateCommand(
        Guid CostEstimateId
    ) : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
