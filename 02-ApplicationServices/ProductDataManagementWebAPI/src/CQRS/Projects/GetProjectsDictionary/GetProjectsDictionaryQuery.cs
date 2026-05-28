using Business.Interfaces.Constants;
using Business.Interfaces.Model;

namespace CQRS.Projects.GetProjectsDictionary
{
    public sealed record GetProjectsDictionaryQuery : IRequestQuery<Dictionary<Guid, string>>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }

        public string PermissionCode => PermissionCodes.TenantView;

        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
