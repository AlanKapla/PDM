using FluentValidation;

namespace CQRS.Tenants.CreateTenant
{
    public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
    {
        public CreateTenantCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tenant name is required")
                .MaximumLength(200).WithMessage("Tenant name cannot exceed 200 characters");
        }
    }
}
