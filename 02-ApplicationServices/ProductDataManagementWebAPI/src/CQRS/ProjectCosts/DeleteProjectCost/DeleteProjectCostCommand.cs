using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.ProjectCosts.DeleteProjectCost
{
    /// <summary>
    /// Command do usunięcia kosztu projektu (soft delete)
    /// </summary>
    public sealed record DeleteProjectCostCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required Guid CostId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
