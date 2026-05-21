using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Files.GetProjectFilePackages;

public sealed class GetProjectFilePackagesQueryValidator : AbstractValidator<GetProjectFilePackagesQuery>
{
    public GetProjectFilePackagesQueryValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ProjectId).RequiredId();
        RuleFor(x => x.Scope).ValidScope();
    }
}
