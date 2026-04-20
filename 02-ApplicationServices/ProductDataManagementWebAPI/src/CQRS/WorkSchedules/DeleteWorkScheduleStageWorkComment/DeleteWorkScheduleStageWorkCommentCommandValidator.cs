using FluentValidation;

namespace CQRS.WorkSchedules.DeleteWorkScheduleStageWorkComment
{
    public sealed class DeleteWorkScheduleStageWorkCommentCommandValidator : AbstractValidator<DeleteWorkScheduleStageWorkCommentCommand>
    {
        public DeleteWorkScheduleStageWorkCommentCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.WorkScheduleId).NotEmpty();
            RuleFor(x => x.CommentId).NotEmpty();
        }
    }
}
