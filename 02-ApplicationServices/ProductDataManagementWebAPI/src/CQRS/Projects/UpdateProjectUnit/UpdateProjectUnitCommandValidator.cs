using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.UpdateProjectUnit
{
    public sealed class UpdateProjectUnitCommandValidator : AbstractValidator<UpdateProjectUnitCommand>
    {
        public UpdateProjectUnitCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.UnitId).RequiredId();

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Unit name is required")
                .MaximumLength(50).WithMessage("Unit name cannot exceed 50 characters");
        }
    }
}
