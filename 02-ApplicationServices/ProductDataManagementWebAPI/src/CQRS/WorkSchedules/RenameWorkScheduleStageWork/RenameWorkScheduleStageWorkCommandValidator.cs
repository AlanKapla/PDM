using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.RenameWorkScheduleStageWork
{
    public sealed class RenameWorkScheduleStageWorkCommandValidator : AbstractValidator<RenameWorkScheduleStageWorkCommand>
    {
        public RenameWorkScheduleStageWorkCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.WorkScheduleStageId).RequiredId();
            RuleFor(x => x.WorkScheduleStageWorkId).RequiredId();
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Work name is required")
                .MaximumLength(255).WithMessage("Work name cannot exceed 255 characters");
        }
    }
}
