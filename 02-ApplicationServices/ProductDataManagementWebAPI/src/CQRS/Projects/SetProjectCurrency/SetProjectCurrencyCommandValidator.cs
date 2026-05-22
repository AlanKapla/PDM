using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.SetProjectCurrency
{
    public sealed class SetProjectCurrencyCommandValidator : AbstractValidator<SetProjectCurrencyCommand>
    {
        public SetProjectCurrencyCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Currency code is required")
                .MaximumLength(10).WithMessage("Currency code cannot exceed 10 characters")
                .Matches(@"^[A-Z]{2,10}$").WithMessage("Currency code must consist of 2 to 10 uppercase letters");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Currency name is required")
                .MaximumLength(100).WithMessage("Currency name cannot exceed 100 characters");

            RuleFor(x => x.Symbol)
                .MaximumLength(10).WithMessage("Currency symbol cannot exceed 10 characters")
                .When(x => x.Symbol is not null);
        }
    }
}
