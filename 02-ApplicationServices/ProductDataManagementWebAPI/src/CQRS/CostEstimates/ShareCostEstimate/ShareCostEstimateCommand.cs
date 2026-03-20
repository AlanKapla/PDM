using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.CostEstimates.ShareCostEstimate
{
    public sealed record ShareCostEstimateCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid CostEstimateId { get; init; }
        public List<Guid> ShareWithUserIds { get; init; } = [];

        public string PermissionCode => PermissionCodes.ProjectResourcesShare;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
