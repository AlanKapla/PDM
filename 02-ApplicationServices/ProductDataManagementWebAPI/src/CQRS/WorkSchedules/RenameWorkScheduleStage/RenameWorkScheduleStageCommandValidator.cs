using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.RenameWorkScheduleStage
{
    public sealed class RenameWorkScheduleStageCommandValidator : AbstractValidator<RenameWorkScheduleStageCommand>
    {
        public RenameWorkScheduleStageCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.WorkScheduleStageId).RequiredId();
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Stage name is required")
                .MaximumLength(255).WithMessage("Stage name cannot exceed 255 characters");
        }
    }
}
