using CQRS.CostTrackers.Shared;
using FluentValidation;

namespace CQRS.CostTrackers.CreateTrackedCost
{
    public sealed class CreateTrackedCostCommandValidator
        : TrackedCostCommandBaseValidator<CreateTrackedCostCommand>
    {
        public CreateTrackedCostCommandValidator()
        {
            RuleFor(x => x.CostEstimateItemId)
                .NotEqual(Guid.Empty)
                .When(x => x.CostEstimateItemId.HasValue)
                .WithMessage("'CostEstimateItemId' must not be an empty Guid.");

            RuleFor(x => x.WorkScheduleStageWorkId)
                .NotEqual(Guid.Empty)
                .When(x => x.WorkScheduleStageWorkId.HasValue)
                .WithMessage("'WorkScheduleStageWorkId' must not be an empty Guid.");

            RuleFor(x => x)
                .Must(x => x.Net.HasValue || x.Gross.HasValue)
                .WithMessage("At least one of 'Net' or 'Gross' must be provided.");
        }
    }
}
