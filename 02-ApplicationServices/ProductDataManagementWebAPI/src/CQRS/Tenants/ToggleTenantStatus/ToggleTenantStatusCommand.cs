using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Tenants.ToggleTenantStatus
{
    /// <summary>
    /// Command do zmiany statusu aktywności tenanta
    /// </summary>
    public sealed record ToggleTenantStatusCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required bool IsActive { get; init; }

        public string PermissionCode => PermissionCodes.TenantStatusManage;

        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
