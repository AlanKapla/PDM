# Feature: AI Cost Estimate Edit — edycja kosztorysu przez AI

## Cel

Użytkownik ma istniejący kosztorys (zapisany w DB) i chce go zmodyfikować przy pomocy AI.
Zamiast ręcznie dodawać/usuwać/edytować pozycje, user wpisuje naturalnym językiem co chce zmienić,
AI analizuje obecny stan kosztorysu i proponuje zmiany, a user zatwierdza je przed zapisem.

## Przepływ (UX)

```
1. User jest na stronie edycji kosztorysu (CostEstimateEditPage)
2. Klikka "Edytuj z AI" w toolbarze
3. Otwiera się modal AIEditCostEstimateModal:
   a. Krok 1 — pole tekstowe: "Co chcesz zmienić?" (np. "dodaj 3 pozycje do grupy Fundamenty",
      "zaktualizuj ceny do 2026", "dodaj grupę wykończeniową pod klucz", "usuń grupę Garaż")
   b. Krok 2 — loading "AI analizuje kosztorys..."
   c. Krok 3 — podgląd proponowanych zmian (diff)
   d. Krok 4 — przyciski "Zatwierdź zmiany" / "Anuluj"
4. User zatwierdza → zmiany są aplikowane do kosztorysu
5. Kosztorys jest przeładowany z nowymi danymi
```

## Zakres

### API — Business.AIAgent

- Nowy agent `cost-estimate-editor.md` w `Resources/Agents/sub_agents/`
- Agent otrzymuje: pełny stan kosztorysu (grupy, pozycje, wartości pól) + request użytkownika
- Agent zwraca JSON z proponowanymi zmianami (diff structure)
- Nowy tool `GetFullCostEstimateTool` — zwraca pełny kosztorys z wszystkimi polami,

### API — Business

- Nowy interfejs `ICostEstimateAIEditService` (lub rozszerzenie `ICostEstimateAIGeneratorService`)
- Implementacja `CostEstimateAIEditService`:
  - Ładuje kosztorys z DB (z template, grupami, pozycjami, field values)
  - Buduje kontekst dla AI (aktualny stan kosztorysu w formacie JSON)
  - Wywołuje agenta `cost-estimate-editor` z kontekstem + requestem użytkownika
  - Parsuje odpowiedź AI na strukturę `AICostEditPreviewWeb`
  - Waliduje proponowane zmiany względem template

### API — CQRS

- `GenerateCostEstimateAIEditCommand` → `AICostEditPreviewWeb`
  - Przyjmuje: `CostEstimateId`, `UserRequest` (string)
  - Ładuje kosztorys z DB
  - Wywołuje AI z pełnym kontekstem
  - Zwraca propozycję zmian (NIE zapisuje do DB)
  
- `ApplyCostEstimateAIEditCommand` → `Unit`
  - Przyjmuje: `CostEstimateId`, `AICostEditPreviewWeb` (zatwierdzony przez usera)
  - Aplikuje zmiany do DB przez istniejące CQRS:
    - `UpdateCostEstimateCommand` — zmiana name/description
    - `AddCostEstimateGroupCommand` — nowe grupy
    - `DeleteCostEstimateGroupCommand` — usunięte grupy
    - `AddCostEstimateItemCommand` — nowe pozycje
    - `DeleteCostEstimateItemCommand` — usunięte pozycje
    - `UpsertCostEstimateItemFieldCommand` — zmienione wartości pól
    - `ReorderCostEstimateItemsCommand` — zmiana kolejności
    - `RecalculateCostEstimateCommand` — recalculation na końcu

### API — WebApi

- Nowe endpointy w `CostEstimateController`:
  - `POST /{id}/ai/edit-preview` — generuj propozycję edycji
  - `POST /{id}/ai/apply-edit` — zatwierdź i aplikuj zmiany

### UI — Typy

- `AICostEditRequestDto` — { costEstimateId, userRequest }
- `AICostEditPreviewDto` — struktura proponowanych zmian
- `AICostEditActionDto` — pojedyncza operacja (add/update/delete group/item/field)
- `AICostEditDiffDto` — podsumowanie zmian (co się zmieniło)

### UI — API Client

- Nowe metody w `costEstimateApi.ts`:
  - `generateAIEditPreview(tenantId, projectId, costEstimateId, userRequest)` 
  - `applyAIEdit(tenantId, projectId, costEstimateId, preview)`

### UI — Hook

- `useAICostEstimateEdit` — hook zarządzający stanem edycji AI
  - `generateEditPreview` — mutation do generowania propozycji
  - `applyEdit` — mutation do aplikowania zmian
  - Stan: `isGenerating`, `isApplying`, `preview`, `error`

### UI — Komponent

- `AIEditCostEstimateModal` — modal z procesem: input → loading → podgląd → zatwierdzenie
- Przycisk "Edytuj z AI" w `CostEstimateToolbar` (ikona `Bot`/`Zap`)
- Integracja w `CostEstimateEditPage`

## Specyfikacja AICostEditPreviewWeb

```csharp
public sealed record AICostEditPreviewWeb
{
    /// Podsumowanie zmian (dla UI)
    public string Summary { get; init; } = string.Empty;
    
    /// Liczba grup do dodania
    public int GroupsToAdd { get; init; }
    
    /// Liczba grup do usunięcia
    public int GroupsToDelete { get; init; }
    
    /// Liczba pozycji do dodania
    public int ItemsToAdd { get; init; }
    
    /// Liczba pozycji do usunięcia
    public int ItemsToDelete { get; init; }
    
    /// Liczba pól do zmiany
    public int FieldsToUpdate { get; init; }
    
    /// Proponowana nowa nazwa kosztorysu (lub null jeśli bez zmian)
    public string? SuggestedName { get; init; }
    
    /// Proponowany nowy opis (lub null jeśli bez zmian)
    public string? SuggestedDescription { get; init; }
    
    /// Lista grup w finalnym stanie (pełny stan po edycji)
    public List<AIGroupPreviewWeb> Groups { get; init; } = [];
    
    /// Ostrzeżenia
    public List<string> Warnings { get; init; } = [];
}
```

## Struktura agenta cost-estimate-editor

Agent otrzymuje:
1. **Aktualny stan kosztorysu** — pełny JSON z grupami, pozycjami, field values, template schema
2. **Request użytkownika** — naturalny język co chce zmienić
3. **Template schema** — dostępne pola, jednostki, ograniczenia

Agent zwraca JSON:
```json
{
  "summary": "Dodano 3 pozycje do grupy Fundamenty, zaktualizowano ceny",
  "suggestedName": null,
  "suggestedDescription": null,
  "groups": [
    {
      // Pełna lista grup (istniejące + nowe, bez usuniętych)
      // Istniejące grupy mają swoje rzeczywiste ID (nie tempId)
      // Nowe grupy mają tempId (np. "new_g1")
    }
  ]
}
```

## Agent tools

Nowy tool `get_full_cost_estimate` — zwraca pełny kosztorys z wszystkimi polami, grupami, pozycjami, komponentami i template schema. Używany przez `cost-estimate-editor`.

## Zasady

1. **Preview → Approve** — AI nigdy nie zapisuje bezpośrednio do DB. Najpierw preview, potem user zatwierdza.
2. **Idempotentność** — Apply może być wywołane wielokrotnie (jeśli pierwsze apply się udało, drugie nie powinno nic zmienić)
3. **Zachowanie istniejących ID** — Istniejące grupy/pozycje zachowują swoje GUID. Nowe są tworzone przez CQRS z nowymi GUID.
4. **Walidacja względem template** — Tak samo jak przy creation, field values są walidowane względem template field definitions.
5. **Bezpieczeństwo** — Sprawdza `CostEstimateAccessLevel.Full` (owner/admin) lub odpowiednie permission.
6. **Recalculate** — Po apply automatycznie wywołuje RecalculateCostEstimate.

## PermissionCode

- `PermissionCodes.ProjectEstimates` ( `PROJECT.ESTIMATES.WRITE_OWN` ) — edycja własnych kosztorysów
- `PROJECT.ESTIMATES.WRITE_SHARED` — edycja udostępnionych

## Zależności

- Istniejące CQRS: wszystkie update commandy (AddGroup, DeleteGroup, AddItem, DeleteItem, UpsertField itd.)
- Istniejący `CostEstimateAIGeneratorService` — wzorzec do naśladowania
- Istniejący agent `cost-estimate-planner` / `cost-estimate-group-generator` — agent definitions do naśladowania
- Istniejący `CostEstimateCalculationService` — do recalculate po zmianach
- Istniejący `CostEstimateEditPage` i `CostEstimateToolbar` — do integracji UI
