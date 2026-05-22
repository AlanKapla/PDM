using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.AddWorkScheduleStageWorkComment
{
    public sealed class AddWorkScheduleStageWorkCommentCommandValidator : AbstractValidator<AddWorkScheduleStageWorkCommentCommand>
    {
        public AddWorkScheduleStageWorkCommentCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.WorkScheduleStageWorkId).RequiredId();
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("'Content' is required.")
                .MaximumLength(2000).WithMessage("'Content' must not exceed 2000 characters.");
        }
    }
}
