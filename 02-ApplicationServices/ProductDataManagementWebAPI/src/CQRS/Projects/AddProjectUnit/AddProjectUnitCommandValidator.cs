using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.AddProjectUnit
{
    public sealed class AddProjectUnitCommandValidator : AbstractValidator<AddProjectUnitCommand>
    {
        public AddProjectUnitCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Unit name is required")
                .MaximumLength(50).WithMessage("Unit name cannot exceed 50 characters");
        }
    }
}
