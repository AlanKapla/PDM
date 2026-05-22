using MediatR;

namespace CQRS.Tenants.AcceptTenantInvitation
{
    public sealed record AcceptTenantInvitationCommand : IRequestCommand<Unit>
    {
        public required string Token { get; init; }
    }
}
