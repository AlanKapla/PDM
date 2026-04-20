using FluentValidation;

namespace CQRS.WorkSchedules.DeleteWorkScheduleStageWork
{
    public sealed class DeleteWorkScheduleStageWorkCommandValidator : AbstractValidator<DeleteWorkScheduleStageWorkCommand>
    {
        public DeleteWorkScheduleStageWorkCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.WorkScheduleId).NotEmpty();
            RuleFor(x => x.WorkScheduleStageId).NotEmpty();
            RuleFor(x => x.WorkScheduleStageWorkId).NotEmpty();
        }
    }
}
