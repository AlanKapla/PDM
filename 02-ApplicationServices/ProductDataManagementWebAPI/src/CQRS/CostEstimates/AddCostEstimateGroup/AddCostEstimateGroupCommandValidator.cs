using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostEstimates.AddCostEstimateGroup
{
    public sealed class AddCostEstimateGroupCommandValidator : AbstractValidator<AddCostEstimateGroupCommand>
    {
        public AddCostEstimateGroupCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();
            RuleFor(x => x.Order).NonNegativeOrder();
        }
    }
}
