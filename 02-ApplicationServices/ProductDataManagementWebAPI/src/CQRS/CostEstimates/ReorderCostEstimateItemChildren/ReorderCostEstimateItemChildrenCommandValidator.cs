using Business.Interfaces.WebModels.CostEstimates;
using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostEstimates.ReorderCostEstimateItemChildren
{
    public sealed class ReorderCostEstimateItemChildrenCommandValidator : AbstractValidator<ReorderCostEstimateItemChildrenCommand>
    {
        public ReorderCostEstimateItemChildrenCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();
            RuleFor(x => x.ParentItemId).RequiredId();

            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage("At least one item must be provided.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.ItemId).RequiredId();
                item.RuleFor(x => x.Order)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Order must be non-negative.");
            });

            RuleFor(x => x.Items)
                .Must(items => items.Select(i => i.ItemId).Distinct().Count() == items.Count)
                .WithMessage("Duplicate ItemIds are not allowed.")
                .When(x => x.Items != null && x.Items.Count > 0);

            RuleFor(x => x.Items)
                .Must(items => items.Select(i => i.Order).Distinct().Count() == items.Count)
                .WithMessage("Duplicate Order values are not allowed.")
                .When(x => x.Items != null && x.Items.Count > 0);
        }
    }
}
