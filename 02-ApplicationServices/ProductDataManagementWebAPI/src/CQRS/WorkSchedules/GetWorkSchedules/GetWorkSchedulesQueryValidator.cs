using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.GetWorkSchedules
{
    public sealed class GetWorkSchedulesQueryValidator : AbstractValidator<GetWorkSchedulesQuery>
    {
        public GetWorkSchedulesQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.Scope).IsInEnum();
        }
    }
}
