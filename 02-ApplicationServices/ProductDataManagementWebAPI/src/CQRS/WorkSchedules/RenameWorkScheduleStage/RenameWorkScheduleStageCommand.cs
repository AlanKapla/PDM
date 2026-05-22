using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.RenameWorkScheduleStage
{
    public sealed record RenameWorkScheduleStageCommand : WorkScheduleStageCommandBase, IRequestCommand<Unit>
    {
        public string Name { get; init; } = string.Empty;

        public override string PermissionCode => PermissionCodes.ProjectResourcesWrite;
    }
}
