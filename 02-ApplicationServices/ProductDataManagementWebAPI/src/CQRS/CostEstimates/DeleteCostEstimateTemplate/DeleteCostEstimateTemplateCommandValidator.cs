using FluentValidation;

namespace CQRS.CostEstimates.DeleteCostEstimateTemplate
{
    /// <summary>
    /// Walidator dla DeleteCostEstimateTemplateCommand
    /// </summary>
    public class DeleteCostEstimateTemplateCommandValidator : AbstractValidator<DeleteCostEstimateTemplateCommand>
    {
        public DeleteCostEstimateTemplateCommandValidator()
        {
            RuleFor(x => x.TemplateId)
                .NotEmpty().WithMessage("Template ID is required");
        }
    }
}
