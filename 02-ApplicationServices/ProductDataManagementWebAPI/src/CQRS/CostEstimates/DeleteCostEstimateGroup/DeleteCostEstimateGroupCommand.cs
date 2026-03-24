using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.CostEstimates.DeleteCostEstimateGroup
{
    /// <summary>
    /// Command to soft-delete a group from a cost estimate
    /// Deletes the group and all its child groups and items
    /// </summary>
    public sealed record DeleteCostEstimateGroupCommand(
        Guid CostEstimateId,
        Guid GroupId
    ) : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
