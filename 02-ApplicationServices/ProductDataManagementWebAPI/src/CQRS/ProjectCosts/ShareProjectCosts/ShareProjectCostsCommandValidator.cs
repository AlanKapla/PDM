using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.ShareProjectCosts
{
    public class ShareProjectCostsCommandValidator : AbstractValidator<ShareProjectCostsCommand>
    {
        public ShareProjectCostsCommandValidator(
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            RuleFor(x => x.ProjectCostIds)
                .NotNull().WithMessage("ProjectCostIds is required")
                .NotEmpty().WithMessage("You must select at least one cost to share")
                .Must(ids => ids.Count <= 50).WithMessage("You can share a maximum of 50 costs at once");

            RuleFor(x => x.SharedWithUserIds)
                .NotNull().WithMessage("SharedWithUserIds is required")
                .NotEmpty().WithMessage("You must select at least one user to share with")
                .Must(ids => ids.Count <= 50).WithMessage("You can share with a maximum of 50 users at once");

            // Check if all users we're sharing with are members of the project
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    // Fetch all project members in one query instead of in a loop
                    var members = await projectMemberRepo.GetBySearch(
                        pm => pm.ProjectId == command.ProjectId &&
                              pm.TenantId == command.TenantId &&
                              command.SharedWithUserIds.Contains(pm.UserId));
                    
                    var memberUserIds = members.Select(m => m.UserId).ToHashSet();
                    
                    // Check if all requested users are project members
                    return command.SharedWithUserIds.All(userId => memberUserIds.Contains(userId));
                })
                .WithMessage("One or more users are not members of this project");

            // Check if not sharing with yourself
            RuleFor(x => x.SharedWithUserIds)
                .Must((command, userIds) => !userIds.Contains(currentUser.Id))
                .WithMessage("You cannot share costs with yourself");
        }
    }
}
