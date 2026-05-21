using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;

namespace CQRS.Projects.GetTenantProjects
{
    public sealed record GetTenantProjectsQuery : IRequestQuery<IEnumerable<ProjectDetailsWeb>>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }

        public string PermissionCode => PermissionCodes.TenantView;

        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
