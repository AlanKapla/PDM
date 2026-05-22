using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.Shared;

namespace CQRS.WorkSchedules.GetWorkSchedule
{
    /// <summary>
    /// Query to retrieve a work schedule by its ID with full details
    /// </summary>
    public sealed record GetWorkScheduleQuery : WorkScheduleCommandBase, IRequestQuery<WorkScheduleDetailsWeb>
    {
        public override string PermissionCode => PermissionCodes.ProjectResourcesReadSingle;
    }
}
