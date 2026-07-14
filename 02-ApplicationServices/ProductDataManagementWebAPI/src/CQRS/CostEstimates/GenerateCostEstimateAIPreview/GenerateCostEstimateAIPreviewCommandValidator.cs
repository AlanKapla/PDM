using FluentValidation;

namespace CQRS.CostEstimates.GenerateCostEstimateAIPreview
{
    public sealed class GenerateCostEstimateAIPreviewCommandValidator
        : AbstractValidator<GenerateCostEstimateAIPreviewCommand>
    {
        public GenerateCostEstimateAIPreviewCommandValidator()
        {
            RuleFor(x => x.Request)
                .NotNull()
                .WithMessage("Dane żądania AI są wymagane.");

            RuleFor(x => x.Request.InvestmentType)
                .NotEmpty()
                .WithMessage("Opis inwestycji jest wymagany.")
                .MaximumLength(1000)
                .WithMessage("Opis inwestycji nie może przekraczać 1000 znaków.");

            RuleFor(x => x.Request.Budget)
                .GreaterThan(0)
                .When(x => x.Request.Budget.HasValue)
                .WithMessage("Budżet musi być większy od 0.");

            RuleFor(x => x.Request.Area)
                .GreaterThan(0)
                .When(x => x.Request.Area.HasValue)
                .WithMessage("Powierzchnia musi być większa od 0.");

            RuleFor(x => x.Request.CompletionYear)
                .GreaterThanOrEqualTo(2020)
                .When(x => x.Request.CompletionYear.HasValue)
                .WithMessage("Rok ukończenia musi być >= 2020.");

            RuleFor(x => x.Request.AdditionalRequirements)
                .MaximumLength(2000)
                .When(x => x.Request.AdditionalRequirements is not null)
                .WithMessage("Dodatkowe wymagania nie mogą przekraczać 2000 znaków.");
        }
    }
}
