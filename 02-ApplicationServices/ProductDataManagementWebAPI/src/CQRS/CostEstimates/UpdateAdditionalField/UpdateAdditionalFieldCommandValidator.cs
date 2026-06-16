using FluentValidation;

namespace CQRS.CostEstimates.UpdateAdditionalField
{
    public sealed class UpdateAdditionalFieldCommandValidator : AbstractValidator<UpdateAdditionalFieldCommand>
    {
        public UpdateAdditionalFieldCommandValidator()
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

            RuleFor(x => x.Name)
                .MaximumLength(256)
                .When(x => x.Name is not null)
                .WithMessage("Nazwa pola może mieć maksymalnie 256 znaków");

            RuleFor(x => x.Order)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Order.HasValue)
                .WithMessage("Order musi być >= 0");
        }
    }
}
