using FluentValidation;

namespace CQRS.CostTrackers.UpdateTrackerBudget
{
    public sealed class UpdateTrackerBudgetCommandValidator : AbstractValidator<UpdateTrackerBudgetCommand>
    {
        public UpdateTrackerBudgetCommandValidator()
        {
            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("Tenant ID is required.");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Project ID is required.");

            RuleFor(x => x.BudgetNet)
                .GreaterThanOrEqualTo(0).When(x => x.BudgetNet.HasValue)
                .WithMessage("BudgetNet cannot be negative.");

            RuleFor(x => x.BudgetGross)
                .GreaterThanOrEqualTo(0).When(x => x.BudgetGross.HasValue)
                .WithMessage("BudgetGross cannot be negative.");
        }
    }
}
