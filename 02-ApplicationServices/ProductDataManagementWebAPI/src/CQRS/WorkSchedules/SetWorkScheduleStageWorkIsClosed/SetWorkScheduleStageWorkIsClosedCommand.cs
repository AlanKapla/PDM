using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkIsClosed
{
    public sealed record SetWorkScheduleStageWorkIsClosedCommand : WorkScheduleStageWorkAssignedCommandBase, IRequestCommand<Unit>
    {
        public bool IsClosed { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectSchedule;
    }
}
