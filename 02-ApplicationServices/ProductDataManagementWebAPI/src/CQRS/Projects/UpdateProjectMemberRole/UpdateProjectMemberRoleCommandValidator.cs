using Business.Interfaces.Model;
using CQRS.Extensions;
using Entities.Enums;
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
            RuleFor(x => x.UserId).NotCurrentUser(currentUser);
            RuleFor(x => x.Modules)
                .Must((command, modules) => command.IsAdmin || !modules.Contains(ProjectModule.Settings))
                .WithMessage("Moduł Settings jest zarezerwowany wyłącznie dla adminów projektu.");
        }
    }
}
