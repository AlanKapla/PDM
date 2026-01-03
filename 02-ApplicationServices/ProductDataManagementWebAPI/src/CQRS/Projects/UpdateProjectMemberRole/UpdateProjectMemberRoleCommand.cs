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
    ) : IRequestCommand<Unit>;
}
