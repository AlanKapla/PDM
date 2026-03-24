using FluentValidation;

namespace CQRS.CostEstimateTemplates.DuplicateCostEstimateTemplate
{
    public class DuplicateCostEstimateTemplateCommandValidator
        : AbstractValidator<DuplicateCostEstimateTemplateCommand>
    {
        public DuplicateCostEstimateTemplateCommandValidator()
        {
            RuleFor(x => x.SourceTemplateId)
                .NotEmpty().WithMessage("Source template ID is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Template name is required")
                .MaximumLength(200).WithMessage("Template name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }
}
