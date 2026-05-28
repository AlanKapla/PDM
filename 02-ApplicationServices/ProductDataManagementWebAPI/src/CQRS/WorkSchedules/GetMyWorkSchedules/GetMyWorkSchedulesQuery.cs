using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.Shared;

namespace CQRS.WorkSchedules.GetMyWorkSchedules
{
    public sealed record GetMyWorkSchedulesQuery : WorkScheduleRequestBase, IRequestQuery<List<MyWorkSchedulesTenantDto>>
    {
        public override string PermissionCode => PermissionCodes.ProjectSchedule;
    }
}
