using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.GetTenantProjects
{
    public sealed class GetTenantProjectsQueryValidator : AbstractValidator<GetTenantProjectsQuery>
    {
        public GetTenantProjectsQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
        }
    }
}
