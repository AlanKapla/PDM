using FluentValidation;

namespace CQRS.WorkSchedules.AddWorkScheduleStageWorkComment
{
    public sealed class AddWorkScheduleStageWorkCommentCommandValidator : AbstractValidator<AddWorkScheduleStageWorkCommentCommand>
    {
        public AddWorkScheduleStageWorkCommentCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.WorkScheduleId).NotEmpty();
            RuleFor(x => x.WorkScheduleStageWorkId).NotEmpty();
            RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
        }
    }
}
