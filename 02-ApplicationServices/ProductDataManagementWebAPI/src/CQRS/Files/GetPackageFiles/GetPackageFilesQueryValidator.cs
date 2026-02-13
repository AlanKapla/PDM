using FluentValidation;

namespace CQRS.Files.GetPackageFiles;

public class GetProjectFilePackagesQueryValidator : AbstractValidator<GetPackageFilesQuery>
{
    public GetProjectFilePackagesQueryValidator()
    {
        RuleFor(x => x.PackageId)
            .NotEmpty()
            .WithMessage("PackageId is required");

        RuleFor(x => x.Scope)
            .IsInEnum()
            .WithMessage("Scope is invalid");
    }
}
