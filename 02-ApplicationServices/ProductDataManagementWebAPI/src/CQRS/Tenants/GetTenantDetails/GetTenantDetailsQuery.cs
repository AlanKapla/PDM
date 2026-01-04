using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using CQRS.Interfaces;

namespace CQRS.Tenants.GetTenantDetails
{
    public sealed record GetTenantDetailsQuery(
        Guid TenantId
    ) : IRequestQuery<TenantDetailsWeb>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.TenantEdit;
        
        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
