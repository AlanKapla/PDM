using FluentValidation;

namespace CQRS.Tenants.UpdateTenant
{
    public class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
    {
        public UpdateTenantCommandValidator()
        {
            RuleFor(c => c.TenantId)
                .NotEmpty()
                .WithMessage("TenantId is required");

            RuleFor(c => c.Name)
                .NotEmpty()
                .WithMessage("Name is required")
                .MaximumLength(200)
                .WithMessage("Name cannot exceed 200 characters");
        }
    }
}
