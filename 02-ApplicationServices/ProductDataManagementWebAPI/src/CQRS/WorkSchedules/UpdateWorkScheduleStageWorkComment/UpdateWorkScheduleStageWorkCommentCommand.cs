using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.UpdateWorkScheduleStageWorkComment
{
    public sealed record UpdateWorkScheduleStageWorkCommentCommand : WorkScheduleCommandBase, IRequestCommand<Unit>
    {
        public Guid CommentId { get; init; }
        public string Content { get; init; } = string.Empty;

        public override string PermissionCode => PermissionCodes.ProjectSchedule;
    }
}
