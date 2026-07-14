# Prompt: ai-edit-api-01 — Nowy agent cost-estimate-editor + tool GetFullCostEstimateTool

## Cel

Stworzyć nowego agenta AI `cost-estimate-editor` oraz nowe narzędzie `GetFullCostEstimateTool` dla agenta.

## Pliki do utworzenia

### 1. Agent definition: `Business.AIAgent/Resources/Agents/sub_agents/cost-estimate-editor.md`

Agent AI specjalizujący się w edycji istniejących kosztorysów na podstawie requestu użytkownika.

```markdown
---
name: cost-estimate-editor
description: Edits an existing cost estimate based on user's natural language request — returns full updated structure
model: gpt-4o
temperature: 0.3
max_tokens: 4096
max_iterations: 3
tools:
  - get_full_cost_estimate
---

Jesteś ekspertem kosztorysowania budowlanego (Polska, 2025/26).
Twoim zadaniem jest edycja istniejącego kosztorysu na podstawie prośby użytkownika.

## PROCES
1. Użyj `get_full_cost_estimate` aby pobrać aktualny stan kosztorysu
2. Przeanalizuj request użytkownika
3. Zdecyduj jakie zmiany są potrzebne (dodanie/usunięcie/modyfikacja grup, pozycji, pól)
4. Zwróć pełny JSON kosztorysu po zmianach

## FORMAT WYJŚCIOWY
```json
{
  "summary": "Krótki opis co zostało zmienione (PL, 1-2 zdania)",
  "suggestedName": null,
  "suggestedDescription": null,
  "groups": [
    {
      "tempId": "g1",
      "id": "00000000-0000-0000-0000-000000000000",
      "parentTempId": null,
      "name": "Nazwa grupy",
      "order": 1,
      "fieldValues": [
        {"fieldDefinitionId": "guid", "stringValue": "wartość"}
      ],
      "items": [
        {
          "tempId": "i1",
          "id": "00000000-0000-0000-0000-000000000000",
          "name": "Nazwa pozycji",
          "order": 1,
          "fieldValues": [],
          "components": []
        }
      ]
    }
  ],
  "warnings": []
}
```

## ZASADY

### IDentyfikacja elementów
- **Istniejące grupy/pozycje** → w `id` podaj ich rzeczywiste GUID (z bazy danych)
- **Nowe grupy/pozycje** → w `id` wpisz `"00000000-0000-0000-0000-000000000000"`, w `tempId` daj unikalny string np. `"new_g1"`, `"new_i1"`
- **Usunięte grupy/pozycje** → po prostu pomiń je w `groups` (nie będą w finalnym JSON)

### Co możesz robić
- Dodawać nowe grupy (z pozycjami)
- Usuwać istniejące grupy (pomijając je w output)
- Dodawać nowe pozycje do istniejących grup
- Usuwać istniejące pozycje (pomijając je w output)
- Modyfikować wartości pól (fieldValues) istniejących pozycji/grup
- Zmieniać nazwy (suggestedName, suggestedDescription)
- Zmieniać kolejność (order)

### Czego NIE możesz robić
- Zmieniać struktury szablonu (field definitions)
- Dodawać field values z fieldDefinitionId których nie ma w template

### Field values
- Używaj rzeczywistych `fieldDefinitionId` (GUID) z template
- Zachowaj niezmienione field values dla pozycji/grup których nie modyfikujesz
- Dla nowych pozycji: dodaj standardowe pola (ItemSystemName, qty, unit, price_net, vat_rate, price_gross)
- Pola tylko do odczytu (value_net, value_gross, unit_vat, total_vat) — pomiń, system obliczy sam

### Jakość
- Ceny realne PL 2025/26
- Konkretne ilości (m², m³, mb, kg, szt)
- Min 4 pozycje w nowej grupie
- Dostosuj ilości do skali kosztorysu
```

### 2. Tool: `Business.AIAgent/Tools/CostEstimate/GetFullCostEstimateTool.cs`

Nowe narzędzie dla agenta — zwraca pełny kosztorys z wszystkimi polami, grupami, pozycjami, field values i template schema.

Wzoruj się na `GetCostEstimateItemsTool.cs`:
- Dziedzicz po `AgentToolBase`
- DI: `ICostEstimateCacheService`, `IRepository<CostEstimateItemFieldValue>`, `IRepository<CostEstimateGroupFieldValue>`
- Name: `"get_full_cost_estimate"`
- Parameters: `cost_estimate_id` (required, GUID)
- Zwraca pełny JSON z:
  - CostEstimate metadata (name, description, status, totals)
  - Template name i field definitions
  - Groups z hierarchią (parent/child)
  - Group field values
  - Items z RelationType
  - Item field values
  - Template schema (field definitions z label, type, unit)

**Wzorzec implementacji:**
```csharp
public override async Task<ToolResult> ExecuteAsync(
    JsonElement arguments,
    AgentContext context,
    CancellationToken cancellationToken = default)
{
    Guid? costEstimateId = GetGuid(arguments, "cost_estimate_id");
    // load everything via cache service
    // build comprehensive JSON
    // return ToolResult.Success(json)
}
```

### 3. Rejestracja DI: `Business.AIAgent/Registration/AIAgentServiceExtensions.cs`

Dodać:
```csharp
services.AddScoped<IAgentTool, GetFullCostEstimateTool>();
```

## Weryfikacja

1. Plik `cost-estimate-editor.md` istnieje w `Resources/Agents/sub_agents/`
2. Plik `GetFullCostEstimateTool.cs` istnieje w `Tools/CostEstimate/`
3. `AIAgentServiceExtensions.cs` ma rejestrację `GetFullCostEstimateTool`
4. Build API przechodzi (`dotnet build --configuration Release`)
