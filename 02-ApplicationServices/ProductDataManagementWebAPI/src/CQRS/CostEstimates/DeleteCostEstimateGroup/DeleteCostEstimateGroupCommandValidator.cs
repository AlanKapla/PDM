using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostEstimates.DeleteCostEstimateGroup
{
    public sealed class DeleteCostEstimateGroupCommandValidator : AbstractValidator<DeleteCostEstimateGroupCommand>
    {
        public DeleteCostEstimateGroupCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();
            RuleFor(x => x.GroupId).RequiredId();
        }
    }
}
