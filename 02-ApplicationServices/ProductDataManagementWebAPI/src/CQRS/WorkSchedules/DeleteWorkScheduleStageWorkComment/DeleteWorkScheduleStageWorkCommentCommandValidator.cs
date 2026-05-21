using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.DeleteWorkScheduleStageWorkComment
{
    public sealed class DeleteWorkScheduleStageWorkCommentCommandValidator : AbstractValidator<DeleteWorkScheduleStageWorkCommentCommand>
    {
        public DeleteWorkScheduleStageWorkCommentCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.CommentId).RequiredId();
        }
    }
}
