using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using Repositiories.Repository.Interfaces;

namespace CQRS.Files.UpdateFileShare
{
    public class UpdateFileShareCommandValidator : AbstractValidator<UpdateFileShareCommand>
    {
        public UpdateFileShareCommandValidator(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectFile> projectFileRepo,
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");

            RuleFor(x => x.FileId)
                .NotEmpty().WithMessage("FileId is required");

            // Verify project exists
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    var project = await projectRepo.GetFirstBySearch(
                        p => p.Id == command.ProjectId && p.TenantId == command.TenantId);
                    return project != null;
                })
                .WithMessage("Project not found");

            // Verify file exists and current user is the owner
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    var file = await projectFileRepo.GetFirstBySearch(
                        pf => pf.Id == command.FileId
                            && pf.ProjectId == command.ProjectId
                            && pf.TenantId == command.TenantId
                            && pf.OwnerId == currentUser.Id
                            && !pf.IsDeleted);
                    
                    return file != null;
                })
                .WithMessage("File not found or you are not the owner. Only file owner can manage sharing.");

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
