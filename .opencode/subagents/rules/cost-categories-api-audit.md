# Raport audytu API: cost-categories

> Data: 2026-07-08  
> Feature spec: `.opencode/features/cost-categories.md`  
> Plan: `.opencode/subagents/rules/cost-categories-plan.md`  
> Status: zatwierdzony przez użytkownika

---

## Executive summary

Feature **cost-categories** wymaga **pełnego nowego slice** w warstwie API. W repozytorium **nie istnieje** żadna implementacja kategorii kosztów (`CategoryId`, `ProjectCostCategory`, `CostByCategory` — 0 wystąpień w kodzie).

Wzorce referencyjne są gotowe i spójne:
- `ProjectUnit` — CRUD + reorder w `ProjectController`
- `CreateProjectCommandHandler` — seed domyślnych danych
- `ParseCostDocumentQueryHandler.EnrichWithContractorAsync` — wzorzec AI enrichment
- `DashboardDataLoader` + `ProjectDashboardAssembler` — agregacja dashboardu

| Metryka | Wartość |
|---------|---------|
| Nowe encje | 1 (`ProjectCostCategory` w TPH `ProjectParams`) |
| Nowe Commands/Queries | 6 (5 CRUD/reorder + modyfikacje istniejących) |
| Nowe endpointy REST | 5 (mirror `/units`) |
| Wymaga migracji DB | **tak** |
| Modyfikacje istniejących handlerów | ~12 plików |
| Pytania otwarte | 2 (z rekomendacjami poniżej) |

---

## Stan obecny

### Encje

**`BaseCost`** (`Entities/Models/Costs/BaseCost.cs`):
- Brak `CategoryId`
- Ma już nullable FK: `ContractorId`, `CostEstimateItemId`, `WorkScheduleStageWorkId`
- Dziedziczone przez `ProjectCost` i `TrackedCost`

**`ProjectParams` TPH** (`ProjectParamsConfiguration.cs`):
- Discriminator `ParamType`: `Currency`, `Unit`
- Tabela `ProjectParams`, relacja 1:N z `Project`
- **Brak** discriminatora dla kategorii kosztów

**`ProjectUnit`** — wzorzec docelowy:
- `Code` (required), `Name`, `Symbol?`, `Order`
- Handlery CQRS w `CQRS/Projects/{Get,Add,Update,Delete,Reorder}ProjectUnit/`

### Endpointy (istniejące)

`ProjectController` (~linie 220–315):
- `GET /units` → `GetProjectUnitsQuery` — `ProjectView`
- `POST /units` → `AddProjectUnitCommand` — `ProjectSettings`
- `PUT /units/{id}` → `UpdateProjectUnitCommand` — `ProjectSettings`
- `DELETE /units/{id}` → `DeleteProjectUnitCommand` — `ProjectSettings`
- `PUT /units/reorder` → `ReorderProjectUnitsCommand` — `ProjectSettings`

**Brak** endpointów `/cost-categories`.

### Handlery kosztów

Mapowanie kosztów do web modeli w **4 miejscach** (wymaga synchronizacji):
1. `CostTrackerHandlerBase.MapCostToWeb`
2. `GetProjectCostsQueryHandler.MapToWeb`
3. `CreateProjectCostCommandHandler.MapToWeb`
4. `UpdateProjectCostCommandHandler.MapToWeb`

Create/Update Commands dla `ProjectCost` i `TrackedCost` — brak pola `CategoryId`.

### Seed przy CreateProject

`CreateProjectCommandHandler`:
- Seeduje walutę PLN
- Seeduje 13 domyślnych jednostek (`DefaultUnits`)
- **Brak** seedu kategorii kosztów

### AI

`ParseCostDocumentQueryHandler`:
- Ma `ProjectId` w query, ale enrichment kontrahenta jest **tenant-scoped** (`EnrichWithContractorAsync(tenantId)`)
- `ParsedCostDto` — tylko pola kontrahenta, brak kategorii
- `DocumentParserService` — prompt bez kategorii

Kategorie muszą być **project-scoped** (lookup w kategoriach danego projektu).

### Dashboard

`DashboardDataLoader.LoadAllCostsAsync`:
- Ładuje `TrackedCost` + `ProjectCost` ze statusem **`Approved`**
- Wykres kategorii musi używać **tego samego zbioru**

`ProjectDashboardWeb` / `ProjectDashboardAssembler`:
- Brak `CostByCategoryWeb[]`
- Brak agregacji po kategorii

### Testy

- Brak testów CRUD `ProjectUnit` w `CQRS.Tests`
- Istnieją testy `CreateProjectCost`, `TrackedCost` — do rozszerzenia o `CategoryId`
- `CreateProjectCommandHandlerTests` — nie weryfikuje seedu units (nowe testy kategorii mogą być wzorcem)

---

## Wzorce referencyjne

| Obszar | Ścieżka |
|--------|---------|
| Encja jednostki | `src/Entities/Models/Projects/ProjectUnit.cs` |
| TPH config | `src/Entities/Configurations/Projects/ProjectParamsConfiguration.cs` |
| Unit EF config | `src/Entities/Configurations/Projects/ProjectUnitConfiguration.cs` |
| CRUD jednostek | `src/CQRS/Projects/{Get,Add,Update,Delete,Reorder}ProjectUnit/` |
| Routing | `src/WebApi/Controllers/ProjectController.cs` |
| Seed | `src/CQRS/Projects/CreateProject/CreateProjectCommandHandler.cs` |
| BaseCost FK | `src/Entities/Configurations/Costs/BaseCostConfiguration.cs` |
| Mapowanie kosztów | `src/CQRS/CostTrackers/Shared/CostTrackerHandlerBase.cs` |
| AI enrichment | `src/CQRS/AI/ParseCostDocument/ParseCostDocumentQueryHandler.cs` |
| ParsedCostDto | `src/Business/Interfaces/WebModels/AI/ParsedCostDto.cs` |
| DocumentParser | `src/Business/Implementation/Services/AI/DocumentParserService.cs` |
| Dashboard loader | `src/CQRS/ProjectDashboard/Services/DashboardDataLoader.cs` |
| Dashboard assembler | `src/CQRS/ProjectDashboard/Services/ProjectDashboardAssembler.cs` |
| Walidacja koloru | `src/CQRS/Extensions/CommonValidationExtensions.cs` (`ValidColorRgb`) |

---

## Pliki do utworzenia

### Encje i konfiguracja
- `Entities/Models/Projects/ProjectCostCategory.cs`
- `Entities/Configurations/Projects/ProjectCostCategoryConfiguration.cs`
- Migracja EF: `add-project-cost-categories`

### Web modele (Business)
- `Business/Interfaces/WebModels/Projects/ProjectCostCategoryWeb.cs`
- `Business/Interfaces/WebModels/Projects/UpsertProjectCostCategoryWeb.cs`
- `Business/Interfaces/WebModels/ProjectDashboard/CostByCategoryWeb.cs`
- Rozszerzenie `ParsedCostDto`: `CategoryId?`, `CategoryFound`, `SuggestedCategoryDto?`

### CQRS — CRUD kategorii (5 folderów × 2–3 pliki)
- `CQRS/Projects/GetProjectCostCategories/`
- `CQRS/Projects/AddProjectCostCategory/`
- `CQRS/Projects/UpdateProjectCostCategory/`
- `CQRS/Projects/DeleteProjectCostCategory/`
- `CQRS/Projects/ReorderProjectCostCategories/`

### Opcjonalnie (rekomendowane)
- `Business/Interfaces/Services/IProjectCostCategoryService.cs`
- `Business/Implementation/Services/ProjectCostCategoryService.cs`  
  (lookup po nazwie/kodzie dla AI + batch resolve nazw w dashboardzie)

### Testy
- `tests/CQRS.Tests/Projects/AddProjectCostCategoryCommandHandlerTests.cs`
- `tests/CQRS.Tests/Projects/DeleteProjectCostCategoryCommandHandlerTests.cs` (SetNull)
- `tests/CQRS.Tests/Projects/CreateProjectCommandHandlerTests.cs` (seed 10 kategorii)
- `tests/CQRS.Tests/ProjectCosts/CreateProjectCostCommandHandlerTests.cs` (CategoryId FK)
- `tests/CQRS.Tests/ProjectDashboard/ProjectDashboardAssemblerTests.cs` (agregacja)

---

## Pliki do modyfikacji

| Warstwa | Pliki |
|---------|-------|
| **Encje** | `BaseCost.cs`, `BaseCostConfiguration.cs`, `ProjectParamsConfiguration.cs`, `AppDbContext.cs` |
| **Seed** | `CreateProjectCommandHandler.cs` |
| **Koszty CQRS** | `Create/UpdateProjectCost` (Command, Handler, Validator), `Create/UpdateTrackedCost` (Command, Handler, Validator), `CostTrackerHandlerBase.cs`, `GetProjectCostsQueryHandler.cs` |
| **Web modele kosztów** | `TrackedCostWeb`, `ProjectCostListItemWeb`, `CreateProjectCostCommand`, `UpdateProjectCostCommand`, `CreateTrackedCostCommand`, `UpdateTrackedCostCommand` |
| **AI** | `ParsedCostDto`, `DocumentParserService`, `ParseCostDocumentQueryHandler`, `IDocumentParserService` |
| **Dashboard** | `ProjectDashboardWeb`, `ProjectDashboardAssembler`, ewentualnie `IDashboardDataLoader` |
| **WebApi** | `ProjectController.cs` (+5 endpointów), `ServiceCollectionExtensions.cs` (repository) |
| **DI** | Rejestracja `IRepository<ProjectCostCategory>` jeśli osobna encja poza TPH — **uwaga**: w TPH używa się `IRepository<ProjectParams>` lub dedykowanego repo |

---

## Szczegóły techniczne implementacji

### Encja `ProjectCostCategory`

```csharp
public class ProjectCostCategory : ProjectParams
{
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
    public int Order { get; set; }
    public string? Color { get; set; }  // #RRGGBB, ValidColorRgb()
}
```

### TPH discriminator

W `ProjectParamsConfiguration.cs`:
```csharp
.HasValue<ProjectCostCategory>("CostCategory")
```

### FK na BaseCost

```csharp
builder.HasOne<ProjectCostCategory>()
    .WithMany()
    .HasForeignKey(c => c.CategoryId)
    .OnDelete(DeleteBehavior.SetNull);
```

Indeks: `CategoryId` (nullable).

Unikalny indeks kategorii: `(ProjectId, Code)` z filtrem `[Code] IS NOT NULL`.

### Walidacja CategoryId w handlerach kosztów

Przy Create/Update:
1. Jeśli `CategoryId` podane — sprawdź istnienie kategorii z `ProjectId == request.ProjectId`
2. Jeśli null — OK (opcjonalne)
3. `NotFoundApiException` gdy kategoria nie istnieje lub należy do innego projektu

### AI enrichment (nowa metoda)

Po `EnrichWithContractorAsync`:
```csharp
EnrichWithCategoryAsync(dto, request.ProjectId, cancellationToken)
```
- Jeśli AI zwróciło `CategoryName` / `CategoryCode` — szukaj w kategoriach projektu (contains, case-insensitive)
- Znaleziono → `CategoryId`, `CategoryFound = true`
- Nie znaleziono → `CategoryFound = false`, `SuggestedCategory = { Name, Code? }`

Rozszerzyć prompt w `DocumentParserService` o listę dostępnych kategorii projektu (przekazać z handlera).

### Dashboard agregacja

```csharp
// Grupuj po CategoryId (null → "Bez kategorii")
// Sumuj Net (zgodnie z pickCostAmount / chartAggregations)
// Opcjonalnie Gross
// CostsCount per segment
```

Źródło: ten sam `allCosts` co reszta dashboardu (Approved only).

---

## Ryzyka i uwagi

| Ryzyko | Poziom | Mitygacja |
|--------|--------|-----------|
| Brak encji/FK — feature nie działa | Krytyczne | api-fix-01 jako pierwszy krok |
| Niespójne mapowanie w 4 ścieżkach | Wysokie | Jedna metoda `MapCostToWeb` lub wspólny helper |
| Istniejące projekty bez kategorii | Wysokie | Backfill w migracji SQL (rekomendacja) |
| AI tenant-scoped vs project-scoped | Wysokie | Przekazać `ProjectId` do enrichment kategorii |
| N+1 przy nazwach kategorii | Normalne | Batch load kategorii w dashboard assembler |
| TPH — wiele typów w `ProjectParams` | Normalne | Discriminator `CostCategory`, osobna konfiguracja jak `ProjectUnitConfiguration` |
| Gross na wykresie | Normalne | Plan: netto primary; Gross opcjonalnie w `CostByCategoryWeb` |

---

## Znaleziska wg priorytetu

### Krytyczne
1. Brak encji `ProjectCostCategory` i `CategoryId` na `BaseCost`
2. Brak CQRS CRUD kategorii
3. Brak endpointów REST
4. Brak rozszerzenia Create/Update `ProjectCost` + `TrackedCost`
5. Wymagana migracja EF Core

### Wysokie
1. Backfill istniejących projektów (10 domyślnych kategorii)
2. Walidacja `CategoryId` per `ProjectId` w handlerach kosztów
3. Zakres kosztów na wykresie — tylko Approved (jak `DashboardDataLoader`)
4. Synchronizacja mapowania w 4 ścieżkach
5. AI enrichment project-scoped
6. Unikalny indeks `(ProjectId, Code)` z nullable Code

### Normalne
1. Opcjonalny `IProjectCostCategoryService` dla AI + batch lookup
2. N+1 przy resolve nazw kategorii
3. Rozszerzenie promptu AI o listę kategorii
4. Brak istniejących testów CRUD jednostek — nowe testy kategorii jako wzorzec

---

## Rekomendacje implementacyjne (kolejność)

1. **api-fix-01** — encja, TPH, FK, migracja (+ backfill SQL)
2. **api-fix-02** — CQRS CRUD + validatory
3. **api-fix-03** — endpointy `ProjectController`
4. **api-fix-04** — seed w `CreateProjectCommandHandler`
5. **api-fix-05** — Create/Update kosztów + walidacja FK
6. **api-fix-06** — `MapCostToWeb` we wszystkich ścieżkach
7. **api-fix-07** — AI: DTO, prompt, enrichment
8. **api-fix-08** — `ProjectDashboardAssembler` + `CostByCategoryWeb`
9. **api-fix-09** — testy jednostkowe

---

## Pytania otwarte — rekomendacje

### 1. Backfill istniejących projektów?

**Rekomendacja: TAK** — migracja SQL wstawia 10 domyślnych kategorii do wszystkich istniejących projektów (ten sam zestaw co `DefaultCostCategories` w `CreateProject`).

Uzasadnienie: spójne UX — każdy projekt ma od razu słownik kategorii; wykres i AI działają bez ręcznej konfiguracji.

### 2. Code przy Add/Update — required czy optional?

**Rekomendacja: Optional** (różnica vs `ProjectUnit` gdzie Code jest required).

- `Name` — required
- `Code` — optional; unikalny w projekcie gdy podany
- Przy AI-suggested: generować slug z nazwy (np. `Materiały budowlane` → `mat-bud`)
- UI może pokazywać Code jako opcjonalne pole

---

## Następny krok

Po zatwierdzeniu raportu API → **audyt UI** (`cost-categories-ui-audit.md`).
