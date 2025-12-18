using FluentValidation;
using Entities.Models.CostEstimateTemplateDefinitions;
using Business.Implementation.Validators;

namespace CQRS.CostEstimates.UpdateCostEstimateTemplate
{
    /// <summary>
    /// Walidator dla UpdateCostEstimateTemplateCommand
    /// </summary>
    public class UpdateCostEstimateTemplateCommandValidator : AbstractValidator<UpdateCostEstimateTemplateCommand>
    {
        public UpdateCostEstimateTemplateCommandValidator()
        {
            RuleFor(x => x.TemplateId)
                .NotEmpty().WithMessage("Template ID is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Template name is required")
                .MaximumLength(200).WithMessage("Template name cannot exceed 200 characters")
                .Matches("^[a-zA-Z0-9 ąćęłńóśźżĄĆĘŁŃÓŚŹŻ_-]+$")
                .WithMessage("Template name can only contain letters, numbers, spaces, underscores and hyphens");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

            RuleFor(x => x.TemplateStructure)
                .NotNull().WithMessage("Template structure is required");

            // Validate TemplateStructure entity properties
            When(x => x.TemplateStructure != null, () =>
            {
                RuleFor(x => x.TemplateStructure.MaxGroupLevel)
                    .GreaterThan(0).When(x => x.TemplateStructure.MaxGroupLevel.HasValue)
                    .WithMessage("Max group level must be greater than 0");

                RuleFor(x => x.TemplateStructure.GroupDefinition)
                    .NotNull().WithMessage("Group definition is required");

                RuleFor(x => x.TemplateStructure.WorkScopeFieldsDefinition)
                    .NotNull().WithMessage("Work scope fields definition is required");
                
                // Validate template structure using CostEstimateTemplateStructureValidator
                // This validates that only ValueNet, ValueGross, UnitVat and TotalVat can be summable
                RuleFor(x => x.TemplateStructure)
                    .Custom((templateStructure, context) =>
                    {
                        var validator = new CostEstimateTemplateStructureValidator(templateStructure);
                        var result = validator.Validate();
                        
                        if (!result.IsValid)
                        {
                            foreach (var error in result.Errors)
                            {
                                context.AddFailure("TemplateStructure", error);
                            }
                        }
                    });
            });
        }
    }
}
