using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.UpdateProjectBudget
{
    public sealed class UpdateProjectBudgetCommandValidator : AbstractValidator<UpdateProjectBudgetCommand>
    {
        public UpdateProjectBudgetCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();

            RuleFor(x => x.BudgetNet)
                .GreaterThanOrEqualTo(0).When(x => x.BudgetNet.HasValue)
                .WithMessage("'BudgetNet' cannot be negative.");

            RuleFor(x => x.BudgetGross)
                .GreaterThanOrEqualTo(0).When(x => x.BudgetGross.HasValue)
                .WithMessage("'BudgetGross' cannot be negative.");
        }
    }
}
