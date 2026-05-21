using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.SyncWorkScheduleWithEstimate
{
    public sealed class SyncWorkScheduleWithEstimateCommandValidator : AbstractValidator<SyncWorkScheduleWithEstimateCommand>
    {
        public SyncWorkScheduleWithEstimateCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
        }
    }
}
