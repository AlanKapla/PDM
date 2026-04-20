using FluentValidation;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkIsClosed
{
    public sealed class SetWorkScheduleStageWorkIsClosedCommandValidator : AbstractValidator<SetWorkScheduleStageWorkIsClosedCommand>
    {
        public SetWorkScheduleStageWorkIsClosedCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.WorkScheduleId).NotEmpty();
            RuleFor(x => x.WorkScheduleStageWorkId).NotEmpty();
        }
    }
}
