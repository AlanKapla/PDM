using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkColorRgb
{
    public sealed record SetWorkScheduleStageWorkColorRgbCommand : WorkScheduleStageCommandBase, IRequestCommand<Unit>
    {
        public Guid WorkScheduleStageWorkId { get; init; }
        public string ColorRgb { get; init; } = string.Empty;

        public override string PermissionCode => PermissionCodes.ProjectSchedule;
    }
}
