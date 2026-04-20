using FluentValidation;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriodIsClosed
{
    public sealed class SetWorkScheduleStageWorkPeriodIsClosedCommandValidator : AbstractValidator<SetWorkScheduleStageWorkPeriodIsClosedCommand>
    {
        public SetWorkScheduleStageWorkPeriodIsClosedCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.WorkScheduleId).NotEmpty();
            RuleFor(x => x.WorkScheduleStageWorkId).NotEmpty();
            RuleFor(x => x.PeriodId).NotEmpty();
        }
    }
}
