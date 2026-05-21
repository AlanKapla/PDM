using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.RenameWorkScheduleStageWork
{
    public sealed record RenameWorkScheduleStageWorkCommand : WorkScheduleStageCommandBase, IRequestCommand<Unit>
    {
        public Guid WorkScheduleStageWorkId { get; init; }
        public string Name { get; init; } = string.Empty;

        public override string PermissionCode => PermissionCodes.ProjectResourcesWrite;
    }
}
