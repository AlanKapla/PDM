using FluentValidation;

namespace CQRS.CostEstimateTemplates.CreateCostEstimateTemplate
{
    /// <summary>
    /// Validator dla CreateCostEstimateTemplateCommand
    /// </summary>
    public class CreateCostEstimateTemplateCommandValidator : AbstractValidator<CreateCostEstimateTemplateCommand>
    {
        public CreateCostEstimateTemplateCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Template name is required")
                .MaximumLength(200).WithMessage("Template name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }
}
