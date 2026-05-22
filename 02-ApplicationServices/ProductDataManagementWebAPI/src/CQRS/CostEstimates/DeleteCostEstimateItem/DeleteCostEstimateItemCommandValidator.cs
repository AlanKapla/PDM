using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostEstimates.DeleteCostEstimateItem
{
    public sealed class DeleteCostEstimateItemCommandValidator : AbstractValidator<DeleteCostEstimateItemCommand>
    {
        public DeleteCostEstimateItemCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();
            RuleFor(x => x.ItemId).RequiredId();
        }
    }
}
