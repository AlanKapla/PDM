using FluentValidation;

namespace CQRS.Tenants.CreateTenant
{
    public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
    {
        public CreateTenantCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotNull().WithMessage("Tenant name is required")
                .NotEmpty().WithMessage("Tenant name is required")
                .MaximumLength(200).WithMessage("Tenant name cannot exceed 200 characters");
        }
    }
}
