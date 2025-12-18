using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Repositories.Repository.Interfaces;
using Repositiories.Repository.Interfaces;

namespace CQRS.Files.UploadProjectFiles
{
    public class UploadProjectFilesCommandValidator : AbstractValidator<UploadProjectFilesCommand>
    {
        public UploadProjectFilesCommandValidator(
            IReadRepository<Project> projectRepo,
            IReadRepository<ProjectFilePackage> packageRepo,
            ICurrentUser currentUser)
        {
            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");

            RuleFor(x => x.ProjectFilePackageId)
                .NotEmpty().WithMessage("ProjectFilePackageId is required");

            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    var project = await projectRepo.GetFirstBySearch(
                        p => p.Id == command.ProjectId && p.TenantId == command.TenantId);
                    return project != null;
                })
                .WithMessage("Project not found");

            // Check if package exists and belongs to current user
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    var package = await packageRepo.GetFirstBySearch(
                        pfp => pfp.Id == command.ProjectFilePackageId &&
                               pfp.TenantId == command.TenantId &&
                               pfp.ProjectId == command.ProjectId &&
                               pfp.OwnerId == currentUser.Id &&
                               !pfp.IsDeleted);
                    return package != null;
                })
                .WithMessage("Package not found or does not belong to you");

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
                        .GreaterThan(0).WithMessage("File cannot be empty")
                        .LessThanOrEqualTo(FileConstants.MaxFileSizeBytes)
                        .WithMessage($"File cannot be larger than {FileConstants.MaxFileSizeBytes / 1024 / 1024} MB")
                        .When(fi => fi.File != null);

                    fileItem.RuleFor(fi => fi.File.FileName)
                        .NotEmpty().WithMessage("File name is required")
                        .Must(BeValidExtension)
                        .WithMessage($"Allowed file formats are: {FileConstants.GetAllowedExtensionsMessage()}")
                        .When(fi => fi.File != null);

                    fileItem.RuleFor(fi => fi.File.ContentType)
                        .Must(BeValidContentType)
                        .WithMessage($"Allowed MIME types are: {FileConstants.GetAllowedContentTypesMessage()}")
                        .When(fi => fi.File != null);

                    fileItem.RuleFor(fi => fi.DisplayName)
                        .MaximumLength(FileConstants.MaxDisplayNameLength)
                        .WithMessage($"Display name cannot exceed {FileConstants.MaxDisplayNameLength} characters")
                        .When(fi => !string.IsNullOrWhiteSpace(fi.DisplayName));
                });
        }

        private bool BeValidExtension(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return FileConstants.AllowedExtensions.Contains(extension);
        }

        private bool BeValidContentType(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
                return false;

            return FileConstants.AllowedContentTypes.Contains(contentType.ToLowerInvariant());
        }
    }
}
