using Business.Interfaces.Model;
using CQRS.Extensions;
using Entities.Models.Projects;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.UpdateFileShare
{
    public sealed class UpdateFileShareCommandValidator : AbstractValidator<UpdateFileShareCommand>
    {
        public UpdateFileShareCommandValidator(
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.FileId).RequiredId();

            // Verify all target users are project members — single query (avoids N+1).
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    if (command.SharedWithUserIds is null || command.SharedWithUserIds.Count == 0)
                    {
                        return true; // Empty list is valid (remove all shares)
                    }

                    HashSet<Guid> targetIds = command.SharedWithUserIds.ToHashSet();
                    IEnumerable<ProjectMember> members = await projectMemberRepo.GetBySearch(
                        pm => pm.ProjectId == command.ProjectId
                            && pm.TenantId == command.TenantId
                            && targetIds.Contains(pm.UserId)
                            && pm.IsActive);

                    HashSet<Guid> memberIds = members.Select(m => m.UserId).ToHashSet();
                    return targetIds.All(memberIds.Contains);
                })
                .WithMessage("All users must be members of the project");

            // Current user cannot share file with themselves
            // Note: File owner is checked in handler (owner always has access, no need for explicit share)
            RuleFor(x => x.SharedWithUserIds)
                .Must((command, userIds) => !userIds.Contains(currentUser.Id))
                .When(x => x.SharedWithUserIds is not null && x.SharedWithUserIds.Count > 0)
                .WithMessage("You cannot share a file with yourself. File owner and current user always have access without explicit sharing.");
        }
    }
}
