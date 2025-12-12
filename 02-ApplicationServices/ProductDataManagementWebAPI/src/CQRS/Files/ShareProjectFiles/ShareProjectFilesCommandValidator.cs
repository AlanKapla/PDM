using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.ShareProjectFiles
{
    public class ShareProjectFilesCommandValidator : AbstractValidator<ShareProjectFilesCommand>
    {
        public ShareProjectFilesCommandValidator(
            IReadRepository<ProjectFile> projectFileRepo,
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");

            RuleFor(x => x.ProjectFileIds)
                .NotNull().WithMessage("ProjectFileIds is required")
                .NotEmpty().WithMessage("You must select at least one file to share")
                .Must(ids => ids.Count <= 50).WithMessage("You can share a maximum of 50 files at once");

            RuleFor(x => x.SharedWithUserIds)
                .NotNull().WithMessage("SharedWithUserIds is required")
                .NotEmpty().WithMessage("You must select at least one user to share with")
                .Must(ids => ids.Count <= 50).WithMessage("You can share with a maximum of 50 users at once");

            // Check if all files exist in the project and user is owner
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    // Fetch all files in one query instead of in a loop
                    var files = await projectFileRepo.GetBySearch(
                        pf => command.ProjectFileIds.Contains(pf.Id) &&
                              pf.ProjectId == command.ProjectId &&
                              pf.TenantId == command.TenantId &&
                              !pf.IsDeleted);
                    
                    var filesList = files.ToList();
                    
                    // Check if all requested files exist
                    if (filesList.Count != command.ProjectFileIds.Count)
                        return false;
                    
                    // Check if user is owner of all files
                    return filesList.All(f => f.OwnerId == currentUser.Id);
                })
                .WithMessage("One or more files do not exist in this project or you are not the owner");

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
                .WithMessage("You cannot share files with yourself");
        }
    }
}
