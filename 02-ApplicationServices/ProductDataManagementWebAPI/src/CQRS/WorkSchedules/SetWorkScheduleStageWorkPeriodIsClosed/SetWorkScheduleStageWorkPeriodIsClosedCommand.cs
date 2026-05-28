using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriodIsClosed
{
    public sealed record SetWorkScheduleStageWorkPeriodIsClosedCommand : WorkScheduleStageWorkAssignedCommandBase, IRequestCommand<Unit>
    {
        public Guid PeriodId { get; init; }
        public bool IsClosed { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectSchedule;
    }
}
