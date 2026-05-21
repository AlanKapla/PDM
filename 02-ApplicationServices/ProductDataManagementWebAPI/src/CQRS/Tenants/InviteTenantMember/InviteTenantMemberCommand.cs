using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Tenants.InviteTenantMember
{
    public sealed record InviteTenantMemberCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required string Email { get; init; }

        public string PermissionCode => PermissionCodes.TenantMembersManage;

        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
