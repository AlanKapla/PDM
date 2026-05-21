using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostEstimates.ReorderCostEstimateItems
{
    public sealed class ReorderCostEstimateItemsCommandValidator : AbstractValidator<ReorderCostEstimateItemsCommand>
    {
        public ReorderCostEstimateItemsCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();
            RuleFor(x => x.GroupId).RequiredId();

            RuleFor(x => x.Items)
                .NotNull().WithMessage("Items collection is required")
                .Must(i => i.Count > 0).WithMessage("At least one item must be provided");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ItemId).RequiredId();
                item.RuleFor(i => i.Order).NonNegativeOrder();
            });
        }
    }
}
