using Business.Interfaces.Constants;
using Business.Interfaces.Model;

namespace CQRS.CostEstimates.CopyCostEstimate
{
    public sealed record CopyCostEstimateCommand(
        Guid CostEstimateId,
        List<Guid> TargetProjectIds
    ) : IRequestCommand<List<Guid>>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
