using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.GetWorkSchedule
{
    public sealed class GetWorkScheduleQueryValidator : AbstractValidator<GetWorkScheduleQuery>
    {
        public GetWorkScheduleQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();
        }
    }
}
