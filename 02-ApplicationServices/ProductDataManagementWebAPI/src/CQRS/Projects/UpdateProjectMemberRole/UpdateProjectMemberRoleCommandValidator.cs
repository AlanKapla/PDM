using Business.Interfaces.Model;
using Entities.Enums;
using Entities.Models;
using FluentValidation;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.UpdateProjectMemberRole
{
    public class UpdateProjectMemberRoleCommandValidator : AbstractValidator<UpdateProjectMemberRoleCommand>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly ICurrentUser currentUser;

        public UpdateProjectMemberRoleCommandValidator(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
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

            RuleFor(x => x.Role)
                .IsInEnum()
                .WithMessage("Invalid role value");

            RuleFor(x => x)
                .MustAsync(ProjectExists)
                .WithMessage("Project not found or inactive");

            RuleFor(x => x)
                .MustAsync(ProjectMemberExists)
                .WithMessage("Project member not found or inactive");

            RuleFor(x => x.UserId)
                .Must(x => x != currentUser.Id)
                .WithMessage("Cannot change your own role");
        }

        private async Task<bool> ProjectExists(UpdateProjectMemberRoleCommand command, CancellationToken cancellationToken)
        {
            Project? project = await projectRepo.GetFirstBySearch(
                p => p.Id == command.ProjectId 
                    && p.TenantId == command.TenantId 
                    && p.IsActive);

            return project is not null;
        }

        private async Task<bool> ProjectMemberExists(UpdateProjectMemberRoleCommand command, CancellationToken cancellationToken)
        {
            ProjectMember? projectMember = await projectMemberRepo.GetFirstBySearch(
                m => m.ProjectId == command.ProjectId 
                    && m.UserId == command.UserId);

            return projectMember is not null;
        }
    }
}
