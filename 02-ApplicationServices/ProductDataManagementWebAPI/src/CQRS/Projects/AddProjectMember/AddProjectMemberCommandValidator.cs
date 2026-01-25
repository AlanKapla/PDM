using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Repositories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.AddProjectMember
{
    public class AddProjectMemberCommandValidator : AbstractValidator<AddProjectMemberCommand>
    {
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;

        public AddProjectMemberCommandValidator(
            IRepository<ProjectMember> projectMemberRepo,
            IRepository<TenantMember> tenantMemberRepo)
        {
            this.projectMemberRepo = projectMemberRepo;
            this.tenantMemberRepo = tenantMemberRepo;

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required");

            // Validate user must be active tenant member
            RuleFor(x => x)
                .MustAsync(UserMustBeTenantMember)
                .WithMessage("User is not an active member of the tenant");

            // Validate user must not be project member already
            RuleFor(x => x)
                .MustAsync(UserMustNotBeProjectMember)
                .WithMessage("User is already a member of this project");
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
