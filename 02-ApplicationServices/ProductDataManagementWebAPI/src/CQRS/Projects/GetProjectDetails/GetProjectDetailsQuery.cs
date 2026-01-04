using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using CQRS.Interfaces;
using MediatR;

namespace CQRS.Projects.GetProjectDetails
{
    public record GetProjectDetailsQuery(Guid TenantId, Guid ProjectId) : IRequestQuery<ProjectDetailsWeb>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectView;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
