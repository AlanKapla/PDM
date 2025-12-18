using FluentValidation;

namespace CQRS.CostEstimates.DeleteCostEstimate
{
    /// <summary>
    /// Walidator dla DeleteCostEstimateCommand
    /// </summary>
    public class DeleteCostEstimateCommandValidator : AbstractValidator<DeleteCostEstimateCommand>
    {
        public DeleteCostEstimateCommandValidator()
        {
            RuleFor(x => x.CostEstimateId)
                .NotEmpty().WithMessage("Cost estimate ID is required");
        }
    }
}
