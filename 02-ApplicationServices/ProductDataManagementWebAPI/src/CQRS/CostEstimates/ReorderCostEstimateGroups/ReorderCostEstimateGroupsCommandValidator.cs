using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostEstimates.ReorderCostEstimateGroups
{
    public sealed class ReorderCostEstimateGroupsCommandValidator : AbstractValidator<ReorderCostEstimateGroupsCommand>
    {
        public ReorderCostEstimateGroupsCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();

            RuleFor(x => x.Groups)
                .NotNull().WithMessage("Groups collection is required")
                .Must(g => g.Count > 0).WithMessage("At least one group must be provided");

            RuleForEach(x => x.Groups).ChildRules(group =>
            {
                group.RuleFor(g => g.GroupId).RequiredId();
                group.RuleFor(g => g.Order).NonNegativeOrder();
            });
        }
    }
}
