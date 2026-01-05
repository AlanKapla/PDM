using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.ProjectCosts;
using CQRS.Interfaces;

namespace CQRS.ProjectCosts.GetProjectCosts
{
    /// <summary>
    /// Query do pobierania kosztów projektu według zakresu (All, Mine, Shared)
    /// </summary>
    public sealed record GetProjectCostsQuery(
        Guid TenantId,
        Guid ProjectId,
        ResourceScope Scope
    ) : IRequestQuery<IEnumerable<ProjectCostListItemWeb>>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectView;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);

        public ResourceScope? GetResourceScope() => Scope;
    }
}
