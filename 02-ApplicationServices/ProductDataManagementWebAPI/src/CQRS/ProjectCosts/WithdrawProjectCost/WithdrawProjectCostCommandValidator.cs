using CQRS.Extensions;
using FluentValidation;

namespace CQRS.ProjectCosts.WithdrawProjectCost
{
    public sealed class WithdrawProjectCostCommandValidator : AbstractValidator<WithdrawProjectCostCommand>
    {
        public WithdrawProjectCostCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostId).RequiredId();
        }
    }
}
