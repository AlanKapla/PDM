using FluentValidation;

namespace CQRS.WorkSchedules.RenameWorkScheduleStage
{
    public sealed class RenameWorkScheduleStageCommandValidator : AbstractValidator<RenameWorkScheduleStageCommand>
    {
        public RenameWorkScheduleStageCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.WorkScheduleId).NotEmpty();
            RuleFor(x => x.StageId).NotEmpty();
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Stage name is required")
                .MaximumLength(255).WithMessage("Stage name cannot exceed 255 characters");
        }
    }
}
