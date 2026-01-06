using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Repositories.Repository.Interfaces;
using Repositiories.Repository.Interfaces;

namespace CQRS.Files.UpdateFileShare
{
    public class UpdateFileShareCommandValidator : AbstractValidator<UpdateFileShareCommand>
    {
        public UpdateFileShareCommandValidator(
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            RuleFor(x => x.FileId)
                .NotEmpty().WithMessage("FileId is required");

            // Verify all target users are project members
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    if (command.SharedWithUserIds == null || !command.SharedWithUserIds.Any())
                        return true; // Empty list is valid (remove all shares)
                    
                    foreach (var userId in command.SharedWithUserIds)
                    {
                        var member = await projectMemberRepo.GetFirstBySearch(
                            pm => pm.ProjectId == command.ProjectId
                                && pm.TenantId == command.TenantId
                                && pm.UserId == userId);
                        
                        if (member == null)
                            return false;
                    }
                    
                    return true;
                })
                .WithMessage("All users must be members of the project");

            // User cannot share file with themselves
            RuleFor(x => x.SharedWithUserIds)
                .Must((command, userIds) => !userIds.Contains(currentUser.Id))
                .When(x => x.SharedWithUserIds != null && x.SharedWithUserIds.Any())
                .WithMessage("You cannot share a file with yourself");
        }
    }
}
