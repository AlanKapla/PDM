using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.UpdateProjectCostCategory
{
    public sealed class UpdateProjectCostCategoryCommandValidator : AbstractValidator<UpdateProjectCostCategoryCommand>
    {
        public UpdateProjectCostCategoryCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CategoryId).RequiredId();

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Category name is required.")
                .MaximumLength(100);

            RuleFor(x => x.Code)
                .MaximumLength(20)
                .When(x => x.Code is not null);

            RuleFor(x => x.Order).NonNegativeOrder();

            RuleFor(x => x.Color)
                .ValidColorRgb()
                .When(x => x.Color is not null);
        }
    }
}
