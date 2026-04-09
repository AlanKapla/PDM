using FluentValidation;

namespace CQRS.CostTrackers.UpdateTrackedCost
{
    public sealed class UpdateTrackedCostCommandValidator : AbstractValidator<UpdateTrackedCostCommand>
    {
        public UpdateTrackedCostCommandValidator()
        {
            RuleFor(x => x.CostId)
                .NotEmpty().WithMessage("Cost ID is required.");

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("Tenant ID is required.");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Project ID is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(300).WithMessage("Name cannot exceed 300 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");

            RuleFor(x => x.Contractor)
                .MaximumLength(300).WithMessage("Contractor cannot exceed 300 characters.");
        }
    }
}
