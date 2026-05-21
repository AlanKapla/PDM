using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriodIsClosed
{
    public sealed class SetWorkScheduleStageWorkPeriodIsClosedCommandValidator : AbstractValidator<SetWorkScheduleStageWorkPeriodIsClosedCommand>
    {
        public SetWorkScheduleStageWorkPeriodIsClosedCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.WorkScheduleStageWorkId).RequiredId();
            RuleFor(x => x.PeriodId).RequiredId();
        }
    }
}
