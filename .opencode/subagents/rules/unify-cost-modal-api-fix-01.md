# API Fix-01 — Ujednolicenie ProjectCost API

## Cel

Ujednolicić kontrakt API dla `ProjectCost` tak, aby pola wspólne z `TrackedCost` (dziedziczone z `BaseCost`) miały identyczne nazwy i semantykę. Zakres jest minimalny — tylko to co blokuje wspólny UI.

## Kontekst z audytu

Raport audytu: `.opencode/subagents/rules/unify-cost-modal-api-audit.md`

`ProjectCost` dziedziczy po `BaseCost` który ma pola: `Name`, `Description`, `Net`, `Gross`, `Contractor`, `Date`, `Number`. Obecny kontrakt API dla ProjectCost:
- używa `NetAmount`/`GrossAmount` zamiast `Net`/`Gross`
- nie eksponuje `Contractor` i `Number` mimo że encja je posiada
- Create zwraca tylko `Guid` (TrackedCost zwraca pełny obiekt)
- Update zwraca 204 (TrackedCost zwraca 200 + obiekt)
- `Date` walidowana jako required (TrackedCost — opcjonalna)
- `Net`/`Gross` min `> 0` (TrackedCost — `>= 0`)
- `Name` max 200 (TrackedCost — 300, ujednolicamy do 200)

## Decyzje architektoniczne (zatwierdzone przez użytkownika)

- `Place` — USUNĄĆ z Commands i WebModel (kolumna w DB zostaje, wypadamy z kontraktu)
- `Contractor` — DODAĆ do Create/Update Commands i Response WebModel
- `Number` — DODAĆ (numer faktury) do Create/Update Commands i Response WebModel
- `NetAmount`/`GrossAmount` → przemianować na `Net`/`Gross` wszędzie
- `Date` → zmienić na opcjonalną (jak TrackedCost)
- `Net`/`Gross` min → zmienić na `>= 0` (jak TrackedCost)
- Create → zwracać pełny obiekt `ProjectCostWeb` zamiast `Guid`
- Update → zwracać 200 + `ProjectCostWeb` zamiast 204

## Pliki do zmodyfikowania

### 1. Web Model — `ProjectCostListItemWeb.cs`

Lokalizacja: `src/Business/Interfaces/WebModels/ProjectCosts/ProjectCostListItemWeb.cs`

Zmiany:
- Rename `NetAmount` → `Net`
- Rename `GrossAmount` → `Gross`
- Dodać `Contractor`
- Dodać `Number`
- Usunąć `Place` (jeśli istnieje)
- Rename klasy (opcjonalnie) lub zostaw nazwę — to tylko lista, ale jeśli jest osobny `ProjectCostWeb` tworzony przez handler Create/Update to użyj tego samego modelu

**Uwaga:** Jeśli `ProjectCostListItemWeb` jest jedynym modelem response (używanym zarówno dla listy jak i dla Create/Update), to zaktualizuj go. Jeśli potrzebny jest nowy `ProjectCostWeb` dla Create/Update response, utwórz go jako osobny record w tym samym katalogu z pełnymi polami.

### 2. CreateProjectCost Command

Lokalizacja: `src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommand.cs`

Zmiany:
- Rename `NetAmount` → `Net`
- Rename `GrossAmount` → `Gross`
- Dodać `Contractor`
- Dodać `Number`
- Usunąć `Place`

### 3. CreateProjectCost Validator

Lokalizacja: `src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommandValidator.cs`

Zmiany:
- Zaktualizować odwołania do `NetAmount`/`GrossAmount` → `Net`/`Gross`
- `Date` — zmienić na opcjonalną (usunąć `NotEmpty`)
- `Net`/`Gross` min — zmienić `GreaterThan(0)` → `GreaterThanOrEqualTo(0)`
- Dodać walidację `Contractor` (opcjonalny, max 300 znaków)
- Dodać walidację `Number` (opcjonalny, max 100 znaków)
- Usunąć walidację `Place`

### 4. CreateProjectCost Handler

Lokalizacja: `src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommandHandler.cs`

Zmiany:
- Przypisać `entity.Contractor = request.Contractor`
- Przypisać `entity.Number = request.Number`
- Usunąć przypisanie `entity.Place`
- Zmienić response na pełny obiekt `ProjectCostWeb` (lub `ProjectCostListItemWeb` jeśli unified)
- Zaktualizować mapowanie: `Net`/`Gross` zamiast `NetAmount`/`GrossAmount`

### 5. UpdateProjectCost Command

Lokalizacja: `src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommand.cs`

Zmiany:
- Rename `NetAmount` → `Net`
- Rename `GrossAmount` → `Gross`
- Dodać `Contractor`
- Dodać `Number`
- Usunąć `Place`

### 6. UpdateProjectCost Validator

Lokalizacja: `src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommandValidator.cs`

Zmiany:
- Analogiczne do Create Validator

### 7. UpdateProjectCost Handler

Lokalizacja: `src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommandHandler.cs`

Zmiany:
- Przypisać `entity.Contractor = request.Contractor`
- Przypisać `entity.Number = request.Number`
- Usunąć przypisanie `entity.Place`
- Zmienić response na 200 + pełny obiekt `ProjectCostWeb`
- Zaktualizować mapowanie

### 8. ProjectCostController

Lokalizacja: `src/WebApi/Controllers/ProjectCostController.cs`

Zmiany:
- Akcja Create: zmienić `return Created(...)` z `Guid` na `return CreatedAtAction(...)` z pełnym obiektem `ProjectCostWeb`
- Akcja Update: zmienić z `NoContent()` na `Ok(result)` z `ProjectCostWeb`
- Zaktualizować typy zwracane (XML docs, ProducesResponseType atrybuty jeśli są)

### 9. ProjectCostValidationExtensions (jeśli istnieje)

Lokalizacja: `src/CQRS/ProjectCosts/Shared/ProjectCostValidationExtensions.cs`

Zmiany:
- Zaktualizować wszystkie odwołania do pól zgodnie z powyższymi zmianami

### 10. ProjectCostHandlerBase (jeśli istnieje)

Lokalizacja: `src/CQRS/ProjectCosts/Shared/ProjectCostHandlerBase.cs`

Zmiany:
- Jeśli zawiera metodę mapowania, zaktualizować `NetAmount`/`GrossAmount` → `Net`/`Gross`
- Dodać `Contractor`, `Number` do mapowania
- Usunąć `Place` z mapowania

## Kryteria sukcesu

- `dotnet build src\WebApi\WebApi.csproj --nologo` → 0 błędów, 0 ostrzeżeń dotyczących tych zmian
- Swagger/OpenAPI: endpoint Create ProjectCost zwraca schema `ProjectCostWeb` nie `Guid`
- Swagger/OpenAPI: endpoint Update ProjectCost zwraca 200 + `ProjectCostWeb`
- Brak odwołań do `NetAmount`, `GrossAmount`, `Place` w plikach ProjectCost (sprawdź grep)

## Uwaga o testach

Jeśli istnieją testy jednostkowe dla ProjectCost Commands/Handlers/Validators — zaktualizuj je zgodnie ze zmianami pól.
