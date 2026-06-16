using FluentValidation;

namespace CQRS.CostEstimates.ReorderAdditionalFields
{
    public sealed class ReorderAdditionalFieldsCommandValidator : AbstractValidator<ReorderAdditionalFieldsCommand>
    {
        public ReorderAdditionalFieldsCommandValidator()
        {
            RuleFor(x => x.TenantId)
                .NotEmpty()
                .WithMessage("TenantId jest wymagany");

            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("ProjectId jest wymagany");

            RuleFor(x => x.CostEstimateId)
                .NotEmpty()
                .WithMessage("CostEstimateId jest wymagany");

            RuleFor(x => x.FieldIds)
                .NotEmpty()
                .WithMessage("Lista FieldIds nie może być pusta");

            RuleFor(x => x.FieldIds)
                .Must(ids => ids.Count == ids.Distinct().Count())
                .When(x => x.FieldIds.Any())
                .WithMessage("Lista FieldIds zawiera duplikaty");
        }
    }
}
