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

            // Check if all files exist in the project
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    foreach (var fileId in command.ProjectFileIds)
                    {
                        var file = await projectFileRepo.GetFirstBySearch(
                            pf => pf.Id == fileId &&
                                  pf.ProjectId == command.ProjectId &&
                                  pf.TenantId == command.TenantId);
                        
                        if (file == null)
                            return false;
                    }
                    return true;
                })
                .WithMessage("One or more files do not exist in this project");

            // Check if user is the owner of all files
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    foreach (var fileId in command.ProjectFileIds)
                    {
                        var file = await projectFileRepo.GetFirstBySearch(
                            pf => pf.Id == fileId &&
                                  pf.OwnerId == currentUser.Id &&
                                  !pf.IsDeleted);
                        
                        if (file == null)
                            return false;
                    }
                    return true;
                })
                .WithMessage("You can only share your own files");

            // Check if all users we're sharing with are members of the project
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    foreach (var userId in command.SharedWithUserIds)
                    {
                        var member = await projectMemberRepo.GetFirstBySearch(
                            pm => pm.ProjectId == command.ProjectId &&
                                  pm.TenantId == command.TenantId &&
                                  pm.UserId == userId);
                        
                        if (member == null)
                            return false;
                    }
                    return true;
                })
                .WithMessage("One or more users are not members of this project");

            // Check if not sharing with yourself
            RuleFor(x => x.SharedWithUserIds)
                .Must((command, userIds) => !userIds.Contains(currentUser.Id))
                .WithMessage("You cannot share files with yourself");
        }
    }
}
