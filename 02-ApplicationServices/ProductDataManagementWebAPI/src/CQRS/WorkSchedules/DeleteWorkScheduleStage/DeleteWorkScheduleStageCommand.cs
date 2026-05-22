using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.DeleteWorkScheduleStage
{
    public sealed record DeleteWorkScheduleStageCommand : WorkScheduleStageCommandBase, IRequestCommand<Unit>
    {
        public override string PermissionCode => PermissionCodes.ProjectResourcesWrite;
    }
}
