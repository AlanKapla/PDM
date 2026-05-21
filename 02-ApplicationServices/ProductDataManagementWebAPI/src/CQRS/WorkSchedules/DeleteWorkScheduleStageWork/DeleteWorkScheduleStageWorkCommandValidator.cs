using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.DeleteWorkScheduleStageWork
{
    public sealed class DeleteWorkScheduleStageWorkCommandValidator : AbstractValidator<DeleteWorkScheduleStageWorkCommand>
    {
        public DeleteWorkScheduleStageWorkCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.WorkScheduleStageId).RequiredId();
            RuleFor(x => x.WorkScheduleStageWorkId).RequiredId();
        }
    }
}
