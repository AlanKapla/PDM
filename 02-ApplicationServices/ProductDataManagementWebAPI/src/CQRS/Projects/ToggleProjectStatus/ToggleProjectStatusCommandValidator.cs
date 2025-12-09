using FluentValidation;

namespace CQRS.Projects.ToggleProjectStatus;

/// <summary>
/// Walidator dla ToggleProjectStatusCommand
/// </summary>
public class ToggleProjectStatusCommandValidator : AbstractValidator<ToggleProjectStatusCommand>
{
    public ToggleProjectStatusCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required");

        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("ProjectId is required");
    }
}
