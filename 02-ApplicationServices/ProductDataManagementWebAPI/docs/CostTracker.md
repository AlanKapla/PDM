# Cost Tracker – dokumentacja modułu

## Spis treści

- [Przegląd](#przegląd)
- [Architektura](#architektura)
- [Endpointy HTTP](#endpointy-http)
  - [GET by-project](#get-by-project)
  - [POST – CreateTrackedCost](#post--createtrackedcost)
  - [PUT – UpdateTrackedCost](#put--updatetrackedcost)
  - [PUT – UpdateTrackerBudget](#put--updatetrackerbudget)
  - [DELETE – DeleteTrackedCost](#delete--deletetrackedcost)
- [Queries](#queries)
  - [GetCostTrackerByProjectQuery](#getcosttrackerbyprojectquery)
  - [GetCostTrackerByEstimateQuery](#getcosttrackerbyestimatequery)
  - [GetTrackedCostsByItemQuery](#gettrackedcostsbyitemquery)
  - [GetTrackedCostListQuery](#gettrackedcostlistquery)
  - [GetTrackedCostDetailsQuery](#gettrackedcostdetailsquery)
- [Commands](#commands)
  - [CreateTrackedCostCommand](#createtrackedcostcommand)
  - [UpdateTrackedCostCommand](#updatetrackedcostcommand)
  - [UpdateTrackerBudgetCommand](#updatetrackerbudgetcommand)
  - [DeleteTrackedCostCommand](#deletetrackedcostcommand)
- [Web Models](#web-models)
  - [CostTrackerDetailsWeb](#costtrackerdetailsweb)
  - [CostTrackerSummaryBaseWeb](#costtrackersummarybaseweb)
  - [CostTrackerSummaryWeb](#costtrackersummaryweb)
  - [CostTrackerBudgetSummary](#costtrackerbudgetsummary)
  - [CostEstimateSummaryWeb](#costestimatesummaryweb)
  - [ProjectAdditionalCostsWeb](#projectadditionalcostsweb)
  - [TrackerAdditionalCostsWeb](#trackeradditionalcostsweb)
  - [TrackedCostWeb](#trackedcostweb)
  - [TrackedCostAttachmentWeb](#trackedcostattachmentweb)
  - [TrackerNodeWeb](#trackernodeweb)
  - [TrackerGroupWeb](#trackergroupweb)
  - [TrackerItemWeb](#trackeritemweb)
  - [TrackedCostItemStatus](#trackedcostitemstatus)
- [Walidacja](#walidacja)
- [Bezpieczeństwo i dostęp](#bezpieczeństwo-i-dostęp)
- [Integracja z widokiem kosztorysu](#integracja-z-widokiem-kosztorysu)
  - [CostEstimateItemCostsWeb](#costestimateitemcostsweb)

---

## Przegląd

Moduł **Cost Tracker** umożliwia śledzenie rzeczywistych kosztów powiązanych z projektami i kosztorysami. Każdy projekt posiada dokładnie jeden tracker kosztów (`CostTracker`), do którego przypisywane są pozycje kosztów (`TrackedCost`). Koszt może być:

- **powiązany z kosztorysem i pozycją kosztorysu** (`CostEstimateId` + `CostEstimateItemId`) — pozwala śledzić wykonanie budżetu,
- **powiązany tylko z kosztorysem** (`CostEstimateId` bez `CostEstimateItemId`) — koszt dodatkowy w ramach kosztorysu,
- **projektu** (bez żadnego powiązania z kosztorysem) — `IsAdditional = true`, koszt dodatkowy na poziomie projektu.

---

## Architektura

```
CostTrackerController
│
├── GetCostTrackerByProjectQuery   →  GetCostTrackerByProjectQueryHandler
├── CreateTrackedCostCommand       →  CreateTrackedCostCommandHandler
├── UpdateTrackedCostCommand       →  UpdateTrackedCostCommandHandler
├── UpdateTrackerBudgetCommand     →  UpdateTrackerBudgetCommandHandler
└── DeleteTrackedCostCommand       →  DeleteTrackedCostCommandHandler

Handlery bez endpointu HTTP (niezarejestrowane w kontrolerze):
  GetCostTrackerByEstimateQuery  →  GetCostTrackerByEstimateQueryHandler
  GetTrackedCostsByItemQuery     →  GetTrackedCostsByItemQueryHandler
  GetTrackedCostListQuery        →  GetTrackedCostListQueryHandler
  GetTrackedCostDetailsQuery     →  GetTrackedCostDetailsQueryHandler

Handlery dziedziczą z:
  TrackedCostMutationHandlerBase  (Create, Update)
    └─ CostTrackerHandlerBase     (wszystkie handlery)
```

Wspólna logika wydzielona do klas bazowych:
| Klasa bazowa | Odpowiedzialność |
|---|---|
| `CostTrackerHandlerBase` | Ładowanie i walidacja trackera, projektu; weryfikacja dostępu; `MapTrackedCostToWeb`; `BuildEstimateSummary(costEstimate, itemsDict, costsByItemId, additionalCostsList, groups, additionalCostWebs)`; `BuildTrackerGroups`; `BuildTrackerGroupHierarchy`; `BuildTrackerGroupWeb`; `BuildTrackerItemWeb`; `ResolveGroupName`; `ResolveItemName` |
| `TrackedCostMutationHandlerBase` | Walidacja kosztorysu i pozycji kosztorysu; `BuildCostWeb` |

---

## Endpointy HTTP

Bazowa ścieżka: `api/tenants/{tenantId}/projects/{projectId}/cost-trackers`

### GET by-project

```
GET /api/tenants/{tenantId}/projects/{projectId}/cost-trackers/by-project
```

Zwraca pełne dane trackera dla projektu — wszystkie kosztorysy zagregowane łącznie z kosztami dodatkowymi projektu.

**Autoryzacja:** `ProjectResourcesReadSingle`

**Parametry ścieżki:**

| Parametr | Typ | Opis |
|---|---|---|
| `tenantId` | `Guid` | Identyfikator tenanta |
| `projectId` | `Guid` | Identyfikator projektu |

**Odpowiedzi:**

| Status | Opis |
|---|---|
| `200 OK` | [`CostTrackerDetailsWeb`](#costtrackerdetailsweb) |
| `403 Forbidden` | Brak dostępu do projektu lub trackera |
| `404 Not Found` | Tracker lub projekt nie istnieje |

---

### POST – CreateTrackedCost

```
POST /api/tenants/{tenantId}/projects/{projectId}/cost-trackers/costs
Content-Type: multipart/form-data
```

Tworzy nowy koszt śledzony w trackerze projektu. Opcjonalnie przyjmuje załączniki plików.

**Autoryzacja:** `ProjectResourcesWrite`

**Limity:** `52 MB` (request + multipart body)

**Parametry ścieżki:**

| Parametr | Typ | Opis |
|---|---|---|
| `tenantId` | `Guid` | Identyfikator tenanta |
| `projectId` | `Guid` | Identyfikator projektu |

**Body (`multipart/form-data`):** → [`CreateTrackedCostCommand`](#createtrackedcostcommand)

**Odpowiedzi:**

| Status | Opis |
|---|---|
| `200 OK` | [`TrackedCostWeb`](#trackedcostweb) — nowo utworzony koszt |
| `400 Bad Request` | Błąd walidacji |
| `403 Forbidden` | Brak dostępu |
| `404 Not Found` | Tracker, kosztorys lub pozycja kosztorysu nie istnieje |

---

### PUT – UpdateTrackedCost

```
PUT /api/tenants/{tenantId}/projects/{projectId}/cost-trackers/costs/{costId}
Content-Type: multipart/form-data
```

Pełne nadpisanie istniejącego kosztu śledzonego. Zarządza listą załączników (nowe pliki + zachowane istniejące).

**Autoryzacja:** `ProjectResourcesWrite`

**Limity:** `52 MB`

**Parametry ścieżki:**

| Parametr | Typ | Opis |
|---|---|---|
| `tenantId` | `Guid` | Identyfikator tenanta |
| `projectId` | `Guid` | Identyfikator projektu |
| `costId` | `Guid` | Identyfikator kosztu do aktualizacji |

**Body (`multipart/form-data`):** → [`UpdateTrackedCostCommand`](#updatetrackedcostcommand)

**Odpowiedzi:**

| Status | Opis |
|---|---|
| `200 OK` | [`TrackedCostWeb`](#trackedcostweb) — zaktualizowany koszt |
| `400 Bad Request` | Błąd walidacji |
| `403 Forbidden` | Brak dostępu |
| `404 Not Found` | Koszt, tracker, kosztorys lub pozycja nie istnieje |

---

### PUT – UpdateTrackerBudget

```
PUT /api/tenants/{tenantId}/projects/{projectId}/cost-trackers/{costTrackerId}/budget
Content-Type: application/json
```

Aktualizuje pola budżetowe (`BudgetNet`, `BudgetGross`) bezpośrednio na encji `CostTracker`. Partial update — pozostałe dane trackera nie są modyfikowane. Oba pola edytowalne niezależnie i addytywne względem budżetów z kosztorysów.

**Autoryzacja:** `ProjectResourcesWrite`

**Parametry ścieżki:**

| Parametr | Typ | Opis |
|---|---|---|
| `tenantId` | `Guid` | Identyfikator tenanta |
| `projectId` | `Guid` | Identyfikator projektu |
| `costTrackerId` | `Guid` | Identyfikator trackera kosztów |

**Body (`application/json`):** → [`UpdateTrackerBudgetCommand`](#updatetrackerbudgetcommand)

**Odpowiedzi:**

| Status | Opis |
|---|---|
| `204 No Content` | Budżet zaktualizowany |
| `400 Bad Request` | Błąd walidacji (np. wartość ujemna) |
| `403 Forbidden` | Brak dostępu |
| `404 Not Found` | Tracker nie istnieje lub nie należy do podanego projektu/tenanta |

---

### DELETE – DeleteTrackedCost

```
DELETE /api/tenants/{tenantId}/projects/{projectId}/cost-trackers/costs/{costId}
```

Usuwa koszt śledzony (soft-delete). Usuwa też wszystkie powiązane załączniki z blobstorage.

**Autoryzacja:** `ProjectResourcesWrite`

**Parametry ścieżki:**

| Parametr | Typ | Opis |
|---|---|---|
| `tenantId` | `Guid` | Identyfikator tenanta |
| `projectId` | `Guid` | Identyfikator projektu |
| `costId` | `Guid` | Identyfikator kosztu do usunięcia |

**Odpowiedzi:**

| Status | Opis |
|---|---|
| `204 No Content` | Koszt usunięty |
| `403 Forbidden` | Brak dostępu |
| `404 Not Found` | Koszt lub tracker nie istnieje |

---

## Queries

### GetCostTrackerByProjectQuery

```csharp
public sealed record GetCostTrackerByProjectQuery() : IRequestQuery<CostTrackerDetailsWeb>, IAuthorizableRequest
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public string PermissionCode => PermissionCodes.ProjectResourcesReadSingle;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
```

**Pola:**

| Pole | Typ | Źródło | Opis |
|---|---|---|---|
| `ProjectId` | `Guid` | route | Identyfikator projektu |
| `TenantId` | `Guid` | route | Identyfikator tenanta (ustawiany przez kontroler) |

**Permission:** `ProjectResourcesReadSingle`

**Przepływ handlera:**
1. Zweryfikuj dostęp użytkownika (`ValidateAccessAsync`)
2. Pobierz wszystkie `TrackedCost` po `tenantId` + `projectId` (przez nawigację `tc.Tracker`)
3. Pobierz załączniki do wszystkich kosztów jednym zapytaniem — zbuduj `ILookup<Guid, TrackedCostAttachment>`
4. Wydziel koszty projektu (brak `CostEstimateId`) → `ProjectAdditionalCostsWeb`
5. Pobierz wszystkie `CostEstimate` dla projektu z repozytorium
6. Dla każdego kosztorysu pobierz wszystkie 4 słowniki z cache (`GetGroupsDictionaryAsync`, `GetGroupFieldValuesDictionaryAsync`, `GetItemsDictionaryAsync`, `GetItemFieldValuesDictionaryAsync`), zbuduj pełną hierarchię grup (`BuildTrackerGroups`) oraz zmapuj koszty dodatkowe kosztorysu przez `MapTrackedCostToWeb`, po czym wywołaj `BuildEstimateSummary`
7. Załaduj pełną encję `CostTracker` (`LoadTrackerEntityAsync`) — niezbędna do odczytu `BudgetNet`/`BudgetGross`
8. Oblicz project-level summary (`ICostTrackerFinancialService.ComputeProjectSummary`) przekazując `tracker.BudgetNet` i `tracker.BudgetGross` jako parametry addytywne do budżetu z kosztorysów
9. Oblicz budget summary wyłącznie na podstawie kosztów dodatkowych projektu (`ICostTrackerFinancialService.ComputeBudgetSummary`)
10. Zwróć [`CostTrackerDetailsWeb`](#costtrackerdetailsweb) z wypełnionym `BudgetSummary`

---

### GetCostTrackerByEstimateQuery

```csharp
public sealed record GetCostTrackerByEstimateQuery : IRequestQuery<CostEstimateSummaryWeb>, IAuthorizableRequest
{
    public required Guid CostEstimateId { get; init; }
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public string PermissionCode => PermissionCodes.ProjectResourcesReadSingle;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
```

**Pola:**

| Pole | Typ | Źródło | Opis |
|---|---|---|---|
| `CostEstimateId` | `Guid` | route | Identyfikator kosztorysu |
| `TenantId` | `Guid` | route | Identyfikator tenanta (ustawiany przez kontroler) |
| `ProjectId` | `Guid` | route | Identyfikator projektu (ustawiany przez kontroler) |

**Permission:** `ProjectResourcesReadSingle`

**Przepływ handlera:**
1. Zweryfikuj dostęp użytkownika (`ValidateAccessAsync`)
2. Pobierz i zwaliduj kosztorys z cache (`ICostEstimateCacheService.GetCostEstimateAsync`)
3. Pobierz słowniki z cache: grupy, field values grup, pozycje, field values pozycji
4. Pobierz `TrackedCost` powiązane z tym kosztorysem
5. Podziel koszty na: powiązane z pozycjami (`ILookup<Guid, TrackedCost>` po `CostEstimateItemId`) i dodatkowe (brak `CostEstimateItemId`)
6. Zbuduj hierarchię grup (`BuildTrackerGroups`) — rekurencyjne drzewo `TrackerGroupWeb` z `ChildGroups` i `Items`
7. Pobierz załączniki dla kosztów dodatkowych i zbuduj `List<TrackedCostWeb>`
8. Wywołaj `BuildEstimateSummary(costEstimate, itemsDict, costsByItemId, additionalCostsList, groups, additionalCostWebs)` — metoda bazowa oblicza summary i wewnętrznie przypisuje `Groups` oraz `AdditionalCosts`
9. Zwróć [`CostEstimateSummaryWeb`](#costestimatesummaryweb)

---

### GetTrackedCostsByItemQuery

```csharp
public sealed record GetTrackedCostsByItemQuery : IRequestQuery<List<TrackedCostWeb>>, IAuthorizableRequest
{
    public required Guid CostEstimateId { get; init; }
    public required Guid CostEstimateItemId { get; init; }
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public string PermissionCode => PermissionCodes.ProjectResourcesReadSingle;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
```

**Pola:**

| Pole | Typ | Źródło | Opis |
|---|---|---|---|
| `CostEstimateId` | `Guid` | route | Identyfikator kosztorysu |
| `CostEstimateItemId` | `Guid` | route | Identyfikator pozycji kosztorysu |
| `TenantId` | `Guid` | route | Identyfikator tenanta (ustawiany przez kontroler) |
| `ProjectId` | `Guid` | route | Identyfikator projektu (ustawiany przez kontroler) |

**Permission:** `ProjectResourcesReadSingle`

**Przepływ handlera:**
1. Zweryfikuj dostęp użytkownika (`ValidateAccessAsync`)
2. Pobierz `TrackedCost` dla danego `CostEstimateItemId` + tenant/project isolation
3. Pobierz załączniki dla tych kosztów
4. Zmapuj do `List<TrackedCostWeb>`

---

### GetTrackedCostListQuery

```csharp
public sealed record GetTrackedCostListQuery : IRequestQuery<List<TrackedCostWeb>>, IAuthorizableRequest
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public string PermissionCode => PermissionCodes.ProjectResourcesReadSingle;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
```

**Pola:**

| Pole | Typ | Źródło | Opis |
|---|---|---|---|
| `TenantId` | `Guid` | route | Identyfikator tenanta |
| `ProjectId` | `Guid` | route | Identyfikator projektu |

**Permission:** `ProjectResourcesReadSingle`

**Przepływ handlera:**
1. Zweryfikuj dostęp użytkownika (`ValidateAccessAsync`)
2. Pobierz wszystkie `TrackedCost` po `tenantId` + `projectId`, posortowane malejąco po `CreatedAt`
3. Załaduj załączniki jednym zapytaniem
4. Zmapuj do `List<TrackedCostWeb>`

---

### GetTrackedCostDetailsQuery

```csharp
public sealed record GetTrackedCostDetailsQuery : IRequestQuery<TrackedCostWeb>, IAuthorizableRequest
{
    public required Guid CostId { get; init; }
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public string PermissionCode => PermissionCodes.ProjectResourcesReadSingle;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
```

**Pola:**

| Pole | Typ | Źródło | Opis |
|---|---|---|---|
| `CostId` | `Guid` | route | Identyfikator kosztu |
| `TenantId` | `Guid` | route | Identyfikator tenanta |
| `ProjectId` | `Guid` | route | Identyfikator projektu |

**Permission:** `ProjectResourcesReadSingle`

**Przepływ handlera:**
1. Załaduj i zwaliduj koszt (`GetAndValidateTrackedCostAsync`) — weryfikuje dostęp i istnienie
2. Pobierz załączniki dla kosztu
3. Zmapuj do `TrackedCostWeb`

---

## Commands

### CreateTrackedCostCommand

```csharp
public sealed record CreateTrackedCostCommand : IRequestCommand<TrackedCostWeb>, IAuthorizableRequest
{
    public Guid? CostEstimateId { get; init; }
    public Guid? CostEstimateItemId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal? Net { get; init; }
    public decimal? Gross { get; init; }
    public string? Contractor { get; init; }
    public DateTime? Date { get; init; }
    public IReadOnlyList<IFormFile>? NewFiles { get; init; }
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
}
```

**Pola:**

| Pole | Typ | Wymagane | Opis |
|---|---|---|---|
| `TenantId` | `Guid` | ✅ | Identyfikator tenanta (z route) |
| `ProjectId` | `Guid` | ✅ | Identyfikator projektu (z route) |
| `CostEstimateId` | `Guid?` | ❌ | Kosztorys, do którego należy koszt |
| `CostEstimateItemId` | `Guid?` | ❌ | Pozycja kosztorysu (tylko `RelationType = None`) |
| `Name` | `string` | ✅ | Nazwa kosztu (max 300 znaków) |
| `Description` | `string?` | ❌ | Opis (max 2000 znaków) |
| `Net` | `decimal?` | ❌ | Wartość netto (przynajmniej `Net` lub `Gross` musi być podane) |
| `Gross` | `decimal?` | ❌ | Wartość brutto |
| `Contractor` | `string?` | ❌ | Nazwa wykonawcy (max 300 znaków) |
| `Date` | `DateTime?` | ❌ | Data poniesienia kosztu |
| `NewFiles` | `IReadOnlyList<IFormFile>?` | ❌ | Nowe pliki do załączenia |

**Permission:** `ProjectResourcesWrite`

**Reguły biznesowe:**
- Jeśli podano `CostEstimateItemId`, wymagane jest też `CostEstimateId`
- `CostEstimateItemId` musi wskazywać na pozycję o `RelationType = None` (pozycja główna)
- Kosztorys musi należeć do projektu trackera
- Wartości finansowe wyliczane przez `ICostTrackerFinancialService.Calculate(Net, Gross)`

---

### UpdateTrackedCostCommand

```csharp
public sealed record UpdateTrackedCostCommand : IRequestCommand<TrackedCostWeb>, IAuthorizableRequest
{
    public Guid CostId { get; init; }
    public Guid? CostEstimateId { get; init; }
    public Guid? CostEstimateItemId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal? Net { get; init; }
    public decimal? Gross { get; init; }
    public decimal? VatRate { get; init; }
    public string? Contractor { get; init; }
    public DateTime? Date { get; init; }
    public IReadOnlyList<IFormFile>? NewFiles { get; init; }
    public required IReadOnlyList<Guid> ExistingAttachmentIds { get; init; }
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
}
```

**Pola:** Identyczne jak `CreateTrackedCostCommand` z następującymi różnicami:

| Pole | Typ | Wymagane | Opis |
|---|---|---|---|
| `CostId` | `Guid` | ✅ | Identyfikator istniejącego kosztu (z route) |
| `VatRate` | `decimal?` | ❌ | Stawka VAT — używana przez `ICostTrackerFinancialService.Calculate` |
| `ExistingAttachmentIds` | `IReadOnlyList<Guid>` | ✅ | Lista ID załączników do zachowania. Załączniki nieobecne na liście zostaną usunięte. |

**Permission:** `ProjectResourcesWrite`

**Zarządzanie załącznikami:**  
`ICostTrackerAttachmentService.SyncAttachmentsAsync(cost, NewFiles, ExistingAttachmentIds)` — synchronizuje stan: usuwa stare, dodaje nowe.

---

### UpdateTrackerBudgetCommand

```csharp
public sealed record UpdateTrackerBudgetCommand : IRequestCommand<Unit>, IAuthorizableRequest
{
    public required Guid CostTrackerId { get; init; }
    public decimal? BudgetNet { get; init; }
    public decimal? BudgetGross { get; init; }
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
}
```

**Pola:**

| Pole | Typ | Wymagane | Opis |
|---|---|---|---|
| `CostTrackerId` | `Guid` | ✅ | Identyfikator trackera (z route) |
| `TenantId` | `Guid` | ✅ | Identyfikator tenanta (z route) |
| `ProjectId` | `Guid` | ✅ | Identyfikator projektu (z route) |
| `BudgetNet` | `decimal?` | ❌ | Budżet netto trackera (`null` = wyzerowanie) |
| `BudgetGross` | `decimal?` | ❌ | Budżet brutto trackera (`null` = wyzerowanie) |

**Permission:** `ProjectResourcesWrite`

**Reguły biznesowe:**
- Partial update — aktualizowane są wyłącznie `BudgetNet` i `BudgetGross`; pozostałe pola encji `CostTracker` pozostają niezmienione
- Oba pola niezależne od siebie — brak automatycznych obliczeń między nimi
- Tracker musi należeć do podanego tenanta i projektu (weryfikacja przez predykat `Id + TenantId + ProjectId`)
- Wartości `BudgetNet`/`BudgetGross` są addytywne względem budżetów z kosztorysów przy obliczaniu `TotalBudgetNet`/`TotalBudgetGross` w `ComputeProjectSummary`

---

### DeleteTrackedCostCommand

```csharp
public sealed record DeleteTrackedCostCommand : IRequestCommand<Unit>, IAuthorizableRequest
{
    public required Guid CostId { get; init; }
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
}
```

**Pola:**

| Pole | Typ | Wymagane | Opis |
|---|---|---|---|
| `CostId` | `Guid` | ✅ | Identyfikator kosztu do usunięcia (z route) |
| `TenantId` | `Guid` | ✅ | Identyfikator tenanta (z route) |
| `ProjectId` | `Guid` | ✅ | Identyfikator projektu (z route) |

**Permission:** `ProjectResourcesWrite`

**Efekty:**
- Soft-delete kosztu (`IsDeleted = true`, `DeletedAt = now`)
- Soft-delete wszystkich powiązanych `TrackedCostAttachment`
- Fizyczne usunięcie plików z blob storage (`IBlobStorageService`) — błędy usunięcia blobów są logowane jako `Warning` i nie przerywają operacji

---

## Web Models

### CostTrackerDetailsWeb

Główny model odpowiedzi dla `GetCostTrackerByProjectQuery`.

```csharp
public record CostTrackerDetailsWeb
{
    public required Guid Id { get; init; }
    public required Guid ProjectId { get; init; }
    public required CostTrackerSummaryWeb Summary { get; init; }
    public required CostTrackerBudgetSummary BudgetSummary { get; init; }
    public required List<CostEstimateSummaryWeb> CostEstimateSummaries { get; init; }
    public required ProjectAdditionalCostsWeb ProjectAdditionalCosts { get; init; }
}
```

| Pole | Typ | Opis |
|---|---|---|
| `Id` | `Guid` | Identyfikator trackera |
| `ProjectId` | `Guid` | Identyfikator projektu |
| `Summary` | [`CostTrackerSummaryWeb`](#costtrackersummaryweb) | Zagregowane podsumowanie całego projektu (kosztorysy + koszty dodatkowe + `BudgetNet`/`BudgetGross` trackera) |
| `BudgetSummary` | [`CostTrackerBudgetSummary`](#costtrackerbudgetsummary) | Podsumowanie budżetowe wyłącznie dla kosztów dodatkowych projektu vs. budżet trackera |
| `CostEstimateSummaries` | `List<CostEstimateSummaryWeb>` | Podsumowania per kosztorys (bez `Groups` i `AdditionalCosts.Costs`) |
| `ProjectAdditionalCosts` | [`ProjectAdditionalCostsWeb`](#projectadditionalcostsweb) | Koszty dodatkowe projektu (bez kosztorysu) |

---

### CostTrackerSummaryBaseWeb

Abstrakcyjny rekord bazowy z polami wspólnymi dla `CostTrackerSummaryWeb` i `CostEstimateSummaryWeb`.

```csharp
public abstract record CostTrackerSummaryBaseWeb
{
    public decimal? TotalCostsNet { get; init; }
    public decimal? TotalCostsGross { get; init; }
    public decimal? TotalBudgetNet { get; init; }
    public decimal? TotalBudgetGross { get; init; }
    public decimal? TotalDeviationNet { get; init; }
    public decimal? TotalDeviationGross { get; init; }
    public decimal? TotalDeviationPercent { get; init; }
    public required bool IsBudgetExceeded { get; init; }
    public decimal? AdditionalCostsNet { get; init; }
    public decimal? AdditionalCostsGross { get; init; }
    public required int AdditionalCostsCount { get; init; }
    public required int CostCount { get; init; }
    public decimal? CoveredPercent { get; init; }
}
```

| Pole | Typ | Opis |
|---|---|---|
| `TotalCostsNet` / `TotalCostsGross` | `decimal?` | Suma kosztów powiązanych z pozycjami kosztorysu |
| `TotalBudgetNet` / `TotalBudgetGross` | `decimal?` | Łączny budżet |
| `TotalDeviationNet` | `decimal?` | Odchylenie kwotowe netto (koszty − budżet) |
| `TotalDeviationGross` | `decimal?` | Odchylenie kwotowe brutto |
| `TotalDeviationPercent` | `decimal?` | Odchylenie procentowe |
| `IsBudgetExceeded` | `bool` | `true` gdy koszty przekraczają budżet |
| `AdditionalCostsNet` / `AdditionalCostsGross` | `decimal?` | Suma kosztów dodatkowych |
| `AdditionalCostsCount` | `int` | Liczba kosztów dodatkowych |
| `CostCount` | `int` | Łączna liczba kosztów (powiązanych z pozycjami + dodatkowych) |
| `CoveredPercent` | `decimal?` | Procent pozycji kosztorysu pokrytych przynajmniej jednym kosztem |

---

### CostTrackerSummaryWeb

Zagregowane podsumowanie finansowe całego trackera (projekt). Dziedziczy z [`CostTrackerSummaryBaseWeb`](#costtrackersummarybaseweb).

```csharp
public record CostTrackerSummaryWeb : CostTrackerSummaryBaseWeb
{
    public required int CostEstimatesCount { get; init; }
    public required int CostEstimatesWithCostsCount { get; init; }
}
```

| Pole | Typ | Opis |
|---|---|---|
| *(pola z `CostTrackerSummaryBaseWeb`)* | | |
| `CostEstimatesCount` | `int` | Liczba kosztorysów powiązanych z trackerem |
| `CostEstimatesWithCostsCount` | `int` | Liczba kosztorysów z przynajmniej jednym kosztem |

---

### CostTrackerBudgetSummary

Podsumowanie budżetowe trackera zawężone **wyłącznie do kosztów dodatkowych projektu** (`TrackedCost` bez `CostEstimateId`). Dziedziczy z [`CostTrackerSummaryBaseWeb`](#costtrackersummarybaseweb) — brak dodatkowych pól.

```csharp
public record CostTrackerBudgetSummary : CostTrackerSummaryBaseWeb
{
}
```

**Semantyka pól odziedziczonych w tym kontekście:**

| Pole | Źródło wartości |
|---|---|
| `TotalCostsNet` / `TotalCostsGross` | Suma kosztów dodatkowych projektu (`ProjectAdditionalCostsWeb.TotalNet/TotalGross`) |
| `TotalBudgetNet` / `TotalBudgetGross` | `CostTracker.BudgetNet` / `CostTracker.BudgetGross` |
| `TotalDeviationNet` / `TotalDeviationGross` | `TotalCostsNet - TotalBudgetNet` / `TotalCostsGross - TotalBudgetGross` |
| `TotalDeviationPercent` | `(TotalCostsNet - TotalBudgetNet) / TotalBudgetNet * 100` |
| `IsBudgetExceeded` | `TotalDeviationNet > 0` |
| `AdditionalCostsNet` / `AdditionalCostsGross` | Identyczne z `TotalCostsNet` / `TotalCostsGross` |
| `AdditionalCostsCount` / `CostCount` | `ProjectAdditionalCostsWeb.CostsCount` |
| `CoveredPercent` | Zawsze `null` (brak pozycji kosztorysu) |

> Obliczany przez `ICostTrackerFinancialService.ComputeBudgetSummary(projectAdditionalCosts, budgetNet, budgetGross)`.

---

### CostEstimateSummaryWeb

Podsumowanie finansowe dla pojedynczego kosztorysu.

```csharp
public record CostEstimateSummaryWeb : CostTrackerSummaryBaseWeb
{
    public required Guid CostEstimateId { get; init; }
    public required string CostEstimateName { get; init; }
    public required int TotalItemsCount { get; init; }
    public required int ItemsWithCostsCount { get; init; }
    public required int ItemsWithoutCostsCount { get; init; }
    public required int ItemsOverBudgetCount { get; init; }
    public required int ItemsNearLimitCount { get; init; }
    public required List<TrackerGroupWeb> Groups { get; init; }
    public required TrackerAdditionalCostsWeb AdditionalCosts { get; init; }
}
```

| Pole | Typ | Opis |
|---|---|---|
| *(pola z `CostTrackerSummaryBaseWeb`)* | | |
| `CostEstimateId` | `Guid` | Identyfikator kosztorysu |
| `CostEstimateName` | `string` | Nazwa kosztorysu |
| `TotalItemsCount` | `int` | Łączna liczba głównych pozycji kosztorysu (`RelationType = None`) |
| `ItemsWithCostsCount` | `int` | Pozycje z przypisanymi kosztami |
| `ItemsWithoutCostsCount` | `int` | Pozycje bez kosztów |
| `ItemsOverBudgetCount` | `int` | Pozycje przekraczające budżet |
| `ItemsNearLimitCount` | `int` | Pozycje zbliżające się do limitu budżetu |
| `Groups` | `List<TrackerGroupWeb>` | Pełna hierarchia grup z pozycjami — wypełniana przez oba handlery |
| `AdditionalCosts` | [`TrackerAdditionalCostsWeb`](#trackeradditionalcostsweb) | Koszty dodatkowe kosztorysu (koszty bez powiązanej pozycji) |

---

### ProjectAdditionalCostsWeb

Koszty dodatkowe na poziomie projektu (`TrackedCost` bez powiązania z kosztorysem).

```csharp
public record ProjectAdditionalCostsWeb
{
    public decimal? TotalNet { get; init; }
    public decimal? TotalGross { get; init; }
    public required int CostsCount { get; init; }
    public required List<TrackedCostWeb> Costs { get; init; }
}
```

| Pole | Typ | Opis |
|---|---|---|
| `TotalNet` / `TotalGross` | `decimal?` | Suma kosztów (`null` gdy żaden koszt nie ma wartości) |
| `CostsCount` | `int` | Liczba kosztów dodatkowych projektu |
| `Costs` | `List<TrackedCostWeb>` | Lista poszczególnych kosztów dodatkowych projektu |

---

### TrackerAdditionalCostsWeb

Koszty dodatkowe na poziomie kosztorysu (`TrackedCost` z `CostEstimateId`, ale bez `CostEstimateItemId`).

```csharp
public record TrackerAdditionalCostsWeb
{
    public decimal? TotalNet { get; init; }
    public decimal? TotalGross { get; init; }
    public required int CostsCount { get; init; }
    public required List<TrackedCostWeb> Costs { get; init; }
}
```

| Pole | Typ | Opis |
|---|---|---|
| `TotalNet` / `TotalGross` | `decimal?` | Suma kosztów dodatkowych kosztorysu |
| `CostsCount` | `int` | Liczba kosztów dodatkowych kosztorysu |
| `Costs` | `List<TrackedCostWeb>` | Lista kosztów dodatkowych (wypełniana przez `GetCostTrackerByEstimateQuery`) |

---

### TrackedCostWeb

Reprezentacja pojedynczego kosztu śledzonego.

```csharp
record TrackedCostWeb(
    Guid Id,
    Guid TrackerId,
    Guid? CostEstimateId,
    Guid? CostEstimateItemId,
    bool IsAdditional,
    string Name,
    string? Description,
    decimal? Net,
    decimal? Gross,
    string? Contractor,
    DateTime? Date,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<TrackedCostAttachmentWeb> Attachments
)
```

| Pole | Typ | Opis |
|---|---|---|
| `Id` | `Guid` | Identyfikator kosztu |
| `TrackerId` | `Guid` | Identyfikator trackera |
| `CostEstimateId` | `Guid?` | Powiązany kosztorys (jeśli dotyczy) |
| `CostEstimateItemId` | `Guid?` | Powiązana pozycja kosztorysu (jeśli dotyczy) |
| `IsAdditional` | `bool` | `true` gdy brak `CostEstimateItemId` (koszt dodatkowy) |
| `Name` | `string` | Nazwa kosztu |
| `Description` | `string?` | Opis |
| `Net` / `Gross` | `decimal?` | Wartości finansowe |
| `Contractor` | `string?` | Wykonawca |
| `Date` | `DateTime?` | Data poniesienia kosztu |
| `CreatedAt` | `DateTime` | Data utworzenia (UTC) |
| `UpdatedAt` | `DateTime?` | Data ostatniej aktualizacji (UTC) |
| `Attachments` | `List<TrackedCostAttachmentWeb>` | Lista załączników |

---

### TrackedCostAttachmentWeb

```csharp
record TrackedCostAttachmentWeb(
    Guid Id,
    string OriginalFileName,
    string FileUrl,
    string ContentType,
    long FileSize,
    DateTime CreatedAt
)
```

| Pole | Typ | Opis |
|---|---|---|
| `Id` | `Guid` | Identyfikator załącznika |
| `OriginalFileName` | `string` | Oryginalna nazwa pliku |
| `FileUrl` | `string` | Podpisany URL do pobrania pliku (generowany przez `ICostTrackerAttachmentService.GenerateFileUrl`) |
| `ContentType` | `string` | MIME type |
| `FileSize` | `long` | Rozmiar pliku w bajtach |
| `CreatedAt` | `DateTime` | Data przesłania |

---

### TrackerNodeWeb

Abstrakcyjny rekord bazowy z polami finansowymi wspólnymi dla `TrackerGroupWeb` i `TrackerItemWeb`.

```csharp
public abstract record TrackerNodeWeb
{
    public decimal? BudgetNet { get; init; }
    public decimal? BudgetGross { get; init; }
    public decimal? CostsNet { get; init; }
    public decimal? CostsGross { get; init; }
    public decimal? DeviationNet { get; init; }
    public decimal? DeviationPercent { get; init; }
    public required bool IsBudgetExceeded { get; init; }
    public required int Status { get; init; }
    public required int CostCount { get; init; }
    public decimal? CoveredPercent { get; init; }
}
```

| Pole | Typ | Opis |
|---|---|---|
| `BudgetNet` / `BudgetGross` | `decimal?` | Budżet węzła |
| `CostsNet` / `CostsGross` | `decimal?` | Koszty węzła |
| `DeviationNet` | `decimal?` | Odchylenie kwotowe |
| `DeviationPercent` | `decimal?` | Odchylenie procentowe |
| `IsBudgetExceeded` | `bool` | Przekroczony budżet |
| `Status` | `int` | Wartość [`TrackedCostItemStatus`](#trackedcostitemstatus) |
| `CostCount` | `int` | Liczba przypisanych kosztów (dla grupy: suma ze wszystkich pozycji i podgrup) |
| `CoveredPercent` | `decimal?` | Dla grup: procent pozycji pokrytych przynajmniej jednym kosztem |

---

### TrackerGroupWeb

Grupa pozycji kosztorysu z podsumowaniem finansowym. Dziedziczy z [`TrackerNodeWeb`](#trackernodeweb).

```csharp
public record TrackerGroupWeb : TrackerNodeWeb
{
    public required Guid GroupId { get; init; }
    public required string GroupName { get; init; }
    public required int Order { get; init; }
    public required int TotalItemsCount { get; init; }
    public required int ItemsWithCostsCount { get; init; }
    public required List<TrackerItemWeb> Items { get; init; }
    public required List<TrackerGroupWeb> ChildGroups { get; init; }
}
```

| Pole | Typ | Opis |
|---|---|---|
| *(pola z `TrackerNodeWeb`)* | | |
| `GroupId` | `Guid` | Identyfikator grupy kosztorysu |
| `GroupName` | `string` | Nazwa grupy |
| `Order` | `int` | Kolejność wyświetlania |
| `TotalItemsCount` | `int` | Łączna liczba głównych pozycji w grupie i podgrupach |
| `ItemsWithCostsCount` | `int` | Liczba pozycji z przynajmniej jednym kosztem |
| `Items` | `List<TrackerItemWeb>` | Pozycje główne (`RelationType = None`) bezpośrednio w tej grupie |
| `ChildGroups` | `List<TrackerGroupWeb>` | Podgrupy (rekurencyjnie) |

---

### TrackerItemWeb

Pojedyncza pozycja kosztorysu z przypisanymi kosztami. Dziedziczy z [`TrackerNodeWeb`](#trackernodeweb).

```csharp
public record TrackerItemWeb : TrackerNodeWeb
{
    public required Guid CostEstimateItemId { get; init; }
    public required string Name { get; init; }
    public required List<TrackedCostWeb> Costs { get; init; }
}
```

| Pole | Typ | Opis |
|---|---|---|
| *(pola z `TrackerNodeWeb`)* | | |
| `CostEstimateItemId` | `Guid` | Identyfikator pozycji kosztorysu |
| `Name` | `string` | Nazwa pozycji |
| `Costs` | `List<TrackedCostWeb>` | Koszty przypisane do tej pozycji |

---

### TrackedCostItemStatus

Enum opisujący stan realizacji budżetu dla pozycji kosztorysu.

```csharp
enum TrackedCostItemStatus
{
    NoCosts    = 0,   // brak przypisanych kosztów
    NoBudget   = 1,   // brak budżetu dla pozycji
    InProgress = 2,   // koszty poniżej limitu
    NearLimit  = 3,   // koszty zbliżają się do budżetu
    OverBudget = 4    // koszty przekroczyły budżet
}
```

---

### CostEstimateItemCostsWeb

Model kosztów rzeczywistych osadzany w widoku kosztorysu — zwracany przez `ICostTrackerFinancialService.ComputeItemCosts` i `ComputeGroupCosts`.

```csharp
record CostEstimateItemCostsWeb(
    int CostsCount,
    decimal? CostsNet,
    decimal? CostsGross,
    decimal? DeviationAmount,
    decimal? DeviationPercent,
    bool IsOverBudget,
    int Status
)
```

| Pole | Typ | Opis |
|---|---|---|
| `CostsCount` | `int` | Liczba przypisanych kosztów |
| `CostsNet` / `CostsGross` | `decimal?` | Łączne koszty rzeczywiste |
| `DeviationAmount` | `decimal?` | Odchylenie kwotowe (koszty − budżet) |
| `DeviationPercent` | `decimal?` | Odchylenie procentowe |
| `IsOverBudget` | `bool` | `true` gdy koszty przekraczają budżet |
| `Status` | `int` | Wartość [`TrackedCostItemStatus`](#trackedcostitemstatus) |

---

## Walidacja

Walidacja odbywa się przez pipeline MediatR z FluentValidation. Błąd walidacji skutkuje odpowiedzią `400 Bad Request`.

### CreateTrackedCostCommand

| Pole | Reguła |
|---|---|
| `TenantId` | Wymagane (`NotEmpty`) |
| `ProjectId` | Wymagane (`NotEmpty`) |
| `Name` | Wymagane, max 300 znaków |
| `Description` | Max 2000 znaków |
| `Contractor` | Max 300 znaków |
| `Net` / `Gross` | Przynajmniej jedno musi być podane (gdy którekolwiek jest obecne) |

### UpdateTrackedCostCommand

| Pole | Reguła |
|---|---|
| `CostId` | Wymagane (`NotEmpty`) |
| `TenantId` | Wymagane (`NotEmpty`) |
| `ProjectId` | Wymagane (`NotEmpty`) |
| `Name` | Wymagane, max 300 znaków |
| `Description` | Max 2000 znaków |
| `Contractor` | Max 300 znaków |

### UpdateTrackerBudgetCommand

| Pole | Reguła |
|---|---|
| `CostTrackerId` | Wymagane (`NotEmpty`) |
| `TenantId` | Wymagane (`NotEmpty`) |
| `ProjectId` | Wymagane (`NotEmpty`) |
| `BudgetNet` | Gdy podane: `>= 0` |
| `BudgetGross` | Gdy podane: `>= 0` |

### DeleteTrackedCostCommand

| Pole | Reguła |
|---|---|
| `CostId` | Wymagane (`NotEmpty`) |
| `TenantId` | Wymagane (`NotEmpty`) |
| `ProjectId` | Wymagane (`NotEmpty`) |

### GetCostTrackerByProjectQuery

| Pole | Reguła |
|---|---|
| `ProjectId` | Wymagane (`NotEmpty`) |
| `TenantId` | Wymagane (`NotEmpty`) |

### GetCostTrackerByEstimateQuery

| Pole | Reguła |
|---|---|
| `CostEstimateId` | Wymagane (`NotEmpty`) |
| `TenantId` | Wymagane (`NotEmpty`) |
| `ProjectId` | Wymagane (`NotEmpty`) |

### GetTrackedCostsByItemQuery

| Pole | Reguła |
|---|---|
| `CostEstimateId` | Wymagane (`NotEmpty`) |
| `CostEstimateItemId` | Wymagane (`NotEmpty`) |
| `TenantId` | Wymagane (`NotEmpty`) |
| `ProjectId` | Wymagane (`NotEmpty`) |

### GetTrackedCostListQuery

| Pole | Reguła |
|---|---|
| `TenantId` | Wymagane (`NotEmpty`) |
| `ProjectId` | Wymagane (`NotEmpty`) |

### GetTrackedCostDetailsQuery

| Pole | Reguła |
|---|---|
| `CostId` | Wymagane (`NotEmpty`) |
| `TenantId` | Wymagane (`NotEmpty`) |
| `ProjectId` | Wymagane (`NotEmpty`) |

---

## Bezpieczeństwo i dostęp

Każdy handler weryfikuje dostęp przez `ValidateAccessAsync` w `CostTrackerHandlerBase`:

1. **User access** — `ICurrentUser.IsTenantOrProjectAdminAsync(tenantId, projectId)` — weryfikuje, czy użytkownik jest adminem tenanta lub projektu. Brak dostępu skutkuje `403 Forbidden`.

**Uprawnienia kontrolera:**

| Endpoint | Permission Code |
|---|---|
| GET by-project | `ProjectResourcesReadSingle` |
| POST costs | `ProjectResourcesWrite` |
| PUT costs/{costId} | `ProjectResourcesWrite` |
| PUT {costTrackerId}/budget | `ProjectResourcesWrite` |
| DELETE costs/{costId} | `ProjectResourcesWrite` |

---

## Integracja z widokiem kosztorysu

`GetCostEstimateDetailsQueryHandler` osadza dane z Cost Trackera bezpośrednio w odpowiedzi szczegółów kosztorysu (`CostEstimateDetailsWeb`), wzbogacając każdą grupę i każdą główną pozycję o rzeczywiste koszty śledzenia.

### Przepływ ładowania kosztów

1. Pobierz wszystkie `TrackedCost` powiązane z kosztorysem (`CostEstimateId == request.CostEstimateId`)
2. Zbuduj lookup `ILookup<Guid, TrackedCost>` po `CostEstimateItemId` (tylko koszty z przypisaną pozycją)
3. Dla każdej grupy oblicz koszty agregując wszystkie pozycje z grupy i podgrup:
   ```csharp
   CostEstimateItemCostsWeb groupTrackerCosts = trackerFinancialService.ComputeGroupCosts(
       group.TotalNet,
       allGroupItemIds.SelectMany(id => costsByItemId[id]));
   ```
4. Dla każdej głównej pozycji (`RelationType == None`) oblicz koszty:
   ```csharp
   CostEstimateItemCostsWeb? trackerCosts = trackerFinancialService.ComputeItemCosts(
       item, costsByItemId[item.Id]);
   ```
5. Pozycje podrzędne (`Option`, `Component`) zawsze mają `TrackerCosts = null`

### Pole TrackerCosts w modelach odpowiedzi

| Model | Pole | Typ | Opis |
|---|---|---|---|
| `CostEstimateGroupWeb` | `TrackerCosts` | `CostEstimateItemCostsWeb?` | Zagregowane koszty dla całej grupy (łącznie z podgrupami) |
| `CostEstimateItemWeb` | `TrackerCosts` | `CostEstimateItemCostsWeb?` | Koszty dla pozycji głównej; `null` dla opcji i komponentów |

### Metody ICostTrackerFinancialService

| Metoda | Parametry | Zwraca | Opis |
|---|---|---|---|
| `Calculate` | `decimal? net`, `decimal? gross` | `(decimal? Net, decimal? Gross)` | Uzupełnia brakujące pola finansowe |
| `ComputeItemStatus` | `decimal? budgetNet`, `decimal? costsNet`, `int costsCount` | `TrackedCostItemStatus` | Oblicza status pozycji kosztorysu |
| `ComputeProjectSummary` | `IReadOnlyCollection<CostEstimateSummaryWeb>`, `ProjectAdditionalCostsWeb`, `decimal? budgetNet`, `decimal? budgetGross` | `CostTrackerSummaryWeb` | Agreguje dane ze wszystkich kosztorysów; `budgetNet`/`budgetGross` trackera dodawane addytywnie do sumy budżetów z kosztorysów |
| `ComputeBudgetSummary` | `ProjectAdditionalCostsWeb`, `decimal? budgetNet`, `decimal? budgetGross` | `CostTrackerBudgetSummary` | Oblicza summary wyłącznie na podstawie kosztów dodatkowych projektu vs. budżet trackera |
| `ComputeEstimateSummary` | `CostEstimate`, `IReadOnlyCollection<CostEstimateItem>`, `ILookup<Guid, TrackedCost>`, `decimal? additionalNet`, `decimal? additionalGross`, `int additionalCostsCount` | `CostEstimateSummaryWeb` | Oblicza wszystkie wskaźniki dla jednego kosztorysu |
| `ComputeGroupCosts` | `decimal? budgetNet`, `IEnumerable<TrackedCost> allGroupCosts` | `CostEstimateItemCostsWeb` | Agreguje koszty wszystkich pozycji w grupie i podgrupach |
| `ComputeItemCosts` | `CostEstimateItem item`, `IEnumerable<TrackedCost> itemCosts` | `CostEstimateItemCostsWeb` | Oblicza koszty dla pojedynczej pozycji kosztorysu |
