using CQRS.Extensions;
using FluentValidation;

namespace CQRS.ProjectCosts.GetProjectCosts;

public sealed class GetProjectCostsQueryValidator : AbstractValidator<GetProjectCostsQuery>
{
    public GetProjectCostsQueryValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ProjectId).RequiredId();
        RuleFor(x => x.Scope).IsInEnum();
    }
}
