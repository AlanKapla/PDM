using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.GenerateScheduleFromEstimateAI
{
    public sealed class GenerateScheduleFromEstimateAICommandValidator : AbstractValidator<GenerateScheduleFromEstimateAICommand>
    {
        public GenerateScheduleFromEstimateAICommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();

            RuleFor(x => x.OverallStartDate)
                .NotEmpty()
                .WithMessage("Overall start date is required.");

            RuleFor(x => x.OverallEndDate)
                .NotEmpty()
                .WithMessage("Overall end date is required.");

            RuleFor(x => x)
                .Must(x => x.OverallEndDate > x.OverallStartDate)
                .WithMessage("Overall end date must be after overall start date.")
                .Must(x => (x.OverallEndDate - x.OverallStartDate).TotalDays >= 1)
                .WithMessage("The overall time frame must be at least 1 day.");
        }
    }
}
