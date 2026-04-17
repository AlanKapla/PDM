using FluentValidation;
using CQRS.WorkSchedules.Shared;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriods
{
    public sealed class SetWorkScheduleStageWorkPeriodsCommandValidator : AbstractValidator<SetWorkScheduleStageWorkPeriodsCommand>
    {
        public SetWorkScheduleStageWorkPeriodsCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.WorkScheduleId).NotEmpty();
            RuleFor(x => x.WorkScheduleStageWorkId).NotEmpty();
            RuleFor(x => x.Periods).NotNull();

            RuleForEach(x => x.Periods).ChildRules(period =>
            {
                period.RuleFor(p => p.StartDate).NotEmpty();
                period.RuleFor(p => p.EndDate).NotEmpty();
                period.RuleFor(p => p)
                    .Must(p => p.EndDate > p.StartDate)
                    .WithName("Period")
                    .WithMessage("Data rozpocz\u0119cia musi by\u0107 wcze\u015bniejsza ni\u017c data zako\u0144czenia.");
            });

            RuleFor(x => x.Periods)
                .Must(HaveNoOverlappingPeriods)
                .WithMessage("Okresy nie mog\u0105 si\u0119 nak\u0142ada\u0107.");
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
