using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.RemoveProjectMember
{
    public class RemoveProjectMemberCommandValidator : AbstractValidator<RemoveProjectMemberCommand>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly ICurrentUser currentUser;

        public RemoveProjectMemberCommandValidator(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.currentUser = currentUser;

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required");

            // Walidacja: projekt musi istnieæ
            RuleFor(x => x)
                .MustAsync(ProjectMustExist)
                .WithMessage("Project not found");

            // Walidacja: cz³onek projektu musi istnieæ
            RuleFor(x => x)
                .MustAsync(ProjectMemberMustExist)
                .WithMessage("User is not a member of this project");

            // Walidacja: nie mo¿na usun¹æ samego siebie
            RuleFor(x => x.UserId)
                .Must(userId => userId != currentUser.Id)
                .WithMessage("Cannot remove yourself from the project");
        }

        private async Task<bool> ProjectMustExist(RemoveProjectMemberCommand command, CancellationToken cancellationToken)
        {
            Project? project = await projectRepo.GetFirstBySearch(
                p => p.Id == command.ProjectId && p.TenantId == command.TenantId);

            return project != null;
        }

        private async Task<bool> ProjectMemberMustExist(RemoveProjectMemberCommand command, CancellationToken cancellationToken)
        {
            ProjectMember? projectMember = await projectMemberRepo.GetFirstBySearch(
                pm => pm.ProjectId == command.ProjectId
                    && pm.TenantId == command.TenantId
                    && pm.UserId == command.UserId);

            return projectMember != null;
        }
    }
}
