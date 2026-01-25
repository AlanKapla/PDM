using FluentValidation;

namespace CQRS.CostEstimates.CreateCostEstimate
{
    /// <summary>
    /// Walidator dla CreateCostEstimateCommand
    /// </summary>
    public class CreateCostEstimateCommandValidator : AbstractValidator<CreateCostEstimateCommand>
    {
        public CreateCostEstimateCommandValidator()
        {
            RuleFor(x => x.TemplateId)
                .NotEmpty().WithMessage("Template ID is required");

            RuleFor(x => x.TemplateVersionId)
                .NotEmpty().WithMessage("Template Version ID is required");

            RuleFor(x => x.SelectedCurrencyId)
                .NotEmpty().WithMessage("Currency selection is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Cost estimate name is required")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");
        }
    }
}
