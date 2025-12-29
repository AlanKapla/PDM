using Entities.Enums;
using MediatR;

namespace CQRS.Projects.UpdateProjectMemberRole
{
    public record UpdateProjectMemberRoleCommand(
        Guid TenantId,
        Guid ProjectId,
        Guid UserId,
        ProjectRole Role
    ) : IRequestCommand<Unit>;
}
