using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkColorRgb
{
    public sealed class SetWorkScheduleStageWorkColorRgbCommandValidator : AbstractValidator<SetWorkScheduleStageWorkColorRgbCommand>
    {
        public SetWorkScheduleStageWorkColorRgbCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.WorkScheduleStageId).RequiredId();
            RuleFor(x => x.WorkScheduleStageWorkId).RequiredId();
            RuleFor(x => x.ColorRgb)
                .NotEmpty().WithMessage("'ColorRgb' is required.")
                .MaximumLength(20).WithMessage("'ColorRgb' must not exceed 20 characters.")
                .ValidColorRgb();
        }
    }
}
