using FluentValidation;

namespace CQRS.CostEstimates.UpsertCostEstimateItemField
{
    public class UpsertCostEstimateItemFieldCommandValidator : AbstractValidator<UpsertCostEstimateItemFieldCommand>
    {
        public UpsertCostEstimateItemFieldCommandValidator()
        {
            RuleFor(x => x.CostEstimateId)
                .NotEmpty().WithMessage("Cost estimate ID is required");

            RuleFor(x => x.ItemId)
                .NotEmpty().WithMessage("Item ID is required");

            // FieldDefinitionId is required only when adding (FieldValueId is null)
            RuleFor(x => x.FieldDefinitionId)
                .NotEmpty().WithMessage("Field definition ID is required when creating a new field value")
                .When(x => x.FieldValueId is null);
        }
    }
}
