using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.CostEstimates.DeleteCostEstimate
{
    /// <summary>
    /// Command do usunięcia kosztorysu (soft delete)
    /// </summary>
    public sealed record DeleteCostEstimateCommand(
        Guid CostEstimateId
    ) : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
