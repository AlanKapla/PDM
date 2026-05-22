using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Files.GetPackageFiles;

public sealed class GetPackageFilesQueryValidator : AbstractValidator<GetPackageFilesQuery>
{
    public GetPackageFilesQueryValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ProjectId).RequiredId();
        RuleFor(x => x.PackageId).RequiredId();
        RuleFor(x => x.Scope).ValidScope();
    }
}
