using CQRS.Extensions;
using FluentValidation;

namespace CQRS.ProjectCosts.DeleteProjectCost;

public sealed class DeleteProjectCostCommandValidator : AbstractValidator<DeleteProjectCostCommand>
{
    public DeleteProjectCostCommandValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ProjectId).RequiredId();
        RuleFor(x => x.CostId).RequiredId();
    }
}
