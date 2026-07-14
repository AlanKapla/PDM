using CQRS.Extensions;
using FluentValidation;

namespace CQRS.ProjectCosts.SubmitProjectCostForApproval
{
    public sealed class SubmitProjectCostForApprovalCommandValidator : AbstractValidator<SubmitProjectCostForApprovalCommand>
    {
        public SubmitProjectCostForApprovalCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostId).RequiredId();
        }
    }
}
