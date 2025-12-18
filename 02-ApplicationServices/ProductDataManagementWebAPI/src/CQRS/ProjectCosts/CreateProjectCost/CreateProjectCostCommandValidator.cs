using Business.Interfaces.Helpers;
using FluentValidation;

namespace CQRS.ProjectCosts.CreateProjectCost
{
    public class CreateProjectCostCommandValidator : AbstractValidator<CreateProjectCostCommand>
    {
        public CreateProjectCostCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required")
                .MaximumLength(200)
                .WithMessage("Name cannot exceed 200 characters");

            RuleFor(x => x.Place)
                .MaximumLength(200)
                .WithMessage("Place cannot exceed 200 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.Place));

            RuleFor(x => x.Date)
                .NotEmpty()
                .WithMessage("Date is required")
                .LessThanOrEqualTo(DateTime.UtcNow.Date.AddDays(1))
                .WithMessage("Date cannot be in the future");

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .WithMessage("Description cannot exceed 2000 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.Description));

            // Validation: Must provide either (NetAmount + VatRate) OR GrossAmount
            RuleFor(x => x)
                .Must(x => AmountCalculationHelper.HasValidAmountCombination(x.NetAmount, x.VatRate, x.GrossAmount))
                .WithMessage("Must provide either NetAmount with VatRate or GrossAmount")
                .OverridePropertyName("Amount");

            RuleFor(x => x.NetAmount)
                .GreaterThan(0)
                .WithMessage("NetAmount must be greater than 0")
                .When(x => x.NetAmount.HasValue);

            RuleFor(x => x.VatRate)
                .GreaterThanOrEqualTo(0)
                .WithMessage("VatRate must be 0 or greater")
                .LessThanOrEqualTo(100)
                .WithMessage("VatRate cannot exceed 100%")
                .When(x => x.VatRate.HasValue);

            RuleFor(x => x.GrossAmount)
                .GreaterThan(0)
                .WithMessage("GrossAmount must be greater than 0")
                .When(x => x.GrossAmount.HasValue);

            // Document validation
            RuleFor(x => x.Document)
                .Must(DocumentValidationHelper.IsValidDocumentType)
                .WithMessage("Document must be JPEG, JPG or PDF")
                .When(x => x.Document != null);

            RuleFor(x => x.Document)
                .Must(DocumentValidationHelper.IsValidDocumentSize)
                .WithMessage("Document size cannot exceed 10MB")
                .When(x => x.Document != null);
        }
    }
}
