using FluentValidation;

namespace CQRS.WorkSchedules.UpdateWorkScheduleStageWorkComment
{
    public sealed class UpdateWorkScheduleStageWorkCommentCommandValidator : AbstractValidator<UpdateWorkScheduleStageWorkCommentCommand>
    {
        public UpdateWorkScheduleStageWorkCommentCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.WorkScheduleId).NotEmpty();
            RuleFor(x => x.CommentId).NotEmpty();
            RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
        }
    }
}
