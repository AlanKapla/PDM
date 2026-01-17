using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.UpdateCostShare
{
    public class UpdateCostShareCommandValidator : AbstractValidator<UpdateCostShareCommand>
    {
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly ICurrentUser currentUser;

        public UpdateCostShareCommandValidator(
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            this.projectMemberRepo = projectMemberRepo;
            this.currentUser = currentUser;

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

        private async Task<bool> AllUsersBeProjectMembers(UpdateCostShareCommand command, CancellationToken cancellationToken)
        {
            // User cannot share with themselves
            if (command.SharedWithUserIds.Contains(currentUser.Id))
            {
                return false;
            }

            // Fetch all project members in one query instead of in a loop
            var members = await projectMemberRepo.GetBySearch(
                pm => pm.ProjectId == command.ProjectId 
                    && pm.TenantId == command.TenantId 
                    && command.SharedWithUserIds.Contains(pm.UserId));

            var memberUserIds = members.Select(m => m.UserId).ToHashSet();

            // Check if all requested users are project members
            return command.SharedWithUserIds.All(userId => memberUserIds.Contains(userId));
        }
    }
}
