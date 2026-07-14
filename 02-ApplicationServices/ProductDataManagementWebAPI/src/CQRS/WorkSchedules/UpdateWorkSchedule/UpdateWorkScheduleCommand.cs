using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.UpdateWorkSchedule
{
    public sealed record UpdateWorkScheduleCommand : WorkScheduleCommandBase, IRequestCommand<Unit>
    {
        public string Name { get; init; } = string.Empty;

        public override string PermissionCode => PermissionCodes.ProjectSchedule;
    }
}
