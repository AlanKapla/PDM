using FluentValidation;

namespace CQRS.WorkSchedules.DeleteWorkSchedule
{
    public class DeleteWorkScheduleCommandValidator : AbstractValidator<DeleteWorkScheduleCommand>
    {
        public DeleteWorkScheduleCommandValidator()
        {
            RuleFor(x => x.WorkScheduleId)
                .NotEmpty().WithMessage("Work schedule ID is required");
        }
    }
}
