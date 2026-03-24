using FluentValidation;

namespace CQRS.CostEstimates.UpsertCostEstimateGroupField
{
    public class UpsertCostEstimateGroupFieldCommandValidator : AbstractValidator<UpsertCostEstimateGroupFieldCommand>
    {
        public UpsertCostEstimateGroupFieldCommandValidator()
        {
            RuleFor(x => x.CostEstimateId)
                .NotEmpty().WithMessage("Cost estimate ID is required");

            RuleFor(x => x.GroupId)
                .NotEmpty().WithMessage("Group ID is required");

            // FieldDefinitionId is required only when adding (FieldValueId is null)
            RuleFor(x => x.FieldDefinitionId)
                .NotEmpty().WithMessage("Field definition ID is required when creating a new field value")
                .When(x => x.FieldValueId is null);
        }
    }
}
