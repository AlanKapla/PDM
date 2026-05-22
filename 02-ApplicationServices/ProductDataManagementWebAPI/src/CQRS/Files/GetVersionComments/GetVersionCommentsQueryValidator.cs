using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Files.GetVersionComments;

public sealed class GetVersionCommentsQueryValidator : AbstractValidator<GetVersionCommentsQuery>
{
    public GetVersionCommentsQueryValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ProjectId).RequiredId();
        RuleFor(x => x.FileId).RequiredId();
        RuleFor(x => x.VersionId).RequiredId();
        RuleFor(x => x.Scope).ValidScope();
    }
}
