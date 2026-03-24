using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.CostEstimates.MoveCostEstimateItem
{
    /// <summary>
    /// Command to move an item from one group to another.
    /// Changes GroupId and places the item at the last position in the target group.
    /// </summary>
    public sealed record MoveCostEstimateItemCommand(
        Guid CostEstimateId,
        Guid ItemId,
        Guid TargetGroupId
    ) : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
