using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostEstimates.GetCostEstimateDetails
{
    public sealed class GetCostEstimateDetailsQueryValidator
        : AbstractValidator<GetCostEstimateDetailsQuery>
    {
        public GetCostEstimateDetailsQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();
        }
    }
}
