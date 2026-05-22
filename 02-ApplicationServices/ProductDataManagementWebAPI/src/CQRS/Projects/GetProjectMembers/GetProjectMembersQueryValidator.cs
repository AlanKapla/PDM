using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.GetProjectMembers
{
    public sealed class GetProjectMembersQueryValidator : AbstractValidator<GetProjectMembersQuery>
    {
        public GetProjectMembersQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
        }
    }
}
