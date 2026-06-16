using FluentValidation;

namespace CQRS.CostEstimates.DeleteAdditionalField
{
    public sealed class DeleteAdditionalFieldCommandValidator : AbstractValidator<DeleteAdditionalFieldCommand>
    {
        public DeleteAdditionalFieldCommandValidator()
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

            RuleFor(x => x.FieldId)
                .NotEmpty()
                .WithMessage("FieldId jest wymagany");
        }
    }
}
