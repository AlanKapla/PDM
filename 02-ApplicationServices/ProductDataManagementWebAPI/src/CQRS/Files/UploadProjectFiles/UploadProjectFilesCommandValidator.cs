using Business.Interfaces.Constants;
using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Files.UploadProjectFiles
{
    public sealed class UploadProjectFilesCommandValidator : AbstractValidator<UploadProjectFilesCommand>
    {
        public UploadProjectFilesCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.ProjectFilePackageId).RequiredId();

            RuleFor(x => x.Files)
                .NotNull().WithMessage("Files list cannot be null")
                .NotEmpty().WithMessage("You must upload at least one file")
                .Must(files => files.Count <= FileConstants.MaxFilesPerUpload)
                .WithMessage($"You can upload a maximum of {FileConstants.MaxFilesPerUpload} files at once");

            RuleForEach(x => x.Files)
                .ChildRules(fileItem =>
                {
                    fileItem.RuleFor(fi => fi.File)
                        .NotNull().WithMessage("File is required");

                    fileItem.RuleFor(fi => fi.File.Length)
                        .MaxFileSize(FileConstants.MaxFileSizeBytes)
                        .When(fi => fi.File != null);

                    fileItem.RuleFor(fi => fi.File.FileName)
                        .NotEmpty().WithMessage("File name is required")
                        .AllowedFileExtension(FileConstants.AllowedExtensions)
                        .When(fi => fi.File != null);

                    fileItem.RuleFor(fi => fi.File.ContentType)
                        .AllowedContentType(FileConstants.AllowedContentTypes)
                        .When(fi => fi.File != null);

                    fileItem.RuleFor(fi => fi.DisplayName)
                        .MaximumLength(FileConstants.MaxDisplayNameLength)
                        .WithMessage($"Display name cannot exceed {FileConstants.MaxDisplayNameLength} characters")
                        .When(fi => !string.IsNullOrWhiteSpace(fi.DisplayName));
                });
        }
    }
}
