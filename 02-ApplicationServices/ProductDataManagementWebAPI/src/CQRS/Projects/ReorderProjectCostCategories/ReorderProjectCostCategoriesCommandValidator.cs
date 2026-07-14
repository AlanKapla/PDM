using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.ReorderProjectCostCategories
{
    public sealed class ReorderProjectCostCategoriesCommandValidator : AbstractValidator<ReorderProjectCostCategoriesCommand>
    {
        public ReorderProjectCostCategoriesCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CategoryIds).NotEmpty();
        }
    }
}
