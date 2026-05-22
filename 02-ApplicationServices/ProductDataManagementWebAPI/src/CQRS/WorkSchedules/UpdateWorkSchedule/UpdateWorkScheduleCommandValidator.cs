using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.UpdateWorkSchedule
{
    public sealed class UpdateWorkScheduleCommandValidator : AbstractValidator<UpdateWorkScheduleCommand>
    {
        public UpdateWorkScheduleCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        }
    }
}
