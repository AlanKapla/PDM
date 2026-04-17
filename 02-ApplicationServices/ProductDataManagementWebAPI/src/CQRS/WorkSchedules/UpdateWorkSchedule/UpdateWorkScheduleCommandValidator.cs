using FluentValidation;

namespace CQRS.WorkSchedules.UpdateWorkSchedule
{
    public class UpdateWorkScheduleCommandValidator : AbstractValidator<UpdateWorkScheduleCommand>
    {
        public UpdateWorkScheduleCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.WorkScheduleId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        }
    }
}
