using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Entities.Models.CostEstimates;

namespace CQRS.CostEstimates.AddCostEstimateItem
{
    /// <summary>
    /// Command to add a new item to a cost estimate group.
    /// Returns the created item ID.
    /// </summary>
    public sealed record AddCostEstimateItemCommand(
        Guid CostEstimateId,
        Guid GroupId,
        Guid? ParentItemId,
        ItemRelationType RelationType,
        int Order
    ) : IRequestCommand<Guid>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
