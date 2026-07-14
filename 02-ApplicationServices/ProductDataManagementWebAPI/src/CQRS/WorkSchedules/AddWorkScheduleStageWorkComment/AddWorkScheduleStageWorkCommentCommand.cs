using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;

namespace CQRS.WorkSchedules.AddWorkScheduleStageWorkComment
{
    public sealed record AddWorkScheduleStageWorkCommentCommand : WorkScheduleStageWorkAssignedCommandBase, IRequestCommand<Guid>
    {
        public string Content { get; init; } = string.Empty;

        public override string PermissionCode => PermissionCodes.ProjectSchedule;
    }
}
