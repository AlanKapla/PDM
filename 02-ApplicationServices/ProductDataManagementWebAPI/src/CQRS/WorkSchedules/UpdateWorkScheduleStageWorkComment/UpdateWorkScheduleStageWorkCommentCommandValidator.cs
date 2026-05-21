using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.UpdateWorkScheduleStageWorkComment
{
    public sealed class UpdateWorkScheduleStageWorkCommentCommandValidator : AbstractValidator<UpdateWorkScheduleStageWorkCommentCommand>
    {
        public UpdateWorkScheduleStageWorkCommentCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.CommentId).RequiredId();
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("'Content' is required.")
                .MaximumLength(2000).WithMessage("'Content' must not exceed 2000 characters.");
        }
    }
}
