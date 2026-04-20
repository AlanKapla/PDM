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
                .NotEmpty().WithMessage("Pole ColorRgb jest wymagane")
                .MaximumLength(20).WithMessage("Pole ColorRgb nie może mieć więcej niż 20 znaków")
                .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Pole ColorRgb musi być poprawnym kolorem HEX w formacie #RRGGBB");
        }
    }
}
