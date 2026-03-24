using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.CostEstimates.DeleteCostEstimateItem
{
    /// <summary>
    /// Command to soft-delete an item from a cost estimate
    /// Also deletes all child items (options, components)
    /// </summary>
    public sealed record DeleteCostEstimateItemCommand(
        Guid CostEstimateId,
        Guid ItemId
    ) : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
