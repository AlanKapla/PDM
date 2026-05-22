using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.GetUserAssignedWorks
{
    public sealed class GetUserAssignedWorksQueryValidator : AbstractValidator<GetUserAssignedWorksQuery>
    {
        public GetUserAssignedWorksQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
        }
    }
}
