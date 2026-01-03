using MediatR;

namespace CQRS.Tenants.UpdateTenantMemberRole
{
    /// <summary>
    /// Command to update a tenant member's role using RoleId
    /// </summary>
    public record UpdateTenantMemberRoleCommand(
        Guid TenantId,
        Guid UserId,
        Guid RoleId  // Changed from TenantRole enum to Guid RoleId
    ) : IRequestCommand<Unit>;
}
