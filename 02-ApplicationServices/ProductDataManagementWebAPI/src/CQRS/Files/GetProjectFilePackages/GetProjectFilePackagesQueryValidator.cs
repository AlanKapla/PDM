using CQRS.Files.GetProjectFilePackages;
using FluentValidation;

namespace CQRS.Files.GetProjectFilePackages;

public class GetProjectFilePackagesQueryValidator : AbstractValidator<GetProjectFilePackagesQuery>
{
    public GetProjectFilePackagesQueryValidator()
    {
        RuleFor(x => x.Scope)
            .IsInEnum()
            .WithMessage("Scope is invalid");
    }
}
