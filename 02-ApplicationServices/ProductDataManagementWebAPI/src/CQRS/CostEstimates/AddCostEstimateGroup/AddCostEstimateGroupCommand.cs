using Business.Interfaces.Constants;
using Business.Interfaces.Model;

namespace CQRS.CostEstimates.AddCostEstimateGroup
{
    /// <summary>
    /// Command to add a new group to a cost estimate.
    /// Returns the created group ID.
    /// </summary>
    public sealed record AddCostEstimateGroupCommand(
        Guid CostEstimateId,
        Guid? ParentGroupId,
        int Order
    ) : IRequestCommand<Guid>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
