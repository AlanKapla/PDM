using Entities.Enums;
using MediatR;

namespace CQRS.Tenants.UpdateTenantMemberRole
{
    public record UpdateTenantMemberRoleCommand(
        Guid TenantId,
        Guid UserId,
        TenantRole Role
    ) : IRequestCommand<Unit>;
}
