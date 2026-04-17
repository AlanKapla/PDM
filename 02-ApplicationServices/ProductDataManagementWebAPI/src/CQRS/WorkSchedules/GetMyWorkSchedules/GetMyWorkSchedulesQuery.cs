using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;

namespace CQRS.WorkSchedules.GetMyWorkSchedules
{
    public sealed record GetMyWorkSchedulesQuery(
        Guid TenantId,
        Guid ProjectId
    ) : IRequestQuery<List<MyWorkSchedulesTenantDto>>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectView;
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
