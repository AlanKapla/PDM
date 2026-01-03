using Business.Interfaces.Model;
using CQRS.Extensions;
using Entities.Enums;
using Entities.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.DeleteProjectFile
{
    public class DeleteProjectFileCommandValidator : AbstractValidator<DeleteProjectFileCommand>
    {
        public DeleteProjectFileCommandValidator(
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

            // Validate project exists
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    var project = await projectRepo.GetFirstBySearch(
                        p => p.Id == command.ProjectId && p.TenantId == command.TenantId);
                    return project != null;
                })
                .WithMessage("Project not found");

            // Validate file exists
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    var file = await projectFileRepo.GetFirstBySearch(
                        pf => pf.Id == command.FileId &&
                              pf.ProjectId == command.ProjectId &&
                              pf.TenantId == command.TenantId);
                    return file != null;
                })
                .WithMessage("File not found");

            // Validate user has permission (file owner OR project admin)
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    var file = await projectFileRepo.GetFirstBySearch(
                        pf => pf.Id == command.FileId &&
                              pf.ProjectId == command.ProjectId &&
                              pf.TenantId == command.TenantId &&
                              !pf.IsDeleted);

                    if (file == null) return false;

                    var membership = await projectMemberRepo.GetFirstBySearch(
                        pm => pm.ProjectId == command.ProjectId &&
                              pm.TenantId == command.TenantId &&
                              pm.UserId == currentUser.Id,
                        include => include.Include(pm => pm.MemberRole));

                    if (membership == null) return false;

                    bool isFileOwner = file.OwnerId == currentUser.Id;
                    bool isProjectAdmin = membership.MemberRole?.Code.IsProjectAdmin() == true;

                    return isFileOwner || isProjectAdmin;
                })
                .WithMessage("Only file owner or project admin can delete files");
        }
    }
}
