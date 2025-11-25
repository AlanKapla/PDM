using CQRS;
using MediatR;

namespace CQRS.Tenants.AcceptTenantInvitation
{
    public record AcceptTenantInvitationCommand(string Token) : IRequestCommand<Unit>;
}
