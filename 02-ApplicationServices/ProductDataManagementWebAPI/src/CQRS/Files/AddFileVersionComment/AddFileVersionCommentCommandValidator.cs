using Business.Interfaces.Constants;
using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Files.AddFileVersionComment
{
    /// <summary>
    /// Validator for adding a comment to a file version
    /// </summary>
    public sealed class AddFileVersionCommentCommandValidator : AbstractValidator<AddFileVersionCommentCommand>
    {
        public AddFileVersionCommentCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.FileId).RequiredId();
            RuleFor(x => x.VersionId).RequiredId();

            RuleFor(x => x.Comment)
                .NotEmpty()
                .WithMessage("Comment cannot be empty")
                .MaximumLength(FileConstants.MaxCommentLength)
                .WithMessage($"Comment cannot exceed {FileConstants.MaxCommentLength} characters");
        }
    }
}
