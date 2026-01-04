using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using CQRS.Interfaces;

namespace CQRS.Projects.CreateProject
{
    public record CreateProjectCommand(string Name) : IRequestCommand<ProjectDetailsWeb>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.TenantProjectCreate;
        
        public ResourceRef GetResource()
        {
            // CreateProject używa ActiveTenantId, więc TenantId = Guid.Empty (AccessService to obsłuży)
            return new ResourceRef(TenantId: Guid.Empty);
        }
    }
}
