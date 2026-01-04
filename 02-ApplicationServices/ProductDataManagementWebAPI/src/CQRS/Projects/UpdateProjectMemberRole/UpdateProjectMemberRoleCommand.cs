using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using CQRS.Interfaces;
using MediatR;

namespace CQRS.Projects.UpdateProjectMemberRole
{
    /// <summary>
    /// Command to update a project member's role using RoleId
    /// </summary>
    public record UpdateProjectMemberRoleCommand(
        Guid TenantId,
        Guid ProjectId,
        Guid UserId,
        Guid RoleId  // Changed from ProjectRole enum to Guid RoleId
    ) : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectMembersManage;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
