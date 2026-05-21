using CQRS.Extensions;
using CQRS.WorkSchedules.Shared;
using FluentValidation;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriods
{
    public sealed class SetWorkScheduleStageWorkPeriodsCommandValidator : AbstractValidator<SetWorkScheduleStageWorkPeriodsCommand>
    {
        public SetWorkScheduleStageWorkPeriodsCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.WorkScheduleStageWorkId).RequiredId();
            RuleFor(x => x.Periods).NotNull();

            RuleForEach(x => x.Periods).ChildRules(period =>
            {
                period.RuleFor(p => p.StartDate).NotEmpty();
                period.RuleFor(p => p.EndDate).NotEmpty();
                period.RuleFor(p => p)
                    .Must(p => p.EndDate > p.StartDate)
                    .WithName("Period")
                    .WithMessage("Start date must be earlier than end date.");
            });

            RuleFor(x => x.Periods)
                .Must(HaveNoOverlappingPeriods)
                .WithMessage("Periods must not overlap.");
        }

        private static bool HaveNoOverlappingPeriods(List<WorkPeriodDto> periods)
        {
            if (periods == null || periods.Count < 2)
            {
                return true;
            }

            List<WorkPeriodDto> sorted = periods.OrderBy(p => p.StartDate).ToList();

            for (int i = 0; i < sorted.Count - 1; i++)
            {
                if (sorted[i].EndDate > sorted[i + 1].StartDate)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
