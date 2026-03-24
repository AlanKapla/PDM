using FluentValidation;

namespace CQRS.CostEstimates.AddCostEstimateGroup
{
    public class AddCostEstimateGroupCommandValidator : AbstractValidator<AddCostEstimateGroupCommand>
    {
        public AddCostEstimateGroupCommandValidator()
        {
            RuleFor(x => x.CostEstimateId)
                .NotEmpty().WithMessage("Cost estimate ID is required");

            RuleFor(x => x.Order)
                .GreaterThanOrEqualTo(0).WithMessage("Order must be non-negative");
        }
    }
}
