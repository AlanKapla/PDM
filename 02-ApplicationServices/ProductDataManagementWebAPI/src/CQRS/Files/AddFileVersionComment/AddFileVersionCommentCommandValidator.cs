using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
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
        }
    }
}
