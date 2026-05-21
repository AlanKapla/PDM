using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.MoveWorkScheduleStageWork
{
    public sealed record MoveWorkScheduleStageWorkCommand : WorkScheduleStageWorkCommandBase, IRequestCommand<Unit>
    {
        public Guid TargetStageId { get; init; }
        public int TargetOrder { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectResourcesWrite;
    }
}
