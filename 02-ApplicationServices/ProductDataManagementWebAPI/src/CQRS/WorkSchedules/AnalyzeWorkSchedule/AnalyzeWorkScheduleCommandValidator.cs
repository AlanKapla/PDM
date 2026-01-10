using FluentValidation;

namespace CQRS.WorkSchedules.AnalyzeWorkSchedule;

/// <summary>
/// Validator for AnalyzeWorkScheduleCommand
/// </summary>
public sealed class AnalyzeWorkScheduleCommandValidator : AbstractValidator<AnalyzeWorkScheduleCommand>
{
    public AnalyzeWorkScheduleCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required");

        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("ProjectId is required");

        RuleFor(x => x.WorkScheduleId)
            .NotEmpty()
            .WithMessage("WorkScheduleId is required");
    }
}
