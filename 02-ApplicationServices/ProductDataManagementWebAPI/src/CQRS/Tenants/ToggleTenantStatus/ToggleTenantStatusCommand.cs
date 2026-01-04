using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using CQRS.Interfaces;
using MediatR;

namespace CQRS.Tenants.ToggleTenantStatus;

/// <summary>
/// Command do zmiany statusu aktywności tenanta
/// </summary>
public record ToggleTenantStatusCommand(Guid TenantId, bool IsActive) : IRequestCommand<Unit>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.TenantStatusManage;
    
    public ResourceRef GetResource() => new(TenantId: TenantId);
}
