using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostEstimates;
using MediatR;

namespace CQRS.CostEstimates.ReorderCostEstimateGroups
{
    /// <summary>
    /// Command to reorder groups within a cost estimate
    /// </summary>
    public sealed record ReorderCostEstimateGroupsCommand(
        Guid CostEstimateId,
        List<ReorderGroupDto> Groups
    ) : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
