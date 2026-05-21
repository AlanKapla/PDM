using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.GetProjectDetails
{
    public sealed class GetProjectDetailsQueryValidator : AbstractValidator<GetProjectDetailsQuery>
    {
        public GetProjectDetailsQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
        }
    }
}
