using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.ShareProjectFile
{
    public class ShareProjectFileCommandValidator : AbstractValidator<ShareProjectFileCommand>
    {
        public ShareProjectFileCommandValidator(
            IReadRepository<ProjectFile> projectFileRepo,
            IRepository<ProjectMember> projectMemberRepo,
            IRepository<SharedProjectFile> sharedProjectFileRepo,
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

            RuleFor(x => x.SharedWithUserId)
                .NotEmpty().WithMessage("SharedWithUserId is required");

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
                                  pf.UploadedByUserId == currentUser.Id);
                        
                        if (file == null)
                            return false;
                    }
                    return true;
                })
                .WithMessage("You can only share your own files");

            // Check if the user we're sharing with is a member of the project
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    var member = await projectMemberRepo.GetFirstBySearch(
                        pm => pm.ProjectId == command.ProjectId &&
                              pm.TenantId == command.TenantId &&
                              pm.UserId == command.SharedWithUserId);
                    return member != null;
                })
                .WithMessage("User is not a member of this project");

            // Check if not sharing with yourself
            RuleFor(x => x.SharedWithUserId)
                .Must((command, sharedWithUserId) => sharedWithUserId != currentUser.Id)
                .WithMessage("You cannot share files with yourself");

            // Check if current user is a member of the project
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    var currentUserMember = await projectMemberRepo.GetFirstBySearch(
                        pm => pm.ProjectId == command.ProjectId &&
                              pm.TenantId == command.TenantId &&
                              pm.UserId == currentUser.Id);
                    return currentUserMember != null;
                })
                .WithMessage("You are not a member of this project");
        }
    }
}
