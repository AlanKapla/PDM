using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkIsClosed
{
    public sealed class SetWorkScheduleStageWorkIsClosedCommandValidator : AbstractValidator<SetWorkScheduleStageWorkIsClosedCommand>
    {
        public SetWorkScheduleStageWorkIsClosedCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.WorkScheduleStageWorkId).RequiredId();
        }
    }
}
