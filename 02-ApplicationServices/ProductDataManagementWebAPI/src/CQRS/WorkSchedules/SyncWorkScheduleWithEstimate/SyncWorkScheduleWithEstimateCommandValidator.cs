using FluentValidation;

namespace CQRS.WorkSchedules.SyncWorkScheduleWithEstimate
{
    public class SyncWorkScheduleWithEstimateCommandValidator : AbstractValidator<SyncWorkScheduleWithEstimateCommand>
    {
        public SyncWorkScheduleWithEstimateCommandValidator()
        {
            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");

            RuleFor(x => x.WorkScheduleId)
                .NotEmpty().WithMessage("WorkScheduleId is required");
        }
    }
}
