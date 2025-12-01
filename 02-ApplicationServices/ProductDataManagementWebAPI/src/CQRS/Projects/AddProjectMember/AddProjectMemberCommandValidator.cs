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

            // Walidacja: projekt musi istnieæ
            RuleFor(x => x)
                .MustAsync(ProjectMustExist)
                .WithMessage("Project not found");

            // Walidacja: projekt musi byæ aktywny
            RuleFor(x => x)
                .MustAsync(ProjectMustBeActive)
                .WithMessage("Cannot add members to inactive project");

            // Walidacja: u¿ytkownik musi byæ aktywnym cz³onkiem tenanta
            RuleFor(x => x)
                .MustAsync(UserMustBeTenantMember)
                .WithMessage("User is not an active member of the tenant");

            // Walidacja: u¿ytkownik nie mo¿e ju¿ byæ cz³onkiem projektu
            RuleFor(x => x)
                .MustAsync(UserMustNotBeProjectMember)
                .WithMessage("User is already a member of this project");
        }

        private async Task<bool> ProjectMustExist(AddProjectMemberCommand command, CancellationToken cancellationToken)
        {
            Project? project = await projectRepo.GetFirstBySearch(
                p => p.Id == command.ProjectId && p.TenantId == command.TenantId,
                cancellationToken);

            return project != null;
        }

        private async Task<bool> ProjectMustBeActive(AddProjectMemberCommand command, CancellationToken cancellationToken)
        {
            Project? project = await projectRepo.GetFirstBySearch(
                p => p.Id == command.ProjectId && p.TenantId == command.TenantId,
                cancellationToken);

            return project?.IsActive ?? false;
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
