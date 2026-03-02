using FluentValidation;

namespace CQRS.CostEstimateTemplates.CreateCostEstimateTemplateFromDefault
{
    public class CreateCostEstimateTemplateFromDefaultCommandValidator
        : AbstractValidator<CreateCostEstimateTemplateFromDefaultCommand>
    {
        public CreateCostEstimateTemplateFromDefaultCommandValidator()
        {
            RuleFor(x => x.Slug)
                .NotEmpty().WithMessage("Default template slug is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Template name is required")
                .MaximumLength(200).WithMessage("Template name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }
}
