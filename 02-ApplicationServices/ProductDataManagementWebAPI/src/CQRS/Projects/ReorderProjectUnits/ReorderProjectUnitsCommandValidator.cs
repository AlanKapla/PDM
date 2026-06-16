using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.ReorderProjectUnits
{
    public sealed class ReorderProjectUnitsCommandValidator : AbstractValidator<ReorderProjectUnitsCommand>
    {
        public ReorderProjectUnitsCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();

            RuleFor(x => x.UnitIds)
                .NotNull().WithMessage("UnitIds list is required")
                .NotEmpty().WithMessage("UnitIds list cannot be empty");
        }
    }
}
