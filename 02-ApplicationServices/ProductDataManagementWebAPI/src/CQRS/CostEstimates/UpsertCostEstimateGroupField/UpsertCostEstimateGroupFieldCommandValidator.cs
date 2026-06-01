using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostEstimates.UpsertCostEstimateGroupField
{
    public sealed class UpsertCostEstimateGroupFieldCommandValidator : AbstractValidator<UpsertCostEstimateGroupFieldCommand>
    {
        public UpsertCostEstimateGroupFieldCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();
            RuleFor(x => x.GroupId).RequiredId();

            RuleFor(x => x.StringValue)
                .MaximumLength(2000).WithMessage("StringValue cannot exceed 2000 characters.")
                .When(x => x.StringValue != null);

            // FieldDefinitionId is required only when adding (FieldValueId is null)
            RuleFor(x => x.FieldDefinitionId)
                .NotEmpty().WithMessage("'FieldDefinitionId' is required.")
                .When(x => x.FieldValueId is null);
        }
    }
}
