using Business.Interfaces.Constants;
using FluentValidation;

namespace CQRS.Files.UploadProjectFileVersion
{
    public class UploadProjectFileVersionCommandValidator : AbstractValidator<UploadProjectFileVersionCommand>
    {
        public UploadProjectFileVersionCommandValidator()
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

            RuleFor(x => x.File)
                .NotNull()
                .WithMessage("File is required");

            When(x => x.File != null, () =>
            {
                RuleFor(x => x.File.Length)
                    .LessThanOrEqualTo(FileConstants.MaxFileSizeBytes)
                    .WithMessage($"File cannot be larger than {FileConstants.MaxFileSizeBytes / 1024 / 1024} MB");

                RuleFor(x => x.File.Length)
                    .GreaterThan(0)
                    .WithMessage("File cannot be empty");

                RuleFor(x => x.File.FileName)
                    .Must(fileName =>
                    {
                        if (string.IsNullOrWhiteSpace(fileName))
                            return false;

                        string extension = Path.GetExtension(fileName).ToLowerInvariant();
                        return FileConstants.AllowedExtensions.Contains(extension);
                    })
                    .WithMessage($"Allowed file formats are: {FileConstants.GetAllowedExtensionsMessage()}");
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
