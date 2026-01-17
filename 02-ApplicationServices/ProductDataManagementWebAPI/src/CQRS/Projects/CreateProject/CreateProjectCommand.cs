using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;

namespace CQRS.Projects.CreateProject
{
    public record CreateProjectCommand(Guid TenantId, string Name) : IRequestCommand<ProjectDetailsWeb>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.TenantProjectCreate;
        
        public ResourceRef GetResource()
        {
            return new ResourceRef(TenantId: TenantId);
        }
    }
}
