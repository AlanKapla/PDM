using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkAssignments
{
    public sealed class SetWorkScheduleStageWorkAssignmentsCommandValidator : AbstractValidator<SetWorkScheduleStageWorkAssignmentsCommand>
    {
        public SetWorkScheduleStageWorkAssignmentsCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.WorkScheduleStageWorkId).RequiredId();
            RuleFor(x => x.UserIds).NotNull().UniqueIds();
            RuleForEach(x => x.UserIds).NotEmpty();
        }
    }
}
