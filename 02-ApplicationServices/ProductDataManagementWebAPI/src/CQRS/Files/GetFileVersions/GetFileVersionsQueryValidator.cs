using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Files.GetFileVersions;

public sealed class GetFileVersionsQueryValidator : AbstractValidator<GetFileVersionsQuery>
{
    public GetFileVersionsQueryValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ProjectId).RequiredId();
        RuleFor(x => x.FileId).RequiredId();
        RuleFor(x => x.Scope).ValidScope();
    }
}
