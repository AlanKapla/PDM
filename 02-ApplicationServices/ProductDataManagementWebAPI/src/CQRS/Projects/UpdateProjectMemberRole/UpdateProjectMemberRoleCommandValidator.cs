using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Repositiories.Repository.Interfaces;

namespace CQRS.Projects.UpdateProjectMemberRole
{
    public class UpdateProjectMemberRoleCommandValidator : AbstractValidator<UpdateProjectMemberRoleCommand>
    {
        private readonly ICurrentUser currentUser;

        public UpdateProjectMemberRoleCommandValidator(ICurrentUser currentUser)
        {
            this.currentUser = currentUser;

            RuleFor(x => x.TenantId)
                .NotEmpty()
                .WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("ProjectId is required");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId is required");

            RuleFor(x => x.RoleId)
                .NotEmpty()
                .WithMessage("RoleId is required");

            RuleFor(x => x.UserId)
                .Must(x => x != currentUser.Id)
                .WithMessage("Cannot change your own role");
        }
    }
}
