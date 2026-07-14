# Prompt API-03: CQRS — GenerateCostEstimateAIPreviewCommand

## Cel
Utwórz Command + Handler + Validator dla operacji generowania podglądu kosztorysu przez AI.
**Nie zapisuje niczego do bazy danych.**

---

## Lokalizacja plików

```
src/CQRS/CostEstimates/GenerateCostEstimateAIPreview/
  GenerateCostEstimateAIPreviewCommand.cs
  GenerateCostEstimateAIPreviewCommandHandler.cs
  GenerateCostEstimateAIPreviewCommandValidator.cs
```

---

## Plik 1: Command

### `GenerateCostEstimateAIPreviewCommand.cs`

```csharp
using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.AI;

namespace CQRS.CostEstimates.GenerateCostEstimateAIPreview
{
    /// <summary>
    /// Generuje podgląd struktury kosztorysu przez AI na podstawie opisu inwestycji.
    /// Nie zapisuje niczego do bazy danych — zwraca AICostEstimatePreviewWeb.
    /// Użytkownik przegląda podgląd i zatwierdza przez CreateCostEstimateFromAIPreview.
    /// </summary>
    public sealed record GenerateCostEstimateAIPreviewCommand : CostEstimateRequestBase, IRequestCommand<AICostEstimatePreviewWeb>
    {
        /// <summary>Dane wejściowe od użytkownika (opis inwestycji, szablon, budżet itp.)</summary>
        public AICostEstimateRequestWeb Request { get; init; } = default!;

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
```

---

## Plik 2: Handler

### `GenerateCostEstimateAIPreviewCommandHandler.cs`

```csharp
using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.GenerateCostEstimateAIPreview
{
    public sealed class GenerateCostEstimateAIPreviewCommandHandler
        : IRequestHandler<GenerateCostEstimateAIPreviewCommand, AICostEstimatePreviewWeb>
    {
        private readonly IReadRepository<CostEstimateTemplate> templateRepository;
        private readonly ICostEstimateAIGeneratorService aiGeneratorService;
        private readonly Business.Interfaces.Model.ICurrentUser currentUser;

        public GenerateCostEstimateAIPreviewCommandHandler(
            IReadRepository<CostEstimateTemplate> templateRepository,
            ICostEstimateAIGeneratorService aiGeneratorService,
            Business.Interfaces.Model.ICurrentUser currentUser)
        {
            this.templateRepository = templateRepository;
            this.aiGeneratorService = aiGeneratorService;
            this.currentUser = currentUser;
        }

        public async Task<AICostEstimatePreviewWeb> Handle(
            GenerateCostEstimateAIPreviewCommand request,
            CancellationToken cancellationToken)
        {
            // Weryfikacja że szablon istnieje i należy do użytkownika
            CostEstimateTemplate template = await templateRepository.GetFirstBySearch(
                t => t.Id == request.Request.TemplateId
                  && !t.IsDeleted
                  && t.OwnerId == currentUser.Id)
                ?? throw new NotFoundApiException(
                    nameof(CostEstimateTemplate),
                    request.Request.TemplateId.ToString());

            return await aiGeneratorService.GeneratePreviewAsync(
                request.Request,
                template,
                cancellationToken);
        }
    }
}
```

---

## Plik 3: Validator

### `GenerateCostEstimateAIPreviewCommandValidator.cs`

```csharp
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

            RuleFor(x => x.Request.TemplateId)
                .NotEmpty()
                .WithMessage("Szablon kosztorysu jest wymagany.");

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
```

---

## Konwencje
- Użyj `IReadRepository<CostEstimateTemplate>` (tylko odczyt — handler nie modyfikuje danych)
- `ICurrentUser` z `Business.Interfaces.Model`
- Handler implementuje `IRequestHandler<TCommand, TResponse>` z MediatR
- Validator dziedziczy po `AbstractValidator<T>` z FluentValidation

## Weryfikacja
```
dotnet build src/CQRS/CQRS.csproj
```
Oczekiwany wynik: Build succeeded, 0 errors.
