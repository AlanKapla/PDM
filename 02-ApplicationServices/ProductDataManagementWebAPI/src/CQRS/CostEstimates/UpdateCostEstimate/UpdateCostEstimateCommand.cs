using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.CostEstimates.UpdateCostEstimate
{
    public sealed record UpdateCostEstimateCommand(
        string Name,
        string? Description
    ) : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid CostEstimateId { get; init; }
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
