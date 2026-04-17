using FluentValidation;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkColorRgb
{
    public sealed class SetWorkScheduleStageWorkColorRgbCommandValidator : AbstractValidator<SetWorkScheduleStageWorkColorRgbCommand>
    {
        public SetWorkScheduleStageWorkColorRgbCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.WorkScheduleId).NotEmpty();
            RuleFor(x => x.WorkScheduleStageId).NotEmpty();
            RuleFor(x => x.WorkScheduleStageWorkId).NotEmpty();
            RuleFor(x => x.ColorRgb)
                .NotEmpty().WithMessage("ColorRgb is required")
                .MaximumLength(20).WithMessage("ColorRgb cannot exceed 20 characters")
                .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("ColorRgb must be a valid hex color in format #RRGGBB");
        }
    }
}
