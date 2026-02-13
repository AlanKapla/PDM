using FluentValidation;

namespace CQRS.CostEstimates.UpdateCostEstimate
{
    /// <summary>
    /// Walidator dla UpdateCostEstimateCommand
    /// </summary>
    public class UpdateCostEstimateCommandValidator : AbstractValidator<UpdateCostEstimateCommand>
    {
        public UpdateCostEstimateCommandValidator()
        {
            RuleFor(x => x.CostEstimateId)
                .NotEmpty().WithMessage("Cost estimate ID is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Cost estimate name is required")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

            RuleFor(x => x.RootGroups)
                .NotNull().WithMessage("Root groups collection is required");
        }
    }
}
