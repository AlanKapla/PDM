using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostEstimates.SetItemIsSelected
{
    public sealed class SetItemIsSelectedCommandValidator
        : AbstractValidator<SetItemIsSelectedCommand>
    {
        public SetItemIsSelectedCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();
            RuleFor(x => x.ItemId).RequiredId();
        }
    }
}
