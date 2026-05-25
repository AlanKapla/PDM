using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Projects.UpdateProjectMemberRole
{
    /// <summary>
    /// Command to update a project member's role using RoleId
    /// </summary>
    public sealed record UpdateProjectMemberRoleCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid UserId { get; init; }
        public required Guid RoleId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectMembersManage;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
