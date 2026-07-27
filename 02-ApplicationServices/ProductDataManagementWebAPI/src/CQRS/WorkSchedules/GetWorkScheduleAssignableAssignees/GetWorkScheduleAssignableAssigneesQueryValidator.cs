using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.GetWorkScheduleAssignableAssignees
{
    public sealed class GetWorkScheduleAssignableAssigneesQueryValidator
        : AbstractValidator<GetWorkScheduleAssignableAssigneesQuery>
    {
        public GetWorkScheduleAssignableAssigneesQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
        }
    }
}
