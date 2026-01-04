using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using CQRS;
using CQRS.Interfaces;

namespace CQRS.Projects.GetTenantProjects
{
    public record GetTenantProjectsQuery(
        Guid TenantId
    ) : IRequestQuery<IEnumerable<ProjectDetailsWeb>>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.TenantView;
        
        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
