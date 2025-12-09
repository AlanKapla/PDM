using Business.Interfaces.Constants;
using Entities.Models;
using FluentValidation;
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
            IRepository<ProjectFileVersion> projectFileVersionRepo)
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

            // Verify that the file exists and belongs to the project
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    var file = await projectFileRepo.GetFirstBySearch(
                        pf => pf.Id == command.FileId
                            && pf.ProjectId == command.ProjectId
                            && pf.TenantId == command.TenantId
                            && !pf.IsDeleted);
                    
                    return file != null;
                })
                .WithMessage("File does not exist or you do not have access to it");

            // Verify that the version exists and belongs to the file
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    var version = await projectFileVersionRepo.GetFirstBySearch(
                        v => v.Id == command.VersionId
                            && v.ProjectFileId == command.FileId
                            && !v.IsDeleted);
                    
                    return version != null;
                })
                .WithMessage("File version does not exist");
        }
    }
}
