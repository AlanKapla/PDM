using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;

namespace CQRS.WorkSchedules.GetUserAssignedWorks
{
    public sealed record GetUserAssignedWorksQuery(
        Guid TenantId
    ) : IRequestQuery<List<UserAssignedWorksGroupedWeb>>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.TenantView;
        
        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
