using CQRS.CostEstimates.Validators;
using Entities.Models;
using FluentValidation;
using Repositiories.Repository.Interfaces;

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

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Cost estimate name is required")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");
        }
    }
}
