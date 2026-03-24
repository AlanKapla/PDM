using FluentValidation;

namespace CQRS.CostEstimates.DeleteCostEstimateItem
{
    public class DeleteCostEstimateItemCommandValidator : AbstractValidator<DeleteCostEstimateItemCommand>
    {
        public DeleteCostEstimateItemCommandValidator()
        {
            RuleFor(x => x.CostEstimateId)
                .NotEmpty().WithMessage("Cost estimate ID is required");

            RuleFor(x => x.ItemId)
                .NotEmpty().WithMessage("Item ID is required");
        }
    }
}
