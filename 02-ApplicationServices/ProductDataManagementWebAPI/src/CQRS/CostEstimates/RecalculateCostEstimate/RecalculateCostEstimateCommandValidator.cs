using FluentValidation;

namespace CQRS.CostEstimates.RecalculateCostEstimate
{
    public class RecalculateCostEstimateCommandValidator : AbstractValidator<RecalculateCostEstimateCommand>
    {
        public RecalculateCostEstimateCommandValidator()
        {
            RuleFor(x => x.CostEstimateId)
                .NotEmpty().WithMessage("Cost estimate ID is required");
        }
    }
}
