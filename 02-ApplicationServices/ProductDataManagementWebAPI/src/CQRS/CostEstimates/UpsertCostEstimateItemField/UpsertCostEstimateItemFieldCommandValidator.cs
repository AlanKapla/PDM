using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostEstimates.UpsertCostEstimateItemField
{
    public sealed class UpsertCostEstimateItemFieldCommandValidator : AbstractValidator<UpsertCostEstimateItemFieldCommand>
    {
        public UpsertCostEstimateItemFieldCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();
            RuleFor(x => x.ItemId).RequiredId();

            // FieldDefinitionId is required only when adding (FieldValueId is null)
            RuleFor(x => x.FieldDefinitionId)
                .NotEmpty().WithMessage("'FieldDefinitionId' is required.")
                .When(x => x.FieldValueId is null);
        }
    }
}
