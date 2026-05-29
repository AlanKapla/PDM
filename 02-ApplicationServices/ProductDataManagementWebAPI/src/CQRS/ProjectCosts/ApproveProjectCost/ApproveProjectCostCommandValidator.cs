using CQRS.Extensions;
using FluentValidation;

namespace CQRS.ProjectCosts.ApproveProjectCost
{
    public sealed class ApproveProjectCostCommandValidator : AbstractValidator<ApproveProjectCostCommand>
    {
        public ApproveProjectCostCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostId).RequiredId();
        }
    }
}
