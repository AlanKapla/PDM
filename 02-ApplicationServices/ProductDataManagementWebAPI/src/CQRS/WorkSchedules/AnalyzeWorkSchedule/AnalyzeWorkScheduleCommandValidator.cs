using FluentValidation;

namespace CQRS.WorkSchedules.AnalyzeWorkSchedule;

/// <summary>
/// Validator for AnalyzeWorkScheduleCommand
/// </summary>
public sealed class AnalyzeWorkScheduleCommandValidator : AbstractValidator<AnalyzeWorkScheduleCommand>
{
    public AnalyzeWorkScheduleCommandValidator()
    {
        RuleFor(x => x.WorkScheduleId)
            .NotEmpty()
            .WithMessage("WorkScheduleId is required");
    }
}
