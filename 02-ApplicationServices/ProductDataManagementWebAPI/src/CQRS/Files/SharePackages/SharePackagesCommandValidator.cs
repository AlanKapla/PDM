using FluentValidation;

namespace CQRS.Files.SharePackages
{
    public class SharePackagesCommandValidator : AbstractValidator<SharePackagesCommand>
    {
        public SharePackagesCommandValidator()
        {
            RuleFor(x => x.PackageIds)
                .NotEmpty()
                .WithMessage("At least one package must be specified");

            RuleFor(x => x.PackageIds)
                .Must(list => list.Distinct().Count() == list.Count)
                .WithMessage("Package IDs must be unique");

            RuleFor(x => x.SharedWithUserIds)
                .NotEmpty()
                .WithMessage("At least one user must be specified");

            RuleFor(x => x.SharedWithUserIds)
                .Must(list => list.Distinct().Count() == list.Count)
                .WithMessage("User IDs must be unique");
        }
    }
}

