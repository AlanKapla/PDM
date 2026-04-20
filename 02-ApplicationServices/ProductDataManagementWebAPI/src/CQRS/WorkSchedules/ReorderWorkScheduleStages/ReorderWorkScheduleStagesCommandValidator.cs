using FluentValidation;

namespace CQRS.WorkSchedules.ReorderWorkScheduleStages
{
    public sealed class ReorderWorkScheduleStagesCommandValidator : AbstractValidator<ReorderWorkScheduleStagesCommand>
    {
        public ReorderWorkScheduleStagesCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.WorkScheduleId).NotEmpty();
            RuleFor(x => x.OrderedStageIds)
                .NotEmpty().WithMessage("OrderedStageIds must not be empty")
                .Must(HaveNoDuplicates).WithMessage("OrderedStageIds must not contain duplicates");
        }

        private static bool HaveNoDuplicates(List<Guid> ids) =>
            ids == null || ids.Count == ids.Distinct().Count();
    }
}
