using Business.Interfaces.Constants;
using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Files.UploadProjectFileVersion
{
    public sealed class UploadProjectFileVersionCommandValidator : AbstractValidator<UploadProjectFileVersionCommand>
    {
        public UploadProjectFileVersionCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.FileId).RequiredId();

            RuleFor(x => x.File)
                .NotNull()
                .WithMessage("File is required");

            When(x => x.File != null, () =>
            {
                RuleFor(x => x.File.Length)
                    .MaxFileSize(FileConstants.MaxFileSizeBytes);

                RuleFor(x => x.File.FileName)
                    .AllowedFileExtension(FileConstants.AllowedExtensions);
            });

            When(x => !string.IsNullOrWhiteSpace(x.Comment), () =>
            {
                RuleFor(x => x.Comment)
                    .MaximumLength(FileConstants.MaxCommentLength)
                    .WithMessage($"Comment cannot exceed {FileConstants.MaxCommentLength} characters");
            });
        }
    }
}
