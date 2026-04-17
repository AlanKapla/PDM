using FluentValidation;

namespace CQRS.WorkSchedules.ReorderWorkScheduleStageWorks
{
    public sealed class ReorderWorkScheduleStageWorksCommandValidator : AbstractValidator<ReorderWorkScheduleStageWorksCommand>
    {
        public ReorderWorkScheduleStageWorksCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.WorkScheduleId).NotEmpty();
            RuleFor(x => x.WorkScheduleStageId).NotEmpty();
            RuleFor(x => x.OrderedWorkIds)
                .NotEmpty().WithMessage("OrderedWorkIds must not be empty")
                .Must(ids => ids == null || ids.Count == ids.Distinct().Count())
                .WithMessage("OrderedWorkIds must not contain duplicates");
        }
    }
}
