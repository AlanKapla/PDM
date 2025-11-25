using MediatR;

namespace CQRS.Tenants.RemoveTenantMember
{
    public record RemoveTenantMemberCommand(Guid TenantId, Guid UserId) : IRequestCommand<Unit>;
}
