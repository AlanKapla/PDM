using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Tenants.UpdateTenantMemberRole
{
    public sealed record UpdateTenantMemberRoleCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid UserId { get; init; }
        public required bool IsAdmin { get; init; }

        public string PermissionCode => PermissionCodes.TenantMembersManage;

        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
