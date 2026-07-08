using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.GetProjectCostCategories
{
    public sealed class GetProjectCostCategoriesQueryValidator : AbstractValidator<GetProjectCostCategoriesQuery>
    {
        public GetProjectCostCategoriesQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
        }
    }
}
