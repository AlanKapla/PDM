using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.DeleteWorkScheduleStageWork
{
    public sealed record DeleteWorkScheduleStageWorkCommand : WorkScheduleStageCommandBase, IRequestCommand<Unit>
    {
        public Guid WorkScheduleStageWorkId { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectSchedule;
    }
}
