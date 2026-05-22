using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.MoveWorkScheduleStageWork
{
    public sealed class MoveWorkScheduleStageWorkCommandValidator : AbstractValidator<MoveWorkScheduleStageWorkCommand>
    {
        public MoveWorkScheduleStageWorkCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.WorkScheduleStageWorkId).RequiredId();
            RuleFor(x => x.TargetStageId).RequiredId();
            RuleFor(x => x.TargetOrder).NonNegativeOrder();
        }
    }
}
