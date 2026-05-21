using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostTrackers.Shared
{
    /// <summary>
    /// Wspólne reguły walidacji dla komend dziedziczących po <see cref="TrackedCostCommandBase"/>.
    /// </summary>
    public abstract class TrackedCostCommandBaseValidator<T> : AbstractValidator<T>
        where T : TrackedCostCommandBase
    {
        protected TrackedCostCommandBaseValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("'Name' is required.")
                .MaximumLength(300).WithMessage("'Name' must not exceed 300 characters.");

            RuleFor(x => x.Number)
                .MaximumLength(100).WithMessage("'Number' must not exceed 100 characters.")
                .When(x => x.Number is not null);

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("'Description' must not exceed 2000 characters.")
                .When(x => x.Description is not null);

            RuleFor(x => x.ContractorId)
                .NotEqual(Guid.Empty)
                .When(x => x.ContractorId.HasValue);

            RuleFor(x => x.Net)
                .GreaterThanOrEqualTo(0).When(x => x.Net.HasValue)
                .WithMessage("'Net' cannot be negative.");

            RuleFor(x => x.Gross)
                .GreaterThanOrEqualTo(0).When(x => x.Gross.HasValue)
                .WithMessage("'Gross' cannot be negative.");
        }
    }
}
