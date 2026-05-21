using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.ProjectCosts;

namespace CQRS.ProjectCosts.GetProjectCosts
{
    /// <summary>
    /// Query do pobierania kosztów projektu według zakresu (All, Mine, Shared)
    /// </summary>
    public sealed record GetProjectCostsQuery : IRequestQuery<IEnumerable<ProjectCostListItemWeb>>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required ResourceScope Scope { get; init; }

        public string PermissionCode => PermissionCodes.ProjectView;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);

        public ResourceScope? GetResourceScope() => Scope;
    }
}
