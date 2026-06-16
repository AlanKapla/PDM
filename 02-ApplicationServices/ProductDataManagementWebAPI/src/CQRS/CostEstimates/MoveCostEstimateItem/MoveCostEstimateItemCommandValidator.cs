using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostEstimates.MoveCostEstimateItem
{
    public sealed class MoveCostEstimateItemCommandValidator : AbstractValidator<MoveCostEstimateItemCommand>
    {
        public MoveCostEstimateItemCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();
            RuleFor(x => x.ItemId).RequiredId();
            RuleFor(x => x.TargetGroupId).RequiredId();
        }
    }
}
