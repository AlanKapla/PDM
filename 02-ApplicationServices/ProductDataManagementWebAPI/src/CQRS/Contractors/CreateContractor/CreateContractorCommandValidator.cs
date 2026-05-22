using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Contractors.CreateContractor
{
    public sealed class CreateContractorCommandValidator : AbstractValidator<CreateContractorCommand>
    {
        public CreateContractorCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();

            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(500);

            RuleFor(x => x.TaxId)
                .MaximumLength(50)
                .When(x => x.TaxId is not null);

            RuleFor(x => x.Email)
                .MaximumLength(200)
                .EmailAddress()
                .When(x => x.Email is not null);

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20)
                .When(x => x.PhoneNumber is not null);

            RuleFor(x => x.Street)
                .MaximumLength(300)
                .When(x => x.Street is not null);

            RuleFor(x => x.City)
                .MaximumLength(100)
                .When(x => x.City is not null);

            RuleFor(x => x.PostalCode)
                .MaximumLength(20)
                .When(x => x.PostalCode is not null);

            RuleFor(x => x.Country)
                .MaximumLength(100)
                .When(x => x.Country is not null);

            RuleFor(x => x.Notes)
                .MaximumLength(2000)
                .When(x => x.Notes is not null);
        }
    }
}
