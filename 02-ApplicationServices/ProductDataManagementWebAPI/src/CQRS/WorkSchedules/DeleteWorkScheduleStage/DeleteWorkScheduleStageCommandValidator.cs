using FluentValidation;

namespace CQRS.WorkSchedules.DeleteWorkScheduleStage
{
    public class DeleteWorkScheduleStageCommandValidator : AbstractValidator<DeleteWorkScheduleStageCommand>
    {
        public DeleteWorkScheduleStageCommandValidator()
        {
            RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");
            RuleFor(x => x.ProjectId).NotEmpty().WithMessage("Project ID is required");
            RuleFor(x => x.WorkScheduleId).NotEmpty().WithMessage("Work schedule ID is required");
            RuleFor(x => x.StageId).NotEmpty().WithMessage("Stage ID is required");
        }
    }
}
