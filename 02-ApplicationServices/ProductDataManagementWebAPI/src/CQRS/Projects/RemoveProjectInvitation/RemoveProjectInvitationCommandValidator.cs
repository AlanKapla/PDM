using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.RemoveProjectInvitation;

public sealed class RemoveProjectInvitationCommandValidator : AbstractValidator<RemoveProjectInvitationCommand>
{
    public RemoveProjectInvitationCommandValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ProjectId).RequiredId();
        RuleFor(x => x.InvitationId).RequiredId();
    }
}
