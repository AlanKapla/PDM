using MediatR;

namespace CQRS.Projects.AcceptProjectInvitation;

public sealed record AcceptProjectInvitationCommand : IRequestCommand<Unit>
{
    public required string Token { get; init; }
}
