using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostEstimates.DeleteCostEstimate
{
    /// <summary>
    /// Walidator dla DeleteCostEstimateCommand
    /// </summary>
    public sealed class DeleteCostEstimateCommandValidator : AbstractValidator<DeleteCostEstimateCommand>
    {
        public DeleteCostEstimateCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();
        }
    }
}
