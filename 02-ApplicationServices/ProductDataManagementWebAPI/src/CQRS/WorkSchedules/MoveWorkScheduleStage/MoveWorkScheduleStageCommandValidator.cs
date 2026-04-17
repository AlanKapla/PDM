using FluentValidation;

namespace CQRS.WorkSchedules.MoveWorkScheduleStage
{
    public sealed class MoveWorkScheduleStageCommandValidator : AbstractValidator<MoveWorkScheduleStageCommand>
    {
        public MoveWorkScheduleStageCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.WorkScheduleId).NotEmpty();
            RuleFor(x => x.StageId).NotEmpty();
            RuleFor(x => x)
                .Must(c => c.ParentStageId == null || c.ParentStageId != c.StageId)
                .WithName("ParentStageId")
                .WithMessage("A stage cannot be its own parent.");
        }
    }
}
