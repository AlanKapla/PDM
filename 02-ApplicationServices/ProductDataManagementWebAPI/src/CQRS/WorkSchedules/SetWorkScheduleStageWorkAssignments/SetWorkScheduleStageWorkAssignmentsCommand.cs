using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkAssignments
{
    public sealed record SetWorkScheduleStageWorkAssignmentsCommand : WorkScheduleStageWorkCommandBase, IRequestCommand<Unit>
    {
        public List<Guid> UserIds { get; init; } = new();

        public override string PermissionCode => PermissionCodes.ProjectSchedule;
    }
}
