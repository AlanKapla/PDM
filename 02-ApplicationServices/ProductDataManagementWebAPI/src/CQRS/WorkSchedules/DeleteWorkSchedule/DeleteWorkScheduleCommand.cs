using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.DeleteWorkSchedule
{
    public sealed record DeleteWorkScheduleCommand : WorkScheduleCommandBase, IRequestCommand<Unit>
    {
        public override string PermissionCode => PermissionCodes.ProjectSchedule;
    }
}
