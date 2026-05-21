using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.DeleteWorkScheduleStage
{
    public sealed class DeleteWorkScheduleStageCommandValidator : AbstractValidator<DeleteWorkScheduleStageCommand>
    {
        public DeleteWorkScheduleStageCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.WorkScheduleStageId).RequiredId();
        }
    }
}
