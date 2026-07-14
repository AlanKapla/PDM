using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.DeleteProjectCostCategory
{
    public sealed class DeleteProjectCostCategoryCommandValidator : AbstractValidator<DeleteProjectCostCategoryCommand>
    {
        public DeleteProjectCostCategoryCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CategoryId).RequiredId();
        }
    }
}
