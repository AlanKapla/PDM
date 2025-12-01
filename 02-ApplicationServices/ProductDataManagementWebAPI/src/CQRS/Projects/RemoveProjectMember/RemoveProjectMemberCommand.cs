using MediatR;

namespace CQRS.Projects.RemoveProjectMember
{
    public record RemoveProjectMemberCommand(
        Guid TenantId,
        Guid ProjectId,
        Guid UserId
    ) : IRequestCommand<Unit>;
}
