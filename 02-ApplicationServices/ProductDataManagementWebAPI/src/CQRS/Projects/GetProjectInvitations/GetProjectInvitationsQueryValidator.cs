using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.GetProjectInvitations;

public sealed class GetProjectInvitationsQueryValidator : AbstractValidator<GetProjectInvitationsQuery>
{
    public GetProjectInvitationsQueryValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ProjectId).RequiredId();
    }
}
