using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.SyncWorkScheduleWithEstimate
{
    public sealed record SyncWorkScheduleWithEstimateCommand : WorkScheduleCommandBase, IRequestCommand<Unit>
    {
        public override string PermissionCode => PermissionCodes.ProjectSchedule;
    }
}
