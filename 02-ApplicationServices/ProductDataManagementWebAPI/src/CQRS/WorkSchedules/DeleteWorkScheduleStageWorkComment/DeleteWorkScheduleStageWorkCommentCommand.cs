using Business.Interfaces.Constants;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.DeleteWorkScheduleStageWorkComment
{
    public sealed record DeleteWorkScheduleStageWorkCommentCommand : WorkScheduleCommandBase, IRequestCommand<Unit>
    {
        public Guid CommentId { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectResourcesWrite;
    }
}
