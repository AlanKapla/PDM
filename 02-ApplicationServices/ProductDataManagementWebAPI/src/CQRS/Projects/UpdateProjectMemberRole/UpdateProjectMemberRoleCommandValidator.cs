using Business.Interfaces.Model;
using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.UpdateProjectMemberRole
{
    public sealed class UpdateProjectMemberRoleCommandValidator : AbstractValidator<UpdateProjectMemberRoleCommand>
    {
        public UpdateProjectMemberRoleCommandValidator(ICurrentUser currentUser)
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.UserId).RequiredId();
            RuleFor(x => x.RoleId).RequiredId();
            RuleFor(x => x.UserId).NotCurrentUser(currentUser);
        }
    }
}
