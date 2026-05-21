using Business.Interfaces.Model;
using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.RemoveProjectMember
{
    public sealed class RemoveProjectMemberCommandValidator : AbstractValidator<RemoveProjectMemberCommand>
    {
        public RemoveProjectMemberCommandValidator(ICurrentUser currentUser)
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.UserId).RequiredId();
            RuleFor(x => x.UserId).NotCurrentUser(currentUser);
        }
    }
}
