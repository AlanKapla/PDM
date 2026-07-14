# Prompt: ai-edit-api-02 — Nowe DTOs + serwis ICostEstimateAIEditService

## Cel

Stworzyć nowy web model `AICostEditPreviewWeb` oraz interfejs i implementację serwisu do generowania propozycji edycji kosztorysu przez AI.

## Pliki do utworzenia

### 1. Web model: `Business/Interfaces/WebModels/AI/AICostEditPreviewWeb.cs`

```csharp
namespace Business.Interfaces.WebModels.AI;

/// <summary>
/// Propozycja edycji istniejącego kosztorysu przez AI.
/// Zawiera pełny stan kosztorysu PO edycji (nie diff).
/// Istniejące grupy/pozycje mają rzeczywiste GUID, nowe mają tempId z guid=0000...0000.
/// </summary>
public sealed record AICostEditPreviewWeb
{
    /// <summary>Krótki opis co zostało zmienione (generowany przez AI).</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Proponowana nowa nazwa kosztorysu (null = bez zmian).</summary>
    public string? SuggestedName { get; init; }

    /// <summary>Proponowany nowy opis (null = bez zmian).</summary>
    public string? SuggestedDescription { get; init; }

    /// <summary>
    /// Pełna lista grup PO edycji (istniejące + nowe, bez usuniętych).
    /// Istniejące mają wypełnione Id (Guid). Nowe mają Id=Guid.Empty i tempId.
    /// </summary>
    public List<AIGroupPreviewWeb> Groups { get; init; } = [];

    /// <summary>Ostrzeżenia (np. "pole X pominięte bo brak danych").</summary>
    public List<string> Warnings { get; init; } = [];
}

/// <summary>
/// Żądanie edycji kosztorysu przez AI.
/// </summary>
public sealed record AICostEditRequestWeb
{
    /// <summary>Naturalny język: co użytkownik chce zmienić.</summary>
    public string UserRequest { get; init; } = string.Empty;
}
```

Uwaga: `AIGroupPreviewWeb`, `AIItemPreviewWeb`, `AIComponentPreviewWeb`, `AIFieldValueWeb` już istnieją w tym samym folderze — nie trzeba ich tworzyć.

### 2. Interfejs serwisu: `Business/Interfaces/Services/ICostEstimateAIEditService.cs`

```csharp
using Business.Interfaces.WebModels.AI;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;

namespace Business.Interfaces.Services;

/// <summary>
/// Serwis edycji kosztorysu przez AI — generuje propozycję zmian.
/// NIE zapisuje niczego do bazy danych.
/// </summary>
public interface ICostEstimateAIEditService
{
    /// <summary>
    /// Generuje propozycję edycji istniejącego kosztorysu.
    /// Przyjmuje aktualny stan kosztorysu i request użytkownika.
    /// Zwraca AICostEditPreviewWeb z pełnym stanem PO edycji.
    /// </summary>
    Task<AICostEditPreviewWeb> GenerateEditPreviewAsync(
        CostEstimate costEstimate,
        CostEstimateTemplate template,
        Dictionary<Guid, CostEstimateGroup> groupsDict,
        Dictionary<Guid, CostEstimateGroupFieldValue> groupFieldValuesDict,
        Dictionary<Guid, CostEstimateItem> itemsDict,
        Dictionary<Guid, CostEstimateItemFieldValue> itemFieldValuesDict,
        string userRequest,
        CancellationToken cancellationToken);
}
```

### 3. Implementacja: `Business/Implementation/Services/AI/CostEstimateAIEditService.cs`

Wzoruj się na `CostEstimateAIGeneratorService.cs` — ten sam wzorzec:
1. Zbuduj kontekst (aktualny stan kosztorysu + template schema + request użytkownika)
2. Wywołaj agenta `cost-estimate-editor` przez `_agentRunner.RunAsync`
3. Sparsuj odpowiedź na `AICostEditPreviewWeb`
4. Usuń nieprawidłowe field values (wzór: `RemoveInvalidFieldValues`)

**Szczegóły:**

```csharp
public sealed class CostEstimateAIEditService : ICostEstimateAIEditService
{
    private readonly IAgentRunner _agentRunner;
    private readonly ILogger<CostEstimateAIEditService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    // Wstrzyknij przez DI

    public async Task<AICostEditPreviewWeb> GenerateEditPreviewAsync(
        CostEstimate costEstimate,
        CostEstimateTemplate template,
        Dictionary<Guid, CostEstimateGroup> groupsDict,
        Dictionary<Guid, CostEstimateGroupFieldValue> groupFieldValuesDict,
        Dictionary<Guid, CostEstimateItem> itemsDict,
        Dictionary<Guid, CostEstimateItemFieldValue> itemFieldValuesDict,
        string userRequest,
        CancellationToken cancellationToken)
    {
        AgentContext context = new();

        // 1. Zbuduj message dla agenta
        string message = BuildEditMessage(
            costEstimate, template, groupsDict, groupFieldValuesDict,
            itemsDict, itemFieldValuesDict, userRequest);

        // 2. Wywołaj agenta
        AgentRunResult result = await _agentRunner.RunAsync(
            "cost-estimate-editor", message, context, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("cost-estimate-editor failed: {Error}", result.ErrorMessage);
            return EmptyEditPreview("Agent edytora nie zwrócił odpowiedzi.");
        }

        // 3. Parsuj odpowiedź
        AICostEditPreviewWeb? preview = ParseEditPreview(result.Response);
        if (preview is null)
        {
            return EmptyEditPreview("Nie udało się sparsować odpowiedzi agenta.");
        }

        // 4. Usuń nieprawidłowe field values
        return RemoveInvalidFieldValues(preview, template);
    }
}
```

**BuildEditMessage** — buduje tekst dla agenta zawierający:
- Stan kosztorysu: nazwa, opis, status, sumy
- Grupy: dla każdej nazwa, ID, pole fieldValues, pozycje
- Pozycje: dla każdej nazwa, ID, RelationType, fieldValues, komponenty
- Template schema: pola, jednostki (użyj `BuildTemplateSchema` z `CostEstimateAIGeneratorService` lub odpowiednik)
- Request użytkownika: `REQUEST: {userRequest}`

**ParseEditPreview** — wzoruj się na `ParseSingleGroup` i `ParseGroupPlan`:
- `ExtractJson(response)` — wyciąga pierwszy `{...}`
- `JsonSerializer.Deserialize<AICostEditPreviewWeb>(json, _jsonOptions)`

**RemoveInvalidFieldValues** — kopiuj z `CostEstimateAIGeneratorService.RemoveInvalidFieldValues` — filtruje field values które nie pasują do template field definitions. Użyj też `IsValidFieldValue` i `BuildFieldDefDictionary`.

**EmptyEditPreview** — zwraca pusty preview z warningiem:
```csharp
private static AICostEditPreviewWeb EmptyEditPreview(string warning)
    => new()
    {
        Summary = "Nie udało się wygenerować propozycji edycji.",
        Groups = [],
        Warnings = [warning]
    };
```

## Weryfikacja

1. Plik `AICostEditPreviewWeb.cs` istnieje w `Business/Interfaces/WebModels/AI/`
2. Plik `ICostEstimateAIEditService.cs` istnieje w `Business/Interfaces/Services/`
3. Plik `CostEstimateAIEditService.cs` istnieje w `Business/Implementation/Services/AI/`
4. Build API przechodzi
