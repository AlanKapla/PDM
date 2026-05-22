using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Tenants.ToggleTenantStatus
{
    /// <summary>
    /// Walidator dla ToggleTenantStatusCommand
    /// </summary>
    public sealed class ToggleTenantStatusCommandValidator : AbstractValidator<ToggleTenantStatusCommand>
    {
        public ToggleTenantStatusCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
        }
    }
}
