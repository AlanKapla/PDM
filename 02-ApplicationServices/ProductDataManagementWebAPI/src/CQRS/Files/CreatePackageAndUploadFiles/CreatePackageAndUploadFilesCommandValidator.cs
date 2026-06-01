using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using CQRS.Extensions;
using Entities.Models.Files;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.CreatePackageAndUploadFiles
{
    public sealed class CreatePackageAndUploadFilesCommandValidator : AbstractValidator<CreatePackageAndUploadFilesCommand>
    {
        public CreatePackageAndUploadFilesCommandValidator(
            IReadRepository<ProjectFilePackage> packageRepo,
            ICurrentUser currentUser)
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();

            RuleFor(x => x.PackageName)
                .NotEmpty().WithMessage("Package name is required")
                .MaximumLength(FileConstants.MaxPackageNameLength)
                .WithMessage($"Package name cannot exceed {FileConstants.MaxPackageNameLength} characters");

            // Check if package with this name already exists for current user in the same parent directory
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    ProjectFilePackage? existingPackage = await packageRepo.GetFirstBySearch(
                        pfp => pfp.TenantId == command.TenantId &&
                               pfp.ProjectId == command.ProjectId &&
                               pfp.OwnerId == currentUser.Id &&
                               pfp.Name == command.PackageName &&
                               pfp.ParentId == command.ParentId);
                    return existingPackage is null;
                })
                .WithMessage("A package with this name already exists for you in this project");

            RuleFor(x => x.ParentId)
                .MustAsync(async (command, parentId, ct) =>
                {
                    if (parentId is null) return true;
                    ProjectFilePackage? parent = await packageRepo.GetFirstBySearch(
                        p => p.Id == parentId.Value &&
                             p.TenantId == command.TenantId &&
                             p.ProjectId == command.ProjectId);
                    return parent is not null;
                })
                .WithMessage("Parent directory not found or does not belong to this project.")
                .When(c => c.ParentId.HasValue);

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
