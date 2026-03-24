using FluentValidation;

namespace CQRS.CostEstimateTemplates.DeleteCostEstimateTemplate
{
    public class DeleteCostEstimateTemplateCommandValidator : AbstractValidator<DeleteCostEstimateTemplateCommand>
    {
        public DeleteCostEstimateTemplateCommandValidator()
        {
            RuleFor(x => x.TemplateId)
                .NotEmpty().WithMessage("Template ID is required");
        }
    }
}
