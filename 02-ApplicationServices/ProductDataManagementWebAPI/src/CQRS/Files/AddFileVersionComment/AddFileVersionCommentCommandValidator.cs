using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.AddFileVersionComment
{
    /// <summary>
    /// Validator for adding a comment to a file version
    /// </summary>
    public class AddFileVersionCommentCommandValidator : AbstractValidator<AddFileVersionCommentCommand>
    {
        public AddFileVersionCommentCommandValidator(
            IRepository<ProjectFile> projectFileRepo,
            IRepository<ProjectFileVersion> projectFileVersionRepo,
            ICurrentUser currentUser)
        {
            RuleFor(x => x.TenantId)
                .NotEmpty()
                .WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("ProjectId is required");

            RuleFor(x => x.FileId)
                .NotEmpty()
                .WithMessage("FileId is required");

            RuleFor(x => x.VersionId)
                .NotEmpty()
                .WithMessage("VersionId is required");

            RuleFor(x => x.Comment)
                .NotEmpty()
                .WithMessage("Comment cannot be empty")
                .MaximumLength(FileConstants.MaxCommentLength)
                .WithMessage($"Comment cannot exceed {FileConstants.MaxCommentLength} characters");

            // Verify that the file exists, belongs to the project, and user has access
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    var file = await projectFileRepo.GetFirstBySearch(
                        pf => pf.Id == command.FileId
                            && pf.ProjectId == command.ProjectId
                            && pf.TenantId == command.TenantId
                            && !pf.IsDeleted
                            && (pf.OwnerId == currentUser.Id || pf.SharedWith.Any(s => s.SharedWithUserId == currentUser.Id))
                            && pf.Versions.Any(v => v.Id == command.VersionId));

                    return file != null;
                        
                })
                .WithMessage("File or version does not exist.");
        }
    }
}
