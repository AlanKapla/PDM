using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Tenants.UpdateTenantMemberRole
{
    /// <summary>
    /// Command to update a tenant member's role using RoleId
    /// </summary>
    public sealed record UpdateTenantMemberRoleCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid UserId { get; init; }
        public required Guid RoleId { get; init; }

        public string PermissionCode => PermissionCodes.TenantMembersManage;

        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
