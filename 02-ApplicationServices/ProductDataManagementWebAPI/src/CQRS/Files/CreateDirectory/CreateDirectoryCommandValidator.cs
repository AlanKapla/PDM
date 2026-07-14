using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using CQRS.Extensions;
using Entities.Models.Files;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.CreateDirectory
{
    public sealed class CreateDirectoryCommandValidator : AbstractValidator<CreateDirectoryCommand>
    {
        public CreateDirectoryCommandValidator(
            IReadRepository<ProjectFilePackage> packageRepo,
            ICurrentUser currentUser)
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();

            RuleFor(x => x.DirectoryName)
                .NotEmpty().WithMessage("Directory name is required")
                .MaximumLength(FileConstants.MaxPackageNameLength)
                .WithMessage($"Directory name cannot exceed {FileConstants.MaxPackageNameLength} characters");

            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    ProjectFilePackage? existing = await packageRepo.GetFirstBySearch(
                        pfp => pfp.TenantId == command.TenantId &&
                               pfp.ProjectId == command.ProjectId &&
                               pfp.OwnerId == currentUser.Id &&
                               pfp.Name == command.DirectoryName &&
                               pfp.ParentId == command.ParentId);
                    return existing is null;
                })
                .WithMessage("A directory with this name already exists for you in this location");

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
        }
    }
}
