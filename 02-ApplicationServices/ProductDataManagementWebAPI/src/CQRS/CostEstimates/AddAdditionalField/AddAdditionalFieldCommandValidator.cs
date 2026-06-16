using Entities.Models.CostEstimates;
using FluentValidation;

namespace CQRS.CostEstimates.AddAdditionalField
{
    public sealed class AddAdditionalFieldCommandValidator : AbstractValidator<AddAdditionalFieldCommand>
    {
        public AddAdditionalFieldCommandValidator()
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

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Nazwa pola jest wymagana")
                .MaximumLength(256)
                .WithMessage("Nazwa pola może mieć maksymalnie 256 znaków");

            RuleFor(x => x.FieldType)
                .IsInEnum()
                .WithMessage("FieldType musi być poprawną wartością AdditionalFieldType (0-3)");

            RuleFor(x => x.Order)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Order.HasValue)
                .WithMessage("Order musi być >= 0");
        }
    }
}
