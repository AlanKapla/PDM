using Entities.Models;
using FluentValidation;
using Repositiories.Repository.Interfaces;

namespace CQRS.Tenants.ToggleTenantStatus;

/// <summary>
/// Walidator dla ToggleTenantStatusCommand
/// </summary>
public class ToggleTenantStatusCommandValidator : AbstractValidator<ToggleTenantStatusCommand>
{
    public ToggleTenantStatusCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required");
    }
}
