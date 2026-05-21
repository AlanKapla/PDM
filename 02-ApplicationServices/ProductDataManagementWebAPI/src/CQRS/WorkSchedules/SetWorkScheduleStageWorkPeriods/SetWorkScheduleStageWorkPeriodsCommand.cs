using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriods
{
    public sealed record SetWorkScheduleStageWorkPeriodsCommand : WorkScheduleStageWorkCommandBase, IRequestCommand<Unit>
    {
        public List<WorkPeriodDto> Periods { get; init; } = new();

        public override string PermissionCode => PermissionCodes.ProjectResourcesWrite;
    }
}
