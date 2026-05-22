using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.ReorderWorkScheduleStages
{
    public sealed class ReorderWorkScheduleStagesCommandValidator : AbstractValidator<ReorderWorkScheduleStagesCommand>
    {
        public ReorderWorkScheduleStagesCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.OrderedStageIds)
                .NotEmpty().WithMessage("OrderedStageIds must not be empty")
                .UniqueIds();
        }
    }
}
