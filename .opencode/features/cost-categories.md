# Feature: Kategorie kosztów projektowych

## Opis

Dodanie możliwości przypisywania kategorii kosztów do wydatków projektowych (`ProjectCost`) i kosztów śledzonych (`TrackedCost`). Kategorie są konfigurowalne w ustawieniach projektu (analogicznie do waluty i jednostek miary). Przy tworzeniu projektu automatycznie dodawane jest 10 domyślnych kategorii wydatków. Agent AI przy dodawaniu kosztów automatycznie dobiera kategorię lub tworzy nową (z informacją dla użytkownika). Na dashboardzie (zakładka Finanse) pojawia się wykres kołowy rozkładu kosztów wg kategorii.

## Problem

Obecnie koszty (`ProjectCost`, `TrackedCost`) nie mają pola kategorii. Użytkownicy nie mogą klasyfikować wydatków ani analizować ich rozkładu wg typu (materiały, robocizna, transport itp.). Brak spójnego mechanizmu zarządzania kategoriami w ustawieniach projektu.

## Cel

1. Encja `ProjectCostCategory` per projekt (jak `ProjectUnit`, `ProjectCurrency`)
2. Opcjonalne pole `CategoryId` na `BaseCost` (dziedziczone przez `ProjectCost` i `TrackedCost`)
3. CRUD kategorii w ustawieniach projektu
4. 10 domyślnych kategorii przy `CreateProject`
5. Wybór kategorii w modalach dodawania/edycji kosztów (ProjectCost + TrackedCost)
6. AI agent: auto-dobór kategorii lub tworzenie nowej z powiadomieniem użytkownika (wzorzec jak kontrahent w `CostModal.tsx`)
7. Wykres kołowy na zakładce Finanse dashboardu — rozkład kosztów wg kategorii
8. Koszty bez kategorii grupowane jako „Bez kategorii" na wykresie

## Wymagania

### Encje / DB
- Nowa encja `ProjectCostCategory`:
  - `Id`, `ProjectId`, `Name`, `Code` (opcjonalny skrót), `Order`, `Color` (opcjonalny kolor do wykresu)
  - Relacja: `Project` 1:N `ProjectCostCategory`
- Rozszerzenie `BaseCost`:
  - `Guid? CategoryId` (nullable — kategoria opcjonalna)
  - FK do `ProjectCostCategory`
- Migracja EF Core

### Domyślne kategorie (10 szt.) przy tworzeniu projektu
Wzorzec jak `DefaultUnits` w `CreateProjectCommandHandler`:
1. Materiały budowlane
2. Robocizna
3. Sprzęt i maszyny
4. Transport i logistyka
5. Usługi zewnętrzne
6. Administracja i biuro
7. Energia i media
8. Podwykonawcy
9. Narzędzia i wyposażenie
10. Inne

### API — CQRS
- `GetProjectCostCategories` — lista kategorii projektu
- `AddProjectCostCategory` — dodanie kategorii
- `UpdateProjectCostCategory` — edycja
- `DeleteProjectCostCategory` — usunięcie (walidacja: czy kategoria nie jest używana lub soft-unassign)
- `ReorderProjectCostCategories` — zmiana kolejności (opcjonalnie, jak jednostki)
- Rozszerzenie `CreateProjectCost`, `UpdateProjectCost`, `CreateTrackedCost`, `UpdateTrackedCost` o `CategoryId?`
- Rozszerzenie web modeli kosztów o `CategoryId`, `CategoryName`
- Rozszerzenie `GetProjectDashboard` / `ProjectDashboardAssembler` o dane do wykresu kołowego (`CostByCategory[]`)

### API — Kontroler
- Endpointy w `ProjectController` lub dedykowany `ProjectCostCategoryController` (wzorzec jak `ProjectUnit`)

### Business / AI
- Rozszerzenie AI cost import / parse o sugestię kategorii
- Jeśli AI nie znajdzie pasującej kategorii — sugeruje nową (`suggestedCategory`)
- UI pokazuje modal/panel potwierdzenia tworzenia nowej kategorii (wzorzec `CostModal.tsx` + `isAiContractorCreateOpen`)

### UI — Ustawienia projektu
- Nowy komponent `CostCategoryManager` (wzorzec `UnitManager.tsx`, `CurrencySelector.tsx`)
- Sekcja w ustawieniach/parametrach projektu
- CRUD: dodaj, edytuj, usuń, reorder

### UI — Modale kosztów
- Pole select/combobox kategorii w `CostModal` (opcjonalne)
- Wspólne dla ProjectCost i TrackedCost (BaseCost fields)
- AI flow: sugestia kategorii + opcjonalne tworzenie nowej

### UI — Dashboard (Finanse)
- Nowy komponent `CostCategoryPieChart` na `FinanceTab`
- Dane z `ProjectDashboardWeb.costByCategory` (lub podobne)
- Segment „Bez kategorii" dla kosztów bez `CategoryId`
- Wartości netto/brutto (spójnie z resztą dashboardu)
- Dostępność: aria-label, test AXE

## Klasa bazowa domenowa (istniejąca)

```csharp
public abstract class BaseCost : DeletableEntity
{
    public Guid TrackerId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public decimal? Net { get; set; }
    public decimal? Gross { get; set; }
    public string? Contractor { get; set; }
    public DateTime? Date { get; set; }
    // NOWE: public Guid? CategoryId { get; set; }
}
```

## Wzorce referencyjne w kodzie

| Obszar | Plik |
|--------|------|
| Domyślne jednostki przy CreateProject | `CreateProjectCommandHandler.cs` |
| Zarządzanie jednostkami UI | `UnitManager.tsx`, `useProjectUnits` |
| AI kontrahent | `CostModal.tsx` (`isAiContractorCreateOpen`) |
| Dashboard Finanse | `FinanceTab.tsx` |
| Dashboard assembler | `ProjectDashboardAssembler.cs` |
| Ujednolicenie modali kosztów | `.opencode/features/unify-cost-modal.md` |

## Kryteria akceptacji

1. Przy tworzeniu projektu automatycznie powstaje 10 kategorii kosztów
2. Użytkownik może zarządzać kategoriami w ustawieniach projektu
3. Przy dodawaniu/edycji kosztu (ProjectCost i TrackedCost) można opcjonalnie wybrać kategorię
4. AI przy imporcie kosztu dobiera kategorię lub proponuje utworzenie nowej z informacją dla użytkownika
5. Na zakładce Finanse dashboardu widoczny jest wykres kołowy rozkładu kosztów wg kategorii
6. Koszty bez kategorii widoczne jako „Bez kategorii" na wykresie
7. Build API i UI bez błędów
8. Testy jednostkowe dla nowych handlerów CQRS
9. Test AXE dla dashboardu (jeśli dotyczy)

## Pytania do rozstrzygnięcia

1. Czy kategorie mają mieć pole `Color` do wykresu, czy generować kolory automatycznie?
2. Czy przy usuwaniu kategorii używanej przez koszty — blokować usunięcie czy odpinać koszty (CategoryId = null)?
3. Czy kategorie są per-projekt (rekomendowane, jak jednostki) czy per-tenant?
4. Czy wykres kołowy pokazuje netto, brutto, czy przełącznik jak w innych widgetach?
