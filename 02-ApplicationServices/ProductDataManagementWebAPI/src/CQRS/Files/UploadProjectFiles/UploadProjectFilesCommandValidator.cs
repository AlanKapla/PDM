using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.UploadProjectFiles
{
    public class UploadProjectFilesCommandValidator : AbstractValidator<UploadProjectFilesCommand>
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".pdf" };
        private static readonly string[] AllowedContentTypes = 
        { 
            "image/jpeg", 
            "image/jpg", 
            "application/pdf" 
        };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        public UploadProjectFilesCommandValidator(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");

            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    var project = await projectRepo.GetFirstBySearch(
                        p => p.Id == command.ProjectId && p.TenantId == command.TenantId);
                    return project != null;
                })
                .WithMessage("Project not found");

            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    var membership = await projectMemberRepo.GetFirstBySearch(
                        pm => pm.ProjectId == command.ProjectId &&
                              pm.TenantId == command.TenantId &&
                              pm.UserId == currentUser.Id);
                    return membership != null;
                })
                .WithMessage("User is not a member of the project");

            RuleFor(x => x.PackageName)
                .NotEmpty().WithMessage("Package name is required")
                .MaximumLength(200).WithMessage("Package name cannot exceed 200 characters")
                .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Package name can only contain letters, numbers, _ and -");

            RuleFor(x => x.Files)
                .NotNull().WithMessage("Files list cannot be null")
                .NotEmpty().WithMessage("You must upload at least one file")
                .Must(files => files.Count <= 50).WithMessage("You can upload a maximum of 50 files at once");

            RuleForEach(x => x.Files)
                .ChildRules(fileItem =>
                {
                    fileItem.RuleFor(fi => fi.File)
                        .NotNull().WithMessage("File is required");

                    fileItem.RuleFor(fi => fi.File.Length)
                        .GreaterThan(0).WithMessage("File cannot be empty")
                        .LessThanOrEqualTo(MaxFileSizeBytes).WithMessage($"File cannot be larger than {MaxFileSizeBytes / 1024 / 1024} MB")
                        .When(fi => fi.File != null);

                    fileItem.RuleFor(fi => fi.File.FileName)
                        .NotEmpty().WithMessage("File name is required")
                        .Must(BeValidExtension).WithMessage($"Allowed file formats are: {string.Join(", ", AllowedExtensions)}")
                        .When(fi => fi.File != null);

                    fileItem.RuleFor(fi => fi.File.ContentType)
                        .Must(BeValidContentType).WithMessage($"Allowed MIME types are: {string.Join(", ", AllowedContentTypes)}")
                        .When(fi => fi.File != null);

                    fileItem.RuleFor(fi => fi.DisplayName)
                        .MaximumLength(255).WithMessage("Display name cannot exceed 255 characters")
                        .When(fi => !string.IsNullOrWhiteSpace(fi.DisplayName));
                });
        }

        private bool BeValidExtension(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return AllowedExtensions.Contains(extension);
        }

        private bool BeValidContentType(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
                return false;

            return AllowedContentTypes.Contains(contentType.ToLowerInvariant());
        }
    }
}
