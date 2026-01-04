using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using CQRS.Interfaces;

namespace CQRS.Projects.GetProjectMembers
{
    public record GetProjectMembersQuery(
        Guid TenantId,
        Guid ProjectId
    ) : IRequestQuery<IEnumerable<ProjectMemberWeb>>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectMembersView;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
