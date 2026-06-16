using FluentValidation;

namespace CQRS.CostEstimates.CreateCostEstimateFromAIPreview
{
    public sealed class CreateCostEstimateFromAIPreviewCommandValidator
        : AbstractValidator<CreateCostEstimateFromAIPreviewCommand>
    {
        public CreateCostEstimateFromAIPreviewCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Nazwa kosztorysu jest wymagana.")
                .MaximumLength(200)
                .WithMessage("Nazwa kosztorysu nie może przekraczać 200 znaków.");

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .When(x => x.Description is not null)
                .WithMessage("Opis nie może przekraczać 2000 znaków.");

            RuleFor(x => x.Preview)
                .NotNull()
                .WithMessage("Podgląd kosztorysu jest wymagany.");

            RuleFor(x => x.Preview.Groups)
                .NotEmpty()
                .When(x => x.Preview is not null)
                .WithMessage("Kosztorys musi zawierać co najmniej jedną grupę.");
        }
    }
}
