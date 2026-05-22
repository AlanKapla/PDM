using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.MoveWorkScheduleStage
{
    public sealed class MoveWorkScheduleStageCommandValidator : AbstractValidator<MoveWorkScheduleStageCommand>
    {
        public MoveWorkScheduleStageCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.WorkScheduleStageId).RequiredId();
            RuleFor(x => x)
                .Must(c => c.ParentStageId == null || c.ParentStageId != c.WorkScheduleStageId)
                .WithName("ParentStageId")
                .WithMessage("A stage cannot be its own parent.");
        }
    }
}
