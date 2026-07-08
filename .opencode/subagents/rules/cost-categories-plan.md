# Plan wdrożenia: Kategorie kosztów projektowych

> Feature spec: `.opencode/features/cost-categories.md`
> Data planu: 2026-07-08

## Typ zmiany

**Full-stack** — encje/DB, CQRS, kontrolery, serwis AI, assembler dashboardu, ustawienia projektu, modal kosztów, wykres na dashboardzie.

## Opis

Rozszerzenie modelu kosztów (`BaseCost` → `ProjectCost` + `TrackedCost`) o opcjonalną kategorię wydatku. Kategorie są słownikiem per projekt (jak jednostki miary), seedowane przy `CreateProject` (10 domyślnych pozycji), zarządzane w Parametrach projektu, wybierane w ujednoliconym `CostModal`, dopasowywane/tworzone przez AI (wzorzec kontrahenta), a na zakładce **Finanse** dashboardu pojawia się wykres kołowy rozkładu kosztów wg kategorii (w tym segment „Bez kategorii").

## Warstwy do zmiany

- [x] **Encje / migracje DB** — `ProjectCostCategory`, `BaseCost.CategoryId`, konfiguracja EF, migracja
- [x] **CQRS** — CRUD kategorii, rozszerzenie Create/Update kosztów, seed w `CreateProject`, agregacja dashboardu
- [x] **WebApi** — endpointy w `ProjectController` (wzorzec `ProjectUnit`)
- [x] **Business** — `DocumentParserService` + enrichment w `ParseCostDocumentQueryHandler`, `ProjectDashboardAssembler`
- [x] **UI** — `CostCategoryManager`, hooki API, `CostModal` + AI flow, `CostCategoryPieChart` na `FinanceTab`
- [ ] **Nowa warstwa** — nie dotyczy (wykorzystujemy istniejący moduł AI)

## Kontekst z repozytorium

| Obszar | Stan obecny | Co trzeba zrobić |
|--------|-------------|------------------|
| `BaseCost` | Brak `CategoryId` | Dodać nullable FK + nawigację |
| `CreateProjectCommandHandler` | Seeduje `DefaultUnits` + walutę PLN | Dodać `DefaultCostCategories` (10 pozycji) |
| `ProjectUnit` / `UnitManager` | Wzorzec CRUD + reorder w parametrach | Skopiować wzorzec dla kategorii |
| `CostModal` | AI flow dla kontrahenta (`isAiContractorCreateOpen`) | Analogiczny flow dla kategorii |
| `ParsedCostDto` | Tylko pola kontrahenta | Dodać `categoryId`, `categoryFound`, `suggestedCategory` |
| `FinanceTab` | KPI + kosztorysy + ostatnie koszty | Dodać wykres kołowy (wzorce: `CostSourcesDonut`, `TopContractorsChart`) |
| `chartAggregations.pickCostAmount` | Używa **netto** (`cost.net`) | Wykres kategorii — spójnie netto |
| `ProjectDashboardWeb` | Brak `costByCategory` | Dodać `List<CostByCategoryWeb>` |

## Plan kroków

### 1. Audyt API
Skupić się na:
- Encje: `BaseCost`, `ProjectCost`, `TrackedCost`, `ProjectUnit`, `ProjectParams`
- Handlery kosztów: `Create/UpdateProjectCost`, `Create/UpdateTrackedCost`, `CostTrackerHandlerBase.MapCostToWeb`
- Handlery jednostek jako wzorzec: `Get/Add/Update/Delete/ReorderProjectUnit`
- `CreateProjectCommandHandler` (seed)
- `ParseCostDocumentQueryHandler`, `DocumentParserService`
- `ProjectDashboardAssembler`, `IDashboardDataLoader`
- `ProjectController` (routing jednostek)
- Uprawnienia: `ProjectView` (GET), `ProjectSettings` (mutacje) — jak jednostki
- Testy: `CQRS.Tests`, `Business.Tests`

### 2. Audyt UI
Skupić się na:
- `ProjectParameters.tsx` — miejsce na nową sekcję
- `UnitManager.tsx`, `useProjectUnits`, `projectApi` — wzorzec hooków/API
- `CostModal.tsx` — wspólne pole kategorii + AI flow
- `CostFormModal.tsx` — sprawdzić czy nadal używany (legacy); jeśli tak — zsynchronizować lub oznaczyć do deprecacji
- `FinanceTab.tsx` — layout wykresu
- `CostSourcesDonut.tsx`, `ChartCard`, `chartTheme` — wzorzec wykresów
- Typy: `projectDashboard.types.ts`, `project.types.ts`, `ai.types.ts`
- Testy AXE: `dashboard-charts.axe.test.tsx`, `dashboard-main.axe.test.tsx`

### 3. Zmiany API (prompty implementacyjne — po audycie)

| # | Plik promptu | Zakres |
|---|-------------|--------|
| api-fix-01 | `cost-categories-api-fix-01.md` | Encja `ProjectCostCategory`, konfiguracja EF, `CategoryId` na `BaseCost`, migracja, `DbSet` |
| api-fix-02 | `cost-categories-api-fix-02.md` | CQRS CRUD kategorii (Get/Add/Update/Delete/Reorder) + web modele + validatory |
| api-fix-03 | `cost-categories-api-fix-03.md` | Endpointy w `ProjectController`, rejestracja w DI jeśli potrzebna |
| api-fix-04 | `cost-categories-api-fix-04.md` | Seed 10 domyślnych kategorii w `CreateProjectCommandHandler` |
| api-fix-05 | `cost-categories-api-fix-05.md` | Rozszerzenie Create/Update kosztów o `CategoryId?`, walidacja FK per projekt |
| api-fix-06 | `cost-categories-api-fix-06.md` | `MapCostToWeb` + web modele kosztów: `CategoryId`, `CategoryName` |
| api-fix-07 | `cost-categories-api-fix-07.md` | AI: rozszerzenie `ParsedCostDto`, prompt w `DocumentParserService`, enrichment kategorii w handlerze |
| api-fix-08 | `cost-categories-api-fix-08.md` | `ProjectDashboardAssembler`: `CostByCategoryWeb[]`, agregacja netto, segment „Bez kategorii" |
| api-fix-09 | `cost-categories-api-fix-09.md` | Testy jednostkowe handlerów CQRS (CRUD kategorii, delete z odpinaniem, seed, dashboard agg.) |

### 4. Zmiany UI (prompty implementacyjne — po audycie)

| # | Plik promptu | Zakres |
|---|-------------|--------|
| ui-fix-01 | `cost-categories-ui-fix-01.md` | Typy TS + funkcje API + hooki React Query (`useProjectCostCategories`) |
| ui-fix-02 | `cost-categories-ui-fix-02.md` | `CostCategoryManager` w `ProjectParameters` (wzorzec `UnitManager`) |
| ui-fix-03 | `cost-categories-ui-fix-03.md` | `CostCategoryPicker` + integracja w `CostModal` (create/edit, tracked + project) |
| ui-fix-04 | `cost-categories-ui-fix-04.md` | AI flow: badge „AI znalazł", alert + `CostCategoryQuickAddModal` (wzorzec kontrahenta) |
| ui-fix-05 | `cost-categories-ui-fix-05.md` | `CostCategoryPieChart` + integracja w `FinanceTab`, `groupCostsByCategory` w aggregations |
| ui-fix-06 | `cost-categories-ui-fix-06.md` | Test AXE wykresu + testy hooków jeśli wymagane |

### Kolejność wykonania (zależności)

```
api-fix-01 → api-fix-02 → api-fix-03 → api-fix-04
                ↓
         api-fix-05 → api-fix-06
                ↓
         api-fix-07 (AI — po CRUD kategorii)
                ↓
         api-fix-08 (dashboard — po MapCostToWeb)
                ↓
         api-fix-09 (testy)

ui-fix-01 → ui-fix-02 (parametry)
         → ui-fix-03 → ui-fix-04 (modal + AI — po api-fix-05/07)
         → ui-fix-05 (wykres — po api-fix-08)
         → ui-fix-06 (testy)
```

API przed UI dla typów i endpointów. Modal kategorii po CRUD API. Wykres po rozszerzeniu `ProjectDashboardWeb`.

## Szczegóły techniczne (decyzje projektowe)

### Encja `ProjectCostCategory`
```csharp
// Dziedziczy ProjectParams (Id, ProjectId) — jak ProjectUnit
public class ProjectCostCategory : ProjectParams
{
    public string Name { get; set; }      // wymagane, max 100
    public string? Code { get; set; }     // opcjonalny skrót, max 20
    public int Order { get; set; }
    public string? Color { get; set; }    // opcjonalny hex, max 7
}
```

### Domyślne kategorie (seed w `CreateProject`)
| Order | Code | Name |
|-------|------|------|
| 1 | mat | Materiały budowlane |
| 2 | rob | Robocizna |
| 3 | sprzet | Sprzęt i maszyny |
| 4 | transport | Transport i logistyka |
| 5 | uslugi | Usługi zewnętrzne |
| 6 | admin | Administracja i biuro |
| 7 | media | Energia i media |
| 8 | podwyk | Podwykonawcy |
| 9 | narz | Narzędzia i wyposażenie |
| 10 | inne | Inne |

### Web model dashboardu
```csharp
public sealed record CostByCategoryWeb
{
    public Guid? CategoryId { get; init; }      // null = „Bez kategorii"
    public required string CategoryName { get; init; }
    public string? Color { get; init; }
    public decimal Net { get; init; }
    public decimal? Gross { get; init; }
    public int CostsCount { get; init; }
}
```

### AI — rozszerzenie `ParsedCostDto`
Analogicznie do kontrahenta:
- `CategoryId?` + `CategoryFound: bool` — gdy dopasowano istniejącą
- `SuggestedCategoryDto?` — `{ Name, Code? }` gdy AI proponuje nową
- Enrichment w `ParseCostDocumentQueryHandler` po `EnrichWithContractorAsync` — wyszukiwanie po nazwie/kodzie w kategoriach projektu (fuzzy/contains)

### Usuwanie kategorii
FK `OnDelete(DeleteBehavior.SetNull)` + handler weryfikujący istnienie — koszty automatycznie trafiają do „Bez kategorii".

## Pytania do rozstrzygnięcia — rekomendacje

| # | Pytanie | Rekomendacja | Uzasadnienie |
|---|---------|--------------|--------------|
| 1 | Czy kategorie mają pole `Color`? | **Tak — opcjonalne `Color` + fallback z `CHART_PALETTE`** | Użytkownik może personalizować wykres; bez koloru UI używa palety jak `CostSourcesDonut` |
| 2 | Usuwanie kategorii używanej przez koszty? | **Odpinać koszty (`CategoryId = null`)** | Lepsze UX niż blokada; spójne z FK `SetNull`; segment „Bez kategorii" na wykresie |
| 3 | Kategorie per-projekt czy per-tenant? | **Per-projekt** (jak jednostki i waluta) | Zgodne ze specyfikacją i istniejącym wzorcem `ProjectParams` |
| 4 | Wykres kołowy: netto, brutto czy przełącznik? | **Netto** (jak `pickCostAmount` i pozostałe wykresy na `CostsTab`) | Spójność z `TopContractorsChart`, `CostTimeSeriesChart`; KPI na `FinanceTab` nadal pokazują netto+brutto |
| 5 | Czy pole `Code` kategorii ma być unikalne w obrębie projektu? | **Tak** — unikalny indeks `(ProjectId, Code)` gdzie `Code IS NOT NULL`; przy AI-suggested bez kodu generować slug z nazwy | Unikanie duplikatów |
| 6 | Czy aktualizować legacy `CostFormModal`? | **Tylko jeśli audyt UI pokaże aktywne użycie** — priorytet: `CostModal` (już ujednolicony) | Minimalizacja scope |

## Kryteria akceptacji

1. 10 kategorii przy tworzeniu projektu
2. CRUD kategorii w Parametrach projektu
3. Opcjonalny wybór kategorii w `CostModal` (tracked + project)
4. AI dobiera kategorię lub proponuje utworzenie nowej z informacją
5. Wykres kołowy na zakładce Finanse
6. „Bez kategorii" dla kosztów bez `CategoryId`
7. Build API + UI bez błędów
8. Testy jednostkowe CQRS
9. Test AXE wykresu

## Status

**Oczekuje na zatwierdzenie użytkownika** (Krok 2 workflow feature-planner-agent)
