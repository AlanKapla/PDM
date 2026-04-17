using FluentValidation;

namespace CQRS.WorkSchedules.RenameWorkScheduleStageWork
{
    public sealed class RenameWorkScheduleStageWorkCommandValidator : AbstractValidator<RenameWorkScheduleStageWorkCommand>
    {
        public RenameWorkScheduleStageWorkCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.WorkScheduleId).NotEmpty();
            RuleFor(x => x.WorkScheduleStageId).NotEmpty();
            RuleFor(x => x.WorkScheduleStageWorkId).NotEmpty();
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Work name is required")
                .MaximumLength(255).WithMessage("Work name cannot exceed 255 characters");
        }
    }
}
