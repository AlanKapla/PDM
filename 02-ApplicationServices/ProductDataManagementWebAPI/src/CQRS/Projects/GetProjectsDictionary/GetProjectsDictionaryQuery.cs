using Business.Interfaces.Constants;
using Business.Interfaces.Model;

namespace CQRS.Projects.GetProjectsDictionary
{
    public record GetProjectsDictionaryQuery(
        Guid TenantId
    ) : IRequestQuery<Dictionary<Guid, string>>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.TenantView;
        
        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
