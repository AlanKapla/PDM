using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Files.SharePackages
{
    public sealed class SharePackagesCommandValidator : AbstractValidator<SharePackagesCommand>
    {
        public SharePackagesCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();

            RuleFor(x => x.PackageIds)
                .NotEmpty()
                .WithMessage("At least one package must be specified");

            RuleFor(x => x.PackageIds).UniqueIds();

            RuleFor(x => x.SharedWithUserIds)
                .NotEmpty()
                .WithMessage("At least one user must be specified");

            RuleFor(x => x.SharedWithUserIds).UniqueIds();
        }
    }
}

