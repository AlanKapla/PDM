using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.Shared;

namespace CQRS.WorkSchedules.GetWorkScheduleAssignableAssignees
{
    public sealed record GetWorkScheduleAssignableAssigneesQuery
        : WorkScheduleRequestBase, IRequestQuery<WorkScheduleAssignableAssigneesWeb>
    {
        public override string PermissionCode => PermissionCodes.ProjectSchedule;
    }
}
