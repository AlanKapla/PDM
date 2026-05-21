using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.AddWorkScheduleStage
{
    public sealed class AddWorkScheduleStageCommandValidator : AbstractValidator<AddWorkScheduleStageCommand>
    {
        public AddWorkScheduleStageCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Stage name is required")
                .MaximumLength(255).WithMessage("Stage name cannot exceed 255 characters");
            RuleFor(x => x.Order).NonNegativeOrder();
        }
    }
}
