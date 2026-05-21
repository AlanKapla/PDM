using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostEstimates.GetCostEstimates
{
    public sealed class GetCostEstimatesQueryValidator
        : AbstractValidator<GetCostEstimatesQuery>
    {
        public GetCostEstimatesQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.Scope).IsInEnum();
        }
    }
}
