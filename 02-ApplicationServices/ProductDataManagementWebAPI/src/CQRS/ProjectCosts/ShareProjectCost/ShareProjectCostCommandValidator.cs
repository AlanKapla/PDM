using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.ShareProjectCost
{
    public class ShareProjectCostCommandValidator : AbstractValidator<ShareProjectCostCommand>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly ICurrentUser currentUser;

        public ShareProjectCostCommandValidator(
            IRepository<ProjectCost> projectCostRepo,
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            this.projectCostRepo = projectCostRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.currentUser = currentUser;

            RuleFor(x => x.CostId)
                .NotEmpty()
                .WithMessage("CostId is required")
                .MustAsync(BeValidCostAndOwner)
                .WithMessage("Cost not found or you are not the owner");

            // Allow empty collection (to remove all shares)
            RuleFor(x => x.SharedWithUserIds)
                .Must(x => x.Distinct().Count() == x.Count)
                .WithMessage("Duplicate user IDs are not allowed")
                .When(x => x.SharedWithUserIds.Any());

            RuleFor(x => x)
                .MustAsync(AllUsersBeProjectMembers)
                .WithMessage("All users must be members of the project")
                .When(x => x.SharedWithUserIds.Any());
        }

        private async Task<bool> BeValidCostAndOwner(ShareProjectCostCommand command, Guid costId, CancellationToken cancellationToken)
        {
            var cost = await projectCostRepo.GetFirstBySearch(
                pc => pc.Id == costId 
                    && pc.TenantId == command.TenantId 
                    && pc.ProjectId == command.ProjectId 
                    && !pc.IsDeleted
                    && pc.UserId == currentUser.Id);

            return cost != null;
        }

        private async Task<bool> AllUsersBeProjectMembers(ShareProjectCostCommand command, CancellationToken cancellationToken)
        {
            foreach (var userId in command.SharedWithUserIds)
            {
                // User cannot share with themselves
                if (userId == currentUser.Id)
                {
                    return false;
                }

                var member = await projectMemberRepo.GetFirstBySearch(
                    pm => pm.ProjectId == command.ProjectId 
                        && pm.TenantId == command.TenantId 
                        && pm.UserId == userId);

                if (member == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
