using FluentValidation;

namespace CQRS.CostEstimates.ReorderCostEstimateItems
{
    public class ReorderCostEstimateItemsCommandValidator : AbstractValidator<ReorderCostEstimateItemsCommand>
    {
        public ReorderCostEstimateItemsCommandValidator()
        {
            RuleFor(x => x.CostEstimateId)
                .NotEmpty().WithMessage("Cost estimate ID is required");

            RuleFor(x => x.GroupId)
                .NotEmpty().WithMessage("Group ID is required");

            RuleFor(x => x.Items)
                .NotNull().WithMessage("Items collection is required")
                .Must(i => i.Count > 0).WithMessage("At least one item must be provided");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ItemId)
                    .NotEmpty().WithMessage("Item ID is required");

                item.RuleFor(i => i.Order)
                    .GreaterThanOrEqualTo(0).WithMessage("Order must be non-negative");
            });
        }
    }
}
