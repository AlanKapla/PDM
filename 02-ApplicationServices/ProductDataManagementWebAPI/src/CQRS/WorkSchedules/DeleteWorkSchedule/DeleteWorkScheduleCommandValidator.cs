using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.DeleteWorkSchedule
{
    public sealed class DeleteWorkScheduleCommandValidator : AbstractValidator<DeleteWorkScheduleCommand>
    {
        public DeleteWorkScheduleCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
        }
    }
}
