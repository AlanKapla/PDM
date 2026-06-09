# Prompt API-02: ICostEstimateAIGeneratorService + CostEstimateAIGeneratorService

## Cel
Utwórz interfejs serwisu w `Business.Interfaces` oraz implementację w `Business.AIAgent`.
Serwis wywołuje Azure OpenAI z system promptem po polsku i zwraca `AICostEstimatePreviewWeb`.

---

## Kontekst

**Wzorzec do naśladowania:** `DocumentParserService` w `Business.AIAgent/Services/`
- Używa `AzureOpenAIClient` z `AzureAIAgentOptions`
- Odpowiedź AI to czysty JSON — parsowany ręcznie
- Prompt wyłącznie po polsku

**Walidacja pól:** `CostEstimateFieldValueValidator` (FluentValidation) z `CQRS/CostEstimates/Validators/`
- Walidacja sprawdza type-mismatch i zakresy
- Jeśli walidacja zwraca błędy → serwis wysyła do AI komunikat z błędami i prosi o korektę (max 1 retry)

---

## Plik 1: Interfejs

### `src/Business/Interfaces/Services/ICostEstimateAIGeneratorService.cs`

```csharp
using Business.Interfaces.WebModels.AI;
using Entities.Models.CostEstimateTemplates;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Serwis generowania podglądu kosztorysu przez Azure OpenAI.
    /// NIE zapisuje niczego do bazy danych — zwraca tylko AICostEstimatePreviewWeb.
    /// </summary>
    public interface ICostEstimateAIGeneratorService
    {
        /// <summary>
        /// Generuje podgląd struktury kosztorysu na podstawie opisu inwestycji i szablonu.
        /// W razie błędu walidacji pól (type-mismatch, zakres) wykonuje max 1 retry z feedbackiem do AI.
        /// </summary>
        Task<AICostEstimatePreviewWeb> GeneratePreviewAsync(
            AICostEstimateRequestWeb request,
            CostEstimateTemplate template,
            CancellationToken cancellationToken);
    }
}
```

---

## Plik 2: Implementacja

### `src/Business.AIAgent/Services/CostEstimateAIGeneratorService.cs`

Implementacja musi:
1. Zbudować system prompt po polsku opisujący szablon (grupy, pozycje, pola z FieldType)
2. Zbudować user prompt z danych z `AICostEstimateRequestWeb`
3. Wysłać do OpenAI i sparsować JSON odpowiedź na `AICostEstimatePreviewWeb`
4. Zwalidować każde pole przez `CostEstimateFieldValueValidator` (`CostEstimateFieldValueContext`)
5. Jeśli są błędy walidacji — zbudować feedback i wykonać 1 dodatkowe wywołanie AI z prośbą o korektę
6. Zwrócić preview. Ostrzeżenia o polach pominiętych dodać do `Warnings`.

```csharp
using Azure.AI.OpenAI;
using Azure.Identity;
using Business.AIAgent.Configuration;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using CQRS.CostEstimates.Validators;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.ClientModel;
using System.Text;
using System.Text.Json;

namespace Business.AIAgent.Services;

public sealed class CostEstimateAIGeneratorService : ICostEstimateAIGeneratorService
{
    private readonly AzureAIAgentOptions _options;
    private readonly CostEstimateFieldValueValidator _validator;
    private readonly ILogger<CostEstimateAIGeneratorService> _logger;

    public CostEstimateAIGeneratorService(
        IOptions<AzureAIAgentOptions> options,
        CostEstimateFieldValueValidator validator,
        ILogger<CostEstimateAIGeneratorService> logger)
    {
        _options = options.Value;
        _validator = validator;
        _logger = logger;
    }

    public async Task<AICostEstimatePreviewWeb> GeneratePreviewAsync(
        AICostEstimateRequestWeb request,
        CostEstimateTemplate template,
        CancellationToken cancellationToken)
    {
        string systemPrompt = BuildSystemPrompt(template);
        string userPrompt = BuildUserPrompt(request);

        ChatClient client = BuildClient();
        List<ChatMessage> messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        ];

        string rawJson = await CallOpenAIAsync(client, messages, cancellationToken);
        AICostEstimatePreviewWeb preview = ParsePreview(rawJson, template.Id);

        // Walidacja pól — zbierz błędy
        List<string> validationErrors = ValidatePreview(preview, template);

        if (validationErrors.Count > 0)
        {
            // Retry z feedbackiem do AI
            string correctionPrompt = BuildCorrectionPrompt(validationErrors);
            messages.Add(new AssistantChatMessage(rawJson));
            messages.Add(new UserChatMessage(correctionPrompt));

            string correctedJson = await CallOpenAIAsync(client, messages, cancellationToken);
            preview = ParsePreview(correctedJson, template.Id);

            // Po retry — błędy które zostały stają się ostrzeżeniami
            List<string> remainingErrors = ValidatePreview(preview, template);
            if (remainingErrors.Count > 0)
            {
                preview = preview with
                {
                    Warnings = [..preview.Warnings, ..remainingErrors.Select(e => $"[pominięte pole] {e}")]
                };
                // Usuń pole z preview jeśli nadal jest błędne — bezpieczne: pomiń je cicho
                preview = RemoveInvalidFieldValues(preview, template);
            }
        }

        return preview;
    }

    private static string BuildSystemPrompt(CostEstimateTemplate template)
    {
        StringBuilder sb = new();
        sb.AppendLine("Jesteś ekspertem od kosztorysowania w budownictwie i remontach.");
        sb.AppendLine("Twoim zadaniem jest wygenerowanie struktury kosztorysu budowlanego na podstawie opisu inwestycji i podanego szablonu.");
        sb.AppendLine("Zawsze odpowiadaj WYŁĄCZNIE w formacie JSON, bez żadnych komentarzy, bez markdown, bez ```json.");
        sb.AppendLine();
        sb.AppendLine("## Szablon kosztorysu");
        sb.AppendLine($"Nazwa szablonu: {template.Name}");
        if (!string.IsNullOrEmpty(template.Description))
            sb.AppendLine($"Opis szablonu: {template.Description}");
        sb.AppendLine($"Można dodawać grupy: {(template.CanAddGroups ? "tak" : "nie")}");
        sb.AppendLine($"Można zagnieżdżać podgrupy: {(template.CanBranchGroups ? "tak" : "nie")}");
        if (template.MaxGroupLevel.HasValue)
            sb.AppendLine($"Maksymalny poziom zagnieżdżenia grup: {template.MaxGroupLevel}");

        sb.AppendLine();
        sb.AppendLine("### Dostępne pola grup (GroupHeaderFields):");
        foreach (CostEstimateTemplateGroupFieldDefinition f in template.GroupFieldDefinitions)
        {
            sb.AppendLine($"  - fieldDefinitionId: \"{f.Id}\", nazwa: \"{f.FieldName}\", label: \"{f.Label}\", typ: {f.FieldType}");
        }

        sb.AppendLine();
        sb.AppendLine("### Dostępne pola systemowe pozycji (ItemSystemFields):");
        foreach (CostEstimateTemplateItemSystemFieldDefinition f in template.SystemFieldDefinitions)
        {
            sb.AppendLine($"  - fieldDefinitionId: \"{f.Id}\", nazwa: \"{f.FieldName}\", label: \"{f.Label}\", typ: {f.FieldType}");
        }

        sb.AppendLine();
        sb.AppendLine("### Dostępne pola obliczeniowe pozycji (ItemCalculatedFields):");
        foreach (CostEstimateTemplateItemCalculatedFieldDefinition f in template.CalculatedFieldDefinitions)
        {
            sb.AppendLine($"  - fieldDefinitionId: \"{f.Id}\", nazwa: \"{f.FieldName}\", label: \"{f.Label}\", typ: {f.FieldType}");
        }

        if (template.GenericFieldDefinitions.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Dostępne pola generyczne pozycji (ItemGenericFields):");
            foreach (CostEstimateTemplateItemGenericFieldDefinition f in template.GenericFieldDefinitions)
            {
                sb.AppendLine($"  - fieldDefinitionId: \"{f.Id}\", nazwa: \"{f.FieldName}\", label: \"{f.Label}\", typ: {f.FieldType}");
            }
        }

        if (template.Units.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Dostępne jednostki miary:");
            foreach (CostEstimateTemplateUnit u in template.Units)
            {
                sb.AppendLine($"  - \"{u.Symbol}\" ({u.Name})");
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Zasady przypisywania wartości pól:");
        sb.AppendLine("- Pola NUMERYCZNE (typy: Quantity, UnitPrice, Budget, VatRate, Value itp.): użyj \"decimalValue\": liczba");
        sb.AppendLine("- Pola TEKSTOWE (typy: Name, Description, Notes, Status, Responsible itp.): użyj \"stringValue\": \"tekst\"");
        sb.AppendLine("- Pola DATY (typy: StartDate, EndDate): użyj \"dateTimeValue\": \"YYYY-MM-DD\"");
        sb.AppendLine("- Pola BOOLOWE: użyj \"boolValue\": true/false");
        sb.AppendLine("- VatRate musi być w zakresie 0-1 (np. 0.23 = 23%)");
        sb.AppendLine("- Ilość (Quantity) musi być >= 0");
        sb.AppendLine("- Ceny (UnitPrice, Value) muszą być >= 0");
        sb.AppendLine("- NIE wypełniaj pól będących kolekcjami (Options, Components) ani plików (Files)");
        sb.AppendLine("- Jeśli nie znasz wartości pola — pomiń je (nie dodawaj wpisu)");
        sb.AppendLine();
        sb.AppendLine("## Format odpowiedzi (JSON):");
        sb.AppendLine("""
{
  "suggestedName": "Nazwa kosztorysu",
  "suggestedDescription": "Opis (opcjonalny)",
  "groups": [
    {
      "tempId": "g1",
      "parentTempId": null,
      "name": "Nazwa grupy",
      "order": 1,
      "fieldValues": [
        { "fieldDefinitionId": "GUID", "stringValue": "wartość" }
      ],
      "items": [
        {
          "tempId": "i1",
          "name": "Nazwa pozycji",
          "order": 1,
          "fieldValues": [
            { "fieldDefinitionId": "GUID", "decimalValue": 10.5 }
          ]
        }
      ]
    }
  ]
}
""");

        return sb.ToString();
    }

    private static string BuildUserPrompt(AICostEstimateRequestWeb request)
    {
        StringBuilder sb = new();
        sb.AppendLine("Wygeneruj kosztorys dla następującej inwestycji:");
        sb.AppendLine($"- Co budujesz: {request.InvestmentType}");

        if (!string.IsNullOrEmpty(request.FinishingStandard))
            sb.AppendLine($"- Stan wykończenia: {request.FinishingStandard}");
        if (request.Budget.HasValue)
            sb.AppendLine($"- Szacowany budżet: {request.Budget:F2} PLN brutto");
        if (request.Area.HasValue)
            sb.AppendLine($"- Powierzchnia/zakres: {request.Area} {request.AreaUnit ?? "m²"}");
        if (!string.IsNullOrEmpty(request.Location))
            sb.AppendLine($"- Lokalizacja: {request.Location}");
        if (request.CompletionYear.HasValue)
            sb.AppendLine($"- Rok ukończenia: {request.CompletionYear}");
        if (!string.IsNullOrEmpty(request.AdditionalRequirements))
            sb.AppendLine($"- Dodatkowe wymagania: {request.AdditionalRequirements}");

        sb.AppendLine();
        sb.AppendLine("Wygeneruj realistyczną strukturę kosztorysu z etapami robót (grupy) i szczegółowymi pozycjami.");
        sb.AppendLine("Wypełnij tylko te pola, dla których możesz wygenerować sensowną wartość.");

        return sb.ToString();
    }

    private static string BuildCorrectionPrompt(List<string> errors)
    {
        StringBuilder sb = new();
        sb.AppendLine("Wygenerowany kosztorys zawiera błędy walidacji. Popraw je i zwróć poprawiony JSON:");
        sb.AppendLine();
        foreach (string error in errors)
        {
            sb.AppendLine($"- {error}");
        }
        sb.AppendLine();
        sb.AppendLine("Pamiętaj: odpowiedz WYŁĄCZNIE poprawionym JSON, bez komentarzy.");

        return sb.ToString();
    }

    private List<string> ValidatePreview(AICostEstimatePreviewWeb preview, CostEstimateTemplate template)
    {
        List<string> errors = [];
        Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allFieldDefs = BuildFieldDefDictionary(template);

        foreach (AIGroupPreviewWeb group in preview.Groups)
        {
            ValidateFieldValues(group.FieldValues, group.Name, allFieldDefs, errors);
            foreach (AIItemPreviewWeb item in group.Items)
            {
                ValidateFieldValues(item.FieldValues, item.Name, allFieldDefs, errors);
            }
        }

        return errors;
    }

    private void ValidateFieldValues(
        List<AIFieldValueWeb> fieldValues,
        string ownerName,
        Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allFieldDefs,
        List<string> errors)
    {
        foreach (AIFieldValueWeb fv in fieldValues)
        {
            if (!allFieldDefs.TryGetValue(fv.FieldDefinitionId, out CostEstimateTemplateFieldDefinitionBase? fieldDef))
                continue;

            CostEstimateFieldTypeConfig typeConfig = FieldTypeConfigRegistry.Get(fieldDef.FieldType);

            CostEstimateFieldValueContext ctx = new CostEstimateFieldValueContext(
                FieldType: fieldDef.FieldType,
                FieldLabel: fieldDef.Label,
                FieldTypeConfig: typeConfig,
                StringValue: fv.StringValue,
                DecimalValue: fv.DecimalValue,
                BoolValue: fv.BoolValue,
                DateTimeValue: fv.DateTimeValue);

            ValidationResult result = _validator.Validate(ctx);
            if (!result.IsValid)
            {
                foreach (ValidationFailure failure in result.Errors)
                {
                    errors.Add($"[{ownerName}] Pole '{fieldDef.Label}': {failure.ErrorMessage}");
                }
            }
        }
    }

    private static AICostEstimatePreviewWeb RemoveInvalidFieldValues(
        AICostEstimatePreviewWeb preview,
        CostEstimateTemplate template)
    {
        Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allFieldDefs = BuildFieldDefDictionary(template);

        List<AIGroupPreviewWeb> cleanGroups = preview.Groups.Select(g =>
        {
            List<AIFieldValueWeb> cleanGroupFields = g.FieldValues
                .Where(fv => IsValidFieldValue(fv, allFieldDefs))
                .ToList();

            List<AIItemPreviewWeb> cleanItems = g.Items.Select(i =>
                i with { FieldValues = i.FieldValues.Where(fv => IsValidFieldValue(fv, allFieldDefs)).ToList() }
            ).ToList();

            return g with { FieldValues = cleanGroupFields, Items = cleanItems };
        }).ToList();

        return preview with { Groups = cleanGroups };
    }

    private static bool IsValidFieldValue(
        AIFieldValueWeb fv,
        Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allFieldDefs)
    {
        if (!allFieldDefs.TryGetValue(fv.FieldDefinitionId, out CostEstimateTemplateFieldDefinitionBase? fieldDef))
            return false;

        CostEstimateFieldTypeConfig typeConfig = FieldTypeConfigRegistry.Get(fieldDef.FieldType);
        if (typeConfig.IsCollection || typeConfig.IsFile)
            return false;

        return true;
    }

    private static Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> BuildFieldDefDictionary(
        CostEstimateTemplate template)
    {
        return template.GroupFieldDefinitions
            .Cast<CostEstimateTemplateFieldDefinitionBase>()
            .Concat(template.SystemFieldDefinitions)
            .Concat(template.CalculatedFieldDefinitions)
            .Concat(template.GenericFieldDefinitions)
            .ToDictionary(f => f.Id);
    }

    private AICostEstimatePreviewWeb ParsePreview(string rawJson, Guid templateId)
    {
        string jsonToParse = rawJson ?? string.Empty;

        // Usuń markdown fences jeśli AI je dodało mimo prośby
        if (jsonToParse.Contains("```"))
        {
            int start = jsonToParse.IndexOf('{');
            int end = jsonToParse.LastIndexOf('}');
            if (start >= 0 && end >= start)
                jsonToParse = jsonToParse[start..(end + 1)];
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(jsonToParse);
            JsonElement root = doc.RootElement;

            string suggestedName = root.TryGetProperty("suggestedName", out JsonElement nameProp)
                ? nameProp.GetString() ?? "Kosztorys AI"
                : "Kosztorys AI";

            string? suggestedDescription = root.TryGetProperty("suggestedDescription", out JsonElement descProp)
                ? descProp.GetString()
                : null;

            List<AIGroupPreviewWeb> groups = [];
            if (root.TryGetProperty("groups", out JsonElement groupsArr) && groupsArr.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement g in groupsArr.EnumerateArray())
                {
                    groups.Add(ParseGroup(g));
                }
            }

            return new AICostEstimatePreviewWeb
            {
                TemplateId = templateId,
                SuggestedName = suggestedName,
                SuggestedDescription = suggestedDescription,
                Groups = groups,
                Warnings = []
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI cost estimate preview JSON");
            return new AICostEstimatePreviewWeb
            {
                TemplateId = templateId,
                SuggestedName = "Kosztorys AI",
                Groups = [],
                Warnings = ["Nie udało się sparsować odpowiedzi AI — spróbuj ponownie."]
            };
        }
    }

    private static AIGroupPreviewWeb ParseGroup(JsonElement g)
    {
        string tempId = g.TryGetProperty("tempId", out JsonElement tid) ? tid.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();
        string? parentTempId = g.TryGetProperty("parentTempId", out JsonElement ptid) && ptid.ValueKind == JsonValueKind.String ? ptid.GetString() : null;
        string name = g.TryGetProperty("name", out JsonElement nProp) ? nProp.GetString() ?? string.Empty : string.Empty;
        int order = g.TryGetProperty("order", out JsonElement oProp) && oProp.ValueKind == JsonValueKind.Number ? oProp.GetInt32() : 0;

        List<AIFieldValueWeb> fieldValues = ParseFieldValues(g);
        List<AIItemPreviewWeb> items = [];

        if (g.TryGetProperty("items", out JsonElement itemsArr) && itemsArr.ValueKind == JsonValueKind.Array)
        {
            int itemOrder = 0;
            foreach (JsonElement item in itemsArr.EnumerateArray())
            {
                items.Add(ParseItem(item, itemOrder++));
            }
        }

        return new AIGroupPreviewWeb
        {
            TempId = tempId,
            ParentTempId = parentTempId,
            Name = name,
            Order = order,
            FieldValues = fieldValues,
            Items = items
        };
    }

    private static AIItemPreviewWeb ParseItem(JsonElement item, int fallbackOrder)
    {
        string tempId = item.TryGetProperty("tempId", out JsonElement tid) ? tid.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();
        string name = item.TryGetProperty("name", out JsonElement nProp) ? nProp.GetString() ?? string.Empty : string.Empty;
        int order = item.TryGetProperty("order", out JsonElement oProp) && oProp.ValueKind == JsonValueKind.Number ? oProp.GetInt32() : fallbackOrder;
        List<AIFieldValueWeb> fieldValues = ParseFieldValues(item);

        return new AIItemPreviewWeb
        {
            TempId = tempId,
            Name = name,
            Order = order,
            FieldValues = fieldValues
        };
    }

    private static List<AIFieldValueWeb> ParseFieldValues(JsonElement parent)
    {
        List<AIFieldValueWeb> result = [];

        if (!parent.TryGetProperty("fieldValues", out JsonElement fvArr) || fvArr.ValueKind != JsonValueKind.Array)
            return result;

        foreach (JsonElement fv in fvArr.EnumerateArray())
        {
            if (!fv.TryGetProperty("fieldDefinitionId", out JsonElement fidProp))
                continue;
            if (!Guid.TryParse(fidProp.GetString(), out Guid fieldDefId))
                continue;

            decimal? decimalValue = fv.TryGetProperty("decimalValue", out JsonElement dProp) && dProp.ValueKind == JsonValueKind.Number
                ? dProp.GetDecimal() : null;
            string? stringValue = fv.TryGetProperty("stringValue", out JsonElement sProp) && sProp.ValueKind == JsonValueKind.String
                ? sProp.GetString() : null;
            bool? boolValue = fv.TryGetProperty("boolValue", out JsonElement bProp) && bProp.ValueKind == JsonValueKind.True || bProp.ValueKind == JsonValueKind.False
                ? bProp.GetBoolean() : null;
            DateTime? dateTimeValue = fv.TryGetProperty("dateTimeValue", out JsonElement dtProp) && dtProp.ValueKind == JsonValueKind.String
                ? DateTime.TryParse(dtProp.GetString(), out DateTime dt) ? dt : null : null;

            result.Add(new AIFieldValueWeb
            {
                FieldDefinitionId = fieldDefId,
                DecimalValue = decimalValue,
                StringValue = stringValue,
                BoolValue = boolValue,
                DateTimeValue = dateTimeValue
            });
        }

        return result;
    }

    private async Task<string> CallOpenAIAsync(
        ChatClient client,
        List<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        ChatCompletion response = await client.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        return response.Content[0].Text;
    }

    private ChatClient BuildClient()
    {
        AzureOpenAIClient azureClient = string.IsNullOrWhiteSpace(_options.ApiKey)
            ? new AzureOpenAIClient(new Uri(_options.Endpoint), new DefaultAzureCredential())
            : new AzureOpenAIClient(new Uri(_options.Endpoint), new ApiKeyCredential(_options.ApiKey));

        return azureClient.GetChatClient(_options.DefaultDeployment);
    }
}
```

---

## Uwagi implementacyjne

1. **`FieldTypeConfigRegistry.Get(FieldType)`** — sprawdź czy taki helper istnieje w projekcie. Jeśli nie, użyj metody statycznej `CostEstimateFieldTypeConfig.FromFieldType(FieldType)` albo sprawdź jak `CostEstimateFieldValueContext` jest budowany w `UpsertCostEstimateItemFieldCommandHandler` i zastosuj ten sam wzorzec.

2. **`template.GenericFieldDefinitions`** — sprawdź czy ta właściwość istnieje w `CostEstimateTemplate`. Jeśli szablon nie ma tej kolekcji, pomiń ten fragment.

3. **`CostEstimateTemplateItemGenericFieldDefinition`** — sprawdź czy taka klasa istnieje w `Entities.Models.CostEstimateTemplates`. Jeśli nie, pomiń.

4. Serwis nie używa `AgentRunner` — to prostszy, bezpośredni call do Azure OpenAI, tak jak `DocumentParserService`.

---

## Weryfikacja
```
dotnet build src/Business.AIAgent/Business.AIAgent.csproj
dotnet build src/Business/Business.csproj
```
Oczekiwany wynik: Build succeeded, 0 errors.
