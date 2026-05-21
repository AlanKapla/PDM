using CQRS.CostTrackers.Shared;
using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostTrackers.UpdateTrackedCost
{
    public sealed class UpdateTrackedCostCommandValidator
        : TrackedCostCommandBaseValidator<UpdateTrackedCostCommand>
    {
        public UpdateTrackedCostCommandValidator()
        {
            RuleFor(x => x.CostId).RequiredId();

            RuleFor(x => x.ExistingAttachmentIds!.ToList())
                .UniqueIds()
                .When(x => x.ExistingAttachmentIds is not null);
        }
    }
}
