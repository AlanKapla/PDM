using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.MoveWorkScheduleStage
{
    public sealed record MoveWorkScheduleStageCommand : WorkScheduleStageCommandBase, IRequestCommand<Unit>
    {
        public Guid? ParentStageId { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectSchedule;
    }
}
