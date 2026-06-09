# API Audit — unify-cost-modal

**Data:** 2026-05-15  
**Agent:** api-audit-agent  
**Feature spec:** `.opencode/features/unify-cost-modal.md`

---

## BLOK 0 — Lista znalezionych plików

### TrackedCost

| Typ | Ścieżka |
|-----|---------|
| Encja | `src/Entities/Models/CostTrackers/TrackedCost.cs` |
| Encja bazowa | `src/Entities/Models/Costs/BaseCost.cs` |
| Command base | `src/CQRS/CostTrackers/Shared/TrackedCostCommandBase.cs` |
| Validator base | `src/CQRS/CostTrackers/Shared/TrackedCostCommandBaseValidator.cs` |
| Handler base | `src/CQRS/CostTrackers/Shared/TrackedCostMutationHandlerBase.cs` |
| Create Command | `src/CQRS/CostTrackers/CreateTrackedCost/CreateTrackedCostCommand.cs` |
| Create Validator | `src/CQRS/CostTrackers/CreateTrackedCost/CreateTrackedCostCommandValidator.cs` |
| Create Handler | `src/CQRS/CostTrackers/CreateTrackedCost/CreateTrackedCostCommandHandler.cs` |
| Update Command | `src/CQRS/CostTrackers/UpdateTrackedCost/UpdateTrackedCostCommand.cs` |
| Update Validator | `src/CQRS/CostTrackers/UpdateTrackedCost/UpdateTrackedCostCommandValidator.cs` |
| Update Handler | `src/CQRS/CostTrackers/UpdateTrackedCost/UpdateTrackedCostCommandHandler.cs` |
| Delete Command/Handler | `src/CQRS/CostTrackers/DeleteTrackedCost/DeleteTrackedCost*.cs` |
| Response Web Model | `src/Business/Interfaces/WebModels/CostTrackers/TrackedCostWeb.cs` |
| Controller | `src/WebApi/Controllers/CostTrackerController.cs` |

### ProjectCost

| Typ | Ścieżka |
|-----|---------|
| Encja | `src/Entities/Models/Costs/ProjectCost.cs` |
| Handler base | `src/CQRS/ProjectCosts/Shared/ProjectCostHandlerBase.cs` |
| Validation extensions | `src/CQRS/ProjectCosts/Shared/ProjectCostValidationExtensions.cs` |
| Create Command | `src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommand.cs` |
| Create Validator | `src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommandValidator.cs` |
| Create Handler | `src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommandHandler.cs` |
| Update Command | `src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommand.cs` |
| Update Validator | `src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommandValidator.cs` |
| Update Handler | `src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommandHandler.cs` |
| Delete Command/Handler | `src/CQRS/ProjectCosts/DeleteProjectCost/DeleteProjectCost*.cs` |
| Get Query/Handler | `src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCosts*.cs` |
| Response Web Model | `src/Business/Interfaces/WebModels/ProjectCosts/ProjectCostListItemWeb.cs` |
| Controller | `src/WebApi/Controllers/ProjectCostController.cs` |

---

## BLOK 1 — Stan obecny

### TrackedCost

- `BaseCost` (abstrakcyjna) definiuje pola: `TenantId`, `ProjectId`, `Name`, `Number`, `Description`, `Net`, `Gross`, `Contractor`, `Date`, `CostEstimateItemId`, `WorkScheduleStageWorkId`, `CreatedAt`, `UpdatedAt`.
- `TrackedCost` dziedziczy po `BaseCost` — jest klasą **pustą** (brak własnych pól).
- Command base `TrackedCostCommandBase` wiernie odwzorowuje pola `BaseCost`: `Name`, `Number`, `Description`, `Net`, `Gross`, `Contractor`, `Date`.
- Validator base `TrackedCostCommandBaseValidator<T>` waliduje wszystkie wspólne pola.
- Create i Update Commands dziedziczą po bazie i dodają pola specyficzne (`CostEstimateItemId`, `WorkScheduleStageWorkId` dla Create; `CostId`, `ExistingAttachmentIds`, `ClearAllAttachments` dla Update).
- Response `TrackedCostWeb` zawiera wszystkie pola z `BaseCost` + kontekst harmonogramu/kosztorysu.
- Endpointy: `POST /costs` (→ 201 + `TrackedCostWeb`), `PUT /costs/{costId}` (→ 200 + `TrackedCostWeb`), `DELETE /costs/{costId}` (→ 204). Brak GET po ID.

### ProjectCost

- `ProjectCost` dziedziczy po `BaseCost` i dodaje pola własne: `UserId`, `Place`, `IsAccepted`, `AcceptedByUserId`, `AcceptedAt`.
- Commands (`Create`/`Update`) definiują pola **od nowa**, bez dziedziczenia z bazy — są izolowane.
- Command nie zawiera pól `Contractor`, `Number` z `BaseCost`, mimo że encja je ma.
- Pola finansowe są nazwane `NetAmount`/`GrossAmount` zamiast `Net`/`Gross`.
- Response `ProjectCostListItemWeb` — uproszczony model listy — odwzorowuje `Net` encji jako `NetAmount`.
- Endpointy: `GET /{scope}` (→ 200 + lista), `POST /` (→ 201 + `{id: Guid}`), `PUT /{costId}` (→ 204), `DELETE /{costId}` (→ 204). Brak GET po ID.

---

## BLOK 2 — Tabela porównawcza pól wspólnych (z BaseCost)

| Pole w `BaseCost` | TrackedCost Command | TrackedCost Response | ProjectCost Command | ProjectCost Response | Spójność |
|---|---|---|---|---|---|
| `Name` | `Name` | `Name` | `Name` | `Name` | ✅ |
| `Description` | `Description` | `Description` | `Description` | `Description` | ✅ |
| `Net` | `Net` | `Net` | `NetAmount` | `NetAmount` | ❌ Różna nazwa |
| `Gross` | `Gross` | `Gross` | `GrossAmount` | `GrossAmount` | ❌ Różna nazwa |
| `Contractor` | `Contractor` | `Contractor` | ❌ brak | ❌ brak | ❌ Pole pominięte |
| `Date` | `Date` | `Date` | `Date` | `Date` | ✅ |
| `Number` | `Number` | `Number` | ❌ brak | ❌ brak | ❌ Pole pominięte |

---

## BLOK 3 — Tabela porównawcza walidacji wspólnych pól

| Reguła | TrackedCost (`TrackedCostCommandBaseValidator`) | ProjectCost (`ProjectCostValidationExtensions`) | Spójność |
|--------|------------------------------------------------|------------------------------------------------|----------|
| `Name` — wymagane | `NotEmpty` ✅ | `NotEmpty` ✅ | ✅ |
| `Name` — max długość | 300 znaków | 200 znaków | ❌ Rozbieżność |
| `Net`/`Gross` — min wartość | `>= 0` (`GreaterThanOrEqualTo`) | `> 0` (`GreaterThan`) | ❌ Rozbieżność |
| `Net`/`Gross` — przynajmniej jedno wymagane | Tylko w Create (nie w Update) | W obu Create i Update | ❌ Rozbieżność |
| `Date` — wymagana | ❌ Nie (pole opcjonalne) | ✅ Tak (`NotEmpty`) | ❌ Rozbieżność |
| `Description` — max długość | 2000 ✅ | 2000 ✅ | ✅ |
| `Contractor` — max długość | 300 | ❌ brak pola | n/a |

---

## BLOK 4 — Typy odpowiedzi i endpointy

### Porównanie response typów

| Operacja | TrackedCost | HTTP | ProjectCost | HTTP | Spójność |
|----------|------------|------|-------------|------|----------|
| Create | `TrackedCostWeb` (pełny obiekt) | 201 | `Guid` (samo ID) | 201 | ❌ |
| Update | `TrackedCostWeb` (pełny obiekt) | 200 | `NoContent` | 204 | ❌ |
| Delete | `NoContent` | 204 | `NoContent` | 204 | ✅ |
| List GET | ❌ brak dedykowanego | — | `IEnumerable<ProjectCostListItemWeb>` | 200 | n/a |
| GET by ID | ❌ brak | — | ❌ brak | — | ✅ (oba brak) |

### Porównanie endpointów

| Endpoint | TrackedCost | ProjectCost |
|----------|------------|-------------|
| Base route | `/api/tenants/{tenantId}/projects/{projectId}/cost-trackers` | `/api/tenants/{tenantId}/projects/{projectId}/cost` |
| Create | `POST /costs` | `POST /` |
| Update | `PUT /costs/{costId}` | `PUT /{costId}` |
| Delete | `DELETE /costs/{costId}` | `DELETE /{costId}` |
| List GET | ❌ brak | `GET /{scope}` |
| Permission (Create/Update) | `ProjectEdit` | `ProjectResourcesWrite` |

---

## BLOK 5 — Niezgodności blokujące integrację UI

| # | Niezgodność | Warstwa | Blokuje |
|---|-------------|---------|---------|
| 1 | Pola finansowe: `Net`/`Gross` (TrackedCost) vs `NetAmount`/`GrossAmount` (ProjectCost) — w Commands i Response | Command + WebModel | UI nie może użyć jednego interfejsu TypeScript dla obu typów żądania |
| 2 | `Contractor` brak w ProjectCost Commands i Response — encja posiada to pole w `BaseCost` | Command + WebModel | UI nie może wyświetlić/edytować `Contractor` dla ProjectCost bez zmiany API |
| 3 | Create ProjectCost zwraca `Guid`, Create TrackedCost zwraca `TrackedCostWeb` | Controller | Dwa różne flow po zapisie: TrackedCost aktualizuje store od razu, ProjectCost musi pobierać dane osobno |
| 4 | Update TrackedCost → 200 + obiekt; Update ProjectCost → 204 + nic | Controller | Hook mutacji nie może mieć wspólnego kształtu odpowiedzi |
| 5 | `Date` opcjonalna w TrackedCost, wymagana w ProjectCost | Validator | Wspólna walidacja formularza musi rozgałęziać się per typ — lub UI toleruje dwie różne strategie walidacji |
| 6 | `Name` max length: 300 vs 200 | Validator | Mniejszy problem, ale wspólna reguła formularza powinna używać 200 (bezpieczna dolna granica), albo API trzeba ujednolicić |
| 7 | `Net`/`Gross` min value: `>= 0` vs `> 0` | Validator | Formularz UI akceptujący 0 dla TrackedCost zostanie odrzucony przez ProjectCost endpoint |

---

## BLOK 6 — Pola specyficzne (nie wymagają ujednolicenia)

Poniższe pola są celowo różne — nie powinny być traktowane jako niezgodności:

| Pole | Gdzie | Dlaczego specyficzne |
|------|-------|---------------------|
| `Place` | ProjectCost only | Fizyczna lokalizacja wydatku — domenowo specyficzne |
| `IsAccepted` | ProjectCost only | Workflow akceptacji przez admina projektu |
| `Document` (single `IFormFile`) | ProjectCost only | Jeden dokument księgowy per wydatek |
| `NewFiles` (lista `IFormFile`) | TrackedCost only | Wiele załączników per koszt trackera |
| `ExistingAttachmentIds` | TrackedCost only | Zarządzanie zestawem załączników |
| `CostEstimateItemId` | TrackedCost only | Powiązanie z pozycją kosztorysu |
| `WorkScheduleStageWorkId` | TrackedCost only | Powiązanie z pozycją harmonogramu |
| `UserId` / `UserName` | ProjectCost response only | Koszt należy do konkretnego członka projektu |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Znalezione niezgodności blokujące UI | **7** |
| Rozbieżności w nazwach pól (Command + Response) | **2** (Net/Gross → NetAmount/GrossAmount) |
| Brakujące pola w ProjectCost | **2** (Contractor, Number) |
| Różnice w response typach | **2** (Create: Guid vs obiekt; Update: 204 vs 200 + obiekt) |
| Różnice w walidacji | **4** (Date wymagana, min wartość, max Name, Net/Gross w Update) |
| Wymaga migracji DB | **nie** |
| Wymaga zmian w kontrolerach | **tak** (Create + Update ProjectCost) |
| Wymaga zmian w Commands | **tak** (CreateProjectCost, UpdateProjectCost) |
| Wymaga zmian w Web Models | **tak** (ProjectCostListItemWeb) |
| Wymaga zmian w Validators | **tak** (ProjectCostValidationExtensions) |

---

## REKOMENDACJA

### Zmiany minimalne (umożliwiające współdzielenie logiki UI)

Wszystkie zmiany dotyczą wyłącznie warstwy **ProjectCost**. TrackedCost jest wzorcem — nie wymaga modyfikacji.

#### 1. Przemianować pola finansowe w ProjectCost

W `CreateProjectCostCommand`, `UpdateProjectCostCommand` i `ProjectCostListItemWeb`:
- `NetAmount` → `Net`
- `GrossAmount` → `Gross`

Zaktualizować odpowiednio handlery (`CreateProjectCostCommandHandler`, `UpdateProjectCostCommandHandler`, `GetProjectCostsQueryHandler`) i walidatory.

#### 2. Dodać pole `Contractor` do ProjectCost

W `CreateProjectCostCommand`, `UpdateProjectCostCommand`:
```csharp
public string? Contractor { get; init; }
```

W `ProjectCostListItemWeb`:
```csharp
public string? Contractor { get; init; }
```

W handlerach: mapować `request.Contractor → projectCost.Contractor` i `pc.Contractor → web.Contractor`.

W `ProjectCostValidationExtensions` dodać regułę:
```csharp
// max 300 znaków — spójnie z TrackedCostCommandBaseValidator
```

#### 3. Zmienić response Create ProjectCost z `Guid` na `ProjectCostListItemWeb`

- `CreateProjectCostCommandHandler.Handle()` → zwracać `ProjectCostListItemWeb` zamiast `Guid`
- `ProjectCostController.CreateProjectCost()` → `return Created(string.Empty, result)`

#### 4. Zmienić response Update ProjectCost z 204 na 200 + `ProjectCostListItemWeb`

- `UpdateProjectCostCommandHandler.Handle()` → zwracać `ProjectCostListItemWeb`
- `ProjectCostController.UpdateProjectCost()` → `return Ok(result)`

#### 5. Ujednolicić walidację

W `ProjectCostValidationExtensions`:
- `Name` max → zmienić z 200 na 300
- `Net`/`Gross` min → zmienić z `GreaterThan(0)` na `GreaterThanOrEqualTo(0)`
- Decyzja o `Date` → patrz pytania domenowe poniżej

### Zmiany nieobowiązkowe (pominięte w minimalnym zakresie)

- Dodanie `Number` do ProjectCost — encja ma to pole, ale nie jest używane przez UI ani biznesowo (brak kontekstu numeracji dla wydatków członków)
- Ujednolicenie formatu listy TrackedCost (brak GET list — koszty wchodzą jako część dashboardu trackera)

---

## Pytania domenowe wymagające decyzji

1. **Czy pole `Date` dla ProjectCost powinno być opcjonalne** (jak w TrackedCost) czy wymagane (jak jest teraz)? Jeśli modal ma wspólne pole `Date`, to czy można zapisać wydatek bez daty?

2. **Czy pole `Number` (`string?`) powinno być dodane do ProjectCost** — czy wydatki projektowe mają numerację (np. numer faktury)? Encja posiada to pole przez `BaseCost`, ale Commands je pomijają.

3. **Czy zmiana response Create/Update ProjectCost z `Guid`/`204` na pełny obiekt** jest akceptowalna — czy istnieją klienci (inne serwisy, integracje) którzy polegają na obecnym kształcie odpowiedzi `POST /cost`?

---

*Koniec raportu. Brak zmian w kodzie — raport tylko.*
