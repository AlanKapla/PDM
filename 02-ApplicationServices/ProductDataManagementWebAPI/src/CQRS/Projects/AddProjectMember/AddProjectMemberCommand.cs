using MediatR;

namespace CQRS.Projects.AddProjectMember
{
    public record AddProjectMemberCommand(
        Guid TenantId,
        Guid ProjectId,
        Guid UserId
    ) : IRequestCommand<Unit>;
}
