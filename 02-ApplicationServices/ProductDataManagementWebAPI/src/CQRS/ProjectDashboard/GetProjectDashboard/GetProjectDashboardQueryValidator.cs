using CQRS.Extensions;
using FluentValidation;

namespace CQRS.ProjectDashboard.GetProjectDashboard
{
    public sealed class GetProjectDashboardQueryValidator : AbstractValidator<GetProjectDashboardQuery>
    {
        public GetProjectDashboardQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
        }
    }
}
