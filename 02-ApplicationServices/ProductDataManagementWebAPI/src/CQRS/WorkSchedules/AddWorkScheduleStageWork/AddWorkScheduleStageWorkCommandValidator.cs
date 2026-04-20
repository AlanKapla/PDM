using FluentValidation;

namespace CQRS.WorkSchedules.AddWorkScheduleStageWork
{
    public sealed class AddWorkScheduleStageWorkCommandValidator : AbstractValidator<AddWorkScheduleStageWorkCommand>
    {
        public AddWorkScheduleStageWorkCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.WorkScheduleId).NotEmpty();
            RuleFor(x => x.WorkScheduleStageId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
            RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ColorRgb)
                .NotEmpty()
                .Matches(@"^#[0-9A-Fa-f]{6}$")
                .WithMessage("ColorRgb must be a valid hex color in format #RRGGBB.");
        }
    }
}
