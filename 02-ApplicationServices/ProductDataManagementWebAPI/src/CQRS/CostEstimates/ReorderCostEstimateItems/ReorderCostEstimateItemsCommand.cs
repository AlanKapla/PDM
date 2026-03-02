using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostEstimates;
using MediatR;

namespace CQRS.CostEstimates.ReorderCostEstimateItems
{
    /// <summary>
    /// Command to reorder items within a cost estimate group
    /// </summary>
    public sealed record ReorderCostEstimateItemsCommand(
        Guid CostEstimateId,
        Guid GroupId,
        List<ReorderItemDto> Items
    ) : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
