using FluentValidation;

namespace CQRS.WorkSchedules.AddWorkScheduleStage
{
    public class AddWorkScheduleStageCommandValidator : AbstractValidator<AddWorkScheduleStageCommand>
    {
        public AddWorkScheduleStageCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");
            RuleFor(x => x.ProjectId).NotEmpty().WithMessage("Project ID is required");
            RuleFor(x => x.WorkScheduleId).NotEmpty().WithMessage("Work schedule ID is required");
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Stage name is required")
                .MaximumLength(255).WithMessage("Stage name cannot exceed 255 characters");
            RuleFor(x => x.Order)
                .GreaterThanOrEqualTo(0).WithMessage("Order must be greater than or equal to 0");
        }
    }
}
