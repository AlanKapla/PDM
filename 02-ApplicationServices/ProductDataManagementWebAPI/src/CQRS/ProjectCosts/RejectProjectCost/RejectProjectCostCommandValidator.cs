using CQRS.Extensions;
using FluentValidation;

namespace CQRS.ProjectCosts.RejectProjectCost
{
    public sealed class RejectProjectCostCommandValidator : AbstractValidator<RejectProjectCostCommand>
    {
        public RejectProjectCostCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostId).RequiredId();
        }
    }
}
