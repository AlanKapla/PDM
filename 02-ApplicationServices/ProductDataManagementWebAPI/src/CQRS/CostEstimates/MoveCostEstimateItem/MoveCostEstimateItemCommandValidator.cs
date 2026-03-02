using FluentValidation;

namespace CQRS.CostEstimates.MoveCostEstimateItem
{
    public class MoveCostEstimateItemCommandValidator : AbstractValidator<MoveCostEstimateItemCommand>
    {
        public MoveCostEstimateItemCommandValidator()
        {
            RuleFor(x => x.CostEstimateId)
                .NotEmpty().WithMessage("Cost estimate ID is required");

            RuleFor(x => x.ItemId)
                .NotEmpty().WithMessage("Item ID is required");

            RuleFor(x => x.TargetGroupId)
                .NotEmpty().WithMessage("Target group ID is required");
        }
    }
}
