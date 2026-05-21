using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.ReorderWorkScheduleStageWorks
{
    public sealed class ReorderWorkScheduleStageWorksCommandValidator : AbstractValidator<ReorderWorkScheduleStageWorksCommand>
    {
        public ReorderWorkScheduleStageWorksCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.WorkScheduleStageId).RequiredId();
            RuleFor(x => x.OrderedWorkIds)
                .NotEmpty().WithMessage("OrderedWorkIds must not be empty")
                .UniqueIds();
        }
    }
}
