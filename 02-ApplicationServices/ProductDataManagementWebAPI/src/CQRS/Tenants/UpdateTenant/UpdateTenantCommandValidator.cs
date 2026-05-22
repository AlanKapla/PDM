using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Tenants.UpdateTenant
{
    public sealed class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
    {
        public UpdateTenantCommandValidator()
        {
            RuleFor(c => c.TenantId).RequiredId();

            RuleFor(c => c.Name)
                .NotEmpty()
                .WithMessage("Name is required")
                .MaximumLength(200)
                .WithMessage("Name cannot exceed 200 characters");
        }
    }
}
