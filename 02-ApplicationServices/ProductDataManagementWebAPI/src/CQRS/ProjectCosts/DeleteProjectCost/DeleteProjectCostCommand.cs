using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using CQRS.Interfaces;
using MediatR;

namespace CQRS.ProjectCosts.DeleteProjectCost
{
    /// <summary>
    /// Command do usunięcia kosztu projektu (soft delete)
    /// </summary>
    public sealed record DeleteProjectCostCommand(
        Guid TenantId,
        Guid ProjectId,
        Guid CostId
    ) : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
