using FluentValidation;

namespace CQRS.CostTrackers.DeleteTrackedCost
{
    public sealed class DeleteTrackedCostCommandValidator : AbstractValidator<DeleteTrackedCostCommand>
    {
        public DeleteTrackedCostCommandValidator()
        {
            RuleFor(x => x.CostId)
                .NotEmpty().WithMessage("Cost ID is required.");

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("Tenant ID is required.");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Project ID is required.");
        }
    }
}
