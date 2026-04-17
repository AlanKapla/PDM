using FluentValidation;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkAssignments
{
    public sealed class SetWorkScheduleStageWorkAssignmentsCommandValidator : AbstractValidator<SetWorkScheduleStageWorkAssignmentsCommand>
    {
        public SetWorkScheduleStageWorkAssignmentsCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.WorkScheduleId).NotEmpty();
            RuleFor(x => x.WorkScheduleStageWorkId).NotEmpty();
            RuleFor(x => x.UserIds).NotNull();
            RuleForEach(x => x.UserIds).NotEmpty();
        }
    }
}
