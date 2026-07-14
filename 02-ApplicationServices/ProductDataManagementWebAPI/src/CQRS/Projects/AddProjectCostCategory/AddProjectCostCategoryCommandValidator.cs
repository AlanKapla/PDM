using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.AddProjectCostCategory
{
    public sealed class AddProjectCostCategoryCommandValidator : AbstractValidator<AddProjectCostCategoryCommand>
    {
        public AddProjectCostCategoryCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Category name is required.")
                .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.");

            RuleFor(x => x.Code)
                .MaximumLength(20)
                .When(x => x.Code is not null);

            RuleFor(x => x.Color)
                .ValidColorRgb()
                .When(x => x.Color is not null);
        }
    }
}
