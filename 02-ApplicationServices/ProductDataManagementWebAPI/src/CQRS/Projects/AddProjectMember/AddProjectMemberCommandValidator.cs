using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.AddProjectMember
{
    public class AddProjectMemberCommandValidator : AbstractValidator<AddProjectMemberCommand>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;

        public AddProjectMemberCommandValidator(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            IRepository<TenantMember> tenantMemberRepo)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.tenantMemberRepo = tenantMemberRepo;

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required");

            // Validate project exists and is active in one rule to avoid duplicate queries
            RuleFor(x => x)
                .MustAsync(ProjectMustExistAndBeActive)
                .WithMessage("Project not found or is inactive");

            // Validate user must be active tenant member
            RuleFor(x => x)
                .MustAsync(UserMustBeTenantMember)
                .WithMessage("User is not an active member of the tenant");

            // Validate user must not be project member already
            RuleFor(x => x)
                .MustAsync(UserMustNotBeProjectMember)
                .WithMessage("User is already a member of this project");
        }

        private async Task<bool> ProjectMustExistAndBeActive(AddProjectMemberCommand command, CancellationToken cancellationToken)
        {
            // Combined check to avoid fetching project twice
            Project? project = await projectRepo.GetFirstBySearch(
                p => p.Id == command.ProjectId && p.TenantId == command.TenantId,
                cancellationToken);

            return project != null && project.IsActive;
        }

        private async Task<bool> UserMustBeTenantMember(AddProjectMemberCommand command, CancellationToken cancellationToken)
        {
            TenantMember? tenantMember = await tenantMemberRepo.GetFirstBySearch(
                tm => tm.TenantId == command.TenantId
                    && tm.UserId == command.UserId
                    && tm.IsActive);

            return tenantMember != null;
        }

        private async Task<bool> UserMustNotBeProjectMember(AddProjectMemberCommand command, CancellationToken cancellationToken)
        {
            ProjectMember? existingMember = await projectMemberRepo.GetFirstBySearch(
                pm => pm.ProjectId == command.ProjectId
                    && pm.TenantId == command.TenantId
                    && pm.UserId == command.UserId);

            return existingMember == null;
        }
    }
}
