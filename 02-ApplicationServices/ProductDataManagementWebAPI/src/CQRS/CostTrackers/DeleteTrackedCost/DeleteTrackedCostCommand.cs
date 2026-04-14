using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.CostTrackers.DeleteTrackedCost
{
    /// <summary>
    /// Command do usunięcia kosztu z trackera (soft-delete)
    /// </summary>
    public sealed record DeleteTrackedCostCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public required Guid CostId { get; init; }
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
