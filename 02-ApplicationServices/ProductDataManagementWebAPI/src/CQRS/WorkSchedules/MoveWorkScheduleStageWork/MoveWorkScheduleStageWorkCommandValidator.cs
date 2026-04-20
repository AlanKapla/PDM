using FluentValidation;

namespace CQRS.WorkSchedules.MoveWorkScheduleStageWork
{
    public sealed class MoveWorkScheduleStageWorkCommandValidator : AbstractValidator<MoveWorkScheduleStageWorkCommand>
    {
        public MoveWorkScheduleStageWorkCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.WorkScheduleId).NotEmpty();
            RuleFor(x => x.WorkScheduleStageWorkId).NotEmpty();
            RuleFor(x => x.TargetStageId).NotEmpty();
            RuleFor(x => x.TargetOrder).GreaterThanOrEqualTo(0);
        }
    }
}
