using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostTrackers.DeleteTrackedCost
{
    public sealed class DeleteTrackedCostCommandValidator : AbstractValidator<DeleteTrackedCostCommand>
    {
        public DeleteTrackedCostCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostId).RequiredId();
        }
    }
}
