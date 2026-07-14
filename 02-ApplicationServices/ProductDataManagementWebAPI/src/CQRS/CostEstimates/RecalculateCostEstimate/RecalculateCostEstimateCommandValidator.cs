using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostEstimates.RecalculateCostEstimate
{
    public sealed class RecalculateCostEstimateCommandValidator : AbstractValidator<RecalculateCostEstimateCommand>
    {
        public RecalculateCostEstimateCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();
        }
    }
}
