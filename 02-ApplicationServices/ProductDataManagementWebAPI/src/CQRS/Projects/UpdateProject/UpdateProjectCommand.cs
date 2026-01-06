using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;

namespace CQRS.Projects.UpdateProject
{
    public sealed record UpdateProjectCommand(
        Guid TenantId,
        Guid ProjectId,
        string Name
    ) : IRequestCommand<ProjectDetailsWeb>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectEdit;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
