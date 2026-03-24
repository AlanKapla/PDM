using FluentValidation;

namespace CQRS.CostEstimates.ReorderCostEstimateGroups
{
    public class ReorderCostEstimateGroupsCommandValidator : AbstractValidator<ReorderCostEstimateGroupsCommand>
    {
        public ReorderCostEstimateGroupsCommandValidator()
        {
            RuleFor(x => x.CostEstimateId)
                .NotEmpty().WithMessage("Cost estimate ID is required");

            RuleFor(x => x.Groups)
                .NotNull().WithMessage("Groups collection is required")
                .Must(g => g.Count > 0).WithMessage("At least one group must be provided");

            RuleForEach(x => x.Groups).ChildRules(group =>
            {
                group.RuleFor(g => g.GroupId)
                    .NotEmpty().WithMessage("Group ID is required");

                group.RuleFor(g => g.Order)
                    .GreaterThanOrEqualTo(0).WithMessage("Order must be non-negative");
            });
        }
    }
}
