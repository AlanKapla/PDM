using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using CQRS.Interfaces;

namespace CQRS.Tenants.UpdateTenant
{
    public sealed record UpdateTenantCommand(Guid TenantId, string Name) : IRequestCommand<TenantDetailsWeb>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.TenantEdit;
        
        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
