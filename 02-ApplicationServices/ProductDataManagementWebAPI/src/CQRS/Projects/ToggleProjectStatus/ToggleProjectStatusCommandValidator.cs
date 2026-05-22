using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.ToggleProjectStatus;

/// <summary>
/// Walidator dla ToggleProjectStatusCommand
/// </summary>
public sealed class ToggleProjectStatusCommandValidator : AbstractValidator<ToggleProjectStatusCommand>
{
    public ToggleProjectStatusCommandValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ProjectId).RequiredId();
    }
}
