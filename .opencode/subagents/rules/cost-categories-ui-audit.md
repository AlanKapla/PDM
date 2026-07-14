# Raport audytu UI: cost-categories

> Data: 2026-07-08  
> Feature spec: `.opencode/features/cost-categories.md`  
> Plan: `.opencode/subagents/rules/cost-categories-plan.md`  
> Raport API: `.opencode/subagents/rules/cost-categories-api-audit.md`  
> Status: zatwierdzony przez użytkownika (kontynuacja po audycie API)

---

## Executive summary

Warstwa UI **nie zawiera** żadnej implementacji kategorii kosztów. Brak `CategoryId` w typach, brak endpointów w `projectApi`, brak komponentów zarządzania/pickera/wykresu.

Wzorce są gotowe i spójne w repozytorium — implementacja to głównie **kopiowanie wzorców** z jednostek miary, kontrahenta AI i wykresów donut.

| Metryka | Wartość |
|---------|---------|
| Nowych komponentów | 4 |
| Zmodyfikowanych komponentów | ~12 |
| Nowych hooków | 5 mutacji + 1 query (1 plik) |
| Nowych typów | 4 nowe + 5 rozszerzonych |
| Pytania otwarte | 3 (2 opcjonalne UX) |

---

## Stan obecny

### Brak implementacji kategorii

- `projectApi.ts` — brak metod `/cost-categories` (wzorzec: units L616–674)
- `project.types.ts` — brak `ProjectCostCategoryDto`
- `projectDashboard.types.ts` — brak `CostByCategoryWeb`, `costByCategory`
- `ai.types.ts` / `ParsedCostDto` — brak pól kategorii
- `CostModal.tsx` — `CostFormState` bez `categoryId`; create/update nie wysyła kategorii
- `FinanceTab.tsx` — KPI + kosztorysy + `RecentCostsList`; **brak wykresów**
- `ProjectParameters.tsx` — tylko `CurrencySelector` + `UnitManager`

### Komponenty istniejące (wzorce)

| Komponent | Rola | Ścieżka |
|-----------|------|---------|
| `UnitManager` | CRUD jednostek w parametrach | `components/ProjectParameters/UnitManager.tsx` |
| `useProjectUnits` | Hooki React Query | `hooks/useProjectUnits.ts` |
| `CostModal` | Ujednolicony modal kosztów + AI kontrahent | `features/dashboard/components/CostModal.tsx` |
| `ContractorQuickAddModal` | Quick-add kontrahenta z AI | `components/ContractorQuickAddModal.tsx` |
| `ContractorPicker` | Select kontrahenta | `components/ContractorPicker.tsx` |
| `CostSourcesDonut` | Wykres donut (źródła kosztów) | `features/dashboard/components/charts/CostSourcesDonut.tsx` |
| `ChartCard` | Wrapper wykresu z `role="img"`, `aria-label` | `features/dashboard/components/shared/ChartCard.tsx` |
| `chartAggregations` | `pickCostAmount` → **netto** | `features/dashboard/utils/chartAggregations.ts` |

### Legacy — poza scope MVP

| Komponent | Użycie | Decyzja |
|-----------|--------|---------|
| `CostFormModal` | Tylko `ProjectBudgetDashboard.tsx` | **Poza scope** — priorytet `CostModal` |
| `FinanceSection` | Brak importów w aktywnym flow | **Nie używać** — wykres na `FinanceTab` |

### FinanceTab — layout docelowy

Obecny układ:
1. KPI grid (4 karty)
2. Sekcja Kosztorysy (`EstimateProgressList`)
3. Sekcja Ostatnie koszty (`RecentCostsList`)

Proponowany układ po zmianach:
1. KPI grid
2. **Nowa sekcja: Wykres kołowy kategorii** (`CostCategoryPieChart`)
3. Kosztorysy
4. Ostatnie koszty

Wzorzec layoutu: `CostsTab` / `GeneralChartsSection` lub `.dashboard-finance-grid` z `dashboard.css`.

---

## Wzorce referencyjne

### API client + hooki
```
projectApi.getProjectUnits / addProjectUnit / ...  → mirror dla cost-categories
hooks/useProjectUnits.ts                           → useProjectCostCategories.ts
```

### Zarządzanie w parametrach
```
components/ProjectParameters/UnitManager.tsx
pages/ProjectParameters.tsx (sekcja po UnitManager)
```

### Modal kosztów + AI
```
CostModal.tsx:
  - isAiContractorCreateOpen (L~134)
  - AI badge + przycisk tworzenia (L~498)
  - ContractorQuickAddModal (L~765–777)
→ Analogicznie: isAiCategoryCreateOpen, CostCategoryQuickAddModal
```

### Wykres donut
```
charts/CostSourcesDonut.tsx  — Recharts PieChart, CHART_PALETTE
shared/ChartCard.tsx         — aria-label, empty state
chartAggregations.ts         — pickCostAmount (netto)
```

### FormData naming (istniejący konwencja)
- Project cost API: **PascalCase** (`CategoryId`)
- Tracked cost API: **camelCase** (`categoryId`)

---

## Pliki do utworzenia

| # | Plik | Opis |
|---|------|------|
| 1 | `src/hooks/useProjectCostCategories.ts` | `useProjectCostCategories`, `useAdd/Update/Delete/ReorderProjectCostCategory` |
| 2 | `src/components/ProjectParameters/CostCategoryManager.tsx` | CRUD kategorii (wzorzec `UnitManager`; Code optional, Color opcjonalny) |
| 3 | `src/components/CostCategoryPicker.tsx` | Select/combobox kategorii (opcjonalny, clearable) |
| 4 | `src/components/CostCategoryQuickAddModal.tsx` | Quick-add z AI (wzorzec `ContractorQuickAddModal`) |
| 5 | `src/features/dashboard/components/charts/CostCategoryPieChart.tsx` | Donut rozkładu kosztów wg kategorii + segment „Bez kategorii" |

---

## Pliki do modyfikacji

| Plik | Zakres zmian |
|------|--------------|
| `src/api/projectApi.ts` | 5 metod CRUD `/cost-categories` + typ `ProjectCostCategoryDto` |
| `src/types/project.types.ts` | `categoryId`, `categoryName` na kosztach; DTO kategorii |
| `src/types/ai.types.ts` | `categoryId`, `categoryFound`, `suggestedCategory` |
| `src/features/dashboard/types/projectDashboard.types.ts` | `CostByCategoryWeb`, `costByCategory[]` |
| `src/hooks/queries/useProjectCostMutations.ts` | `categoryId` w payload create/update |
| `src/pages/ProjectParameters.tsx` | Sekcja `CostCategoryManager` |
| `src/features/dashboard/components/CostModal.tsx` | `CostCategoryPicker`, AI flow kategorii, FormData |
| `src/features/dashboard/components/tabs/FinanceTab.tsx` | Integracja `CostCategoryPieChart` |
| `src/features/dashboard/utils/chartAggregations.ts` | `groupCostsByCategory` (helper/testy; produkcja z API) |
| `src/hooks/queries/index.ts` | Export nowych hooków |
| `src/features/dashboard/__tests__/dashboard-main.axe.test.tsx` | Mock `costByCategory` |
| `src/features/dashboard/__tests__/dashboard-charts.axe.test.tsx` | Test `CostCategoryPieChart` |

### Poza scope MVP (faza 2)
- `CostsTab.tsx` — kolumna kategorii
- `RecentCostsList.tsx` — badge kategorii
- `CostFormModal.tsx` / `ProjectBudgetDashboard.tsx` — legacy sync

---

## UX / dostępność

| Kategoria | Status | Rekomendacja |
|-----------|--------|--------------|
| Kontrast | ⚠ | Empty state wykresu: `neutral.600+`, nie `neutral.400` |
| ARIA | ✓ wzorzec | `ChartCard` z `role="img"` + opisowy `aria-label` |
| Form labels | ✓ | `CostCategoryPicker` — `FormLabel` + `aria-label` na Select |
| Klawiatura | ✓ | `AppModal`, `DeleteAlertDialog` — istniejące wzorce |
| Testy AXE | ✗ brak | Dodać w `ui-fix-06` |
| Delete kategorii | — | Komunikat: „Koszty z tą kategorią trafią do «Bez kategorii»" |

### Kolor kategorii w managerze
- Presety z `CHART_PALETTE` (swatches)
- Opcjonalny custom color input (wzorzec: `WorkScheduleFormModal`)
- Fallback na wykresie gdy `Color` null

---

## Ryzyka

| Ryzyko | Poziom | Mitygacja |
|--------|--------|-----------|
| Brak całego slice UI | Krytyczne | ui-fix-01 jako pierwszy krok UI |
| CostModal — 7+ miejsc użycia | Wysokie | Jedna zmiana w `CostModal`, test manualny wszystkich entry points |
| Niespójność PascalCase/camelCase | Wysokie | Trzymać się istniejącej konwencji per endpoint |
| FinanceTab bez wykresów dziś | Normalne | Nowa sekcja — nie psuje istniejącego layoutu |
| Stale dashboard po edycji kategorii | Normalne | Invalidacja `projectDashboard` + `projectCostCategories` |
| CostFormModal legacy | Normalne | Poza scope — dokumentować w summary |

---

## Znaleziska wg priorytetu

### Krytyczne
1. Brak typów, API client, hooków dla kategorii
2. Brak pola kategorii w `CostModal` (główny flow kosztów)
3. Brak wykresu na `FinanceTab`

### Wysokie
4. `CostCategoryManager` w `ProjectParameters` (Code optional, Name required)
5. AI flow — mirror kontrahenta (`CostCategoryQuickAddModal`)
6. Segment „Bez kategorii" na wykresie (`categoryId === null`)
7. Delete UX — komunikat o odpinaniu kosztów
8. `CostFormModal` — tylko legacy, poza MVP

### Normalne
9. Testy AXE wykresu
10. Kolumna kategorii w listach — faza 2
11. `groupCostsByCategory` helper w aggregations
12. Invalidacja cache po quick-add z AI

---

## Prompty implementacyjne (ui-fix-01..06)

| # | Plik | Zakres |
|---|------|--------|
| ui-fix-01 | `cost-categories-ui-fix-01.md` | Typy TS, `projectApi` (5 metod), `useProjectCostCategories`, rozszerzenie typów kosztów |
| ui-fix-02 | `cost-categories-ui-fix-02.md` | `CostCategoryManager` + integracja w `ProjectParameters` |
| ui-fix-03 | `cost-categories-ui-fix-03.md` | `CostCategoryPicker` + integracja w `CostModal` (create/edit) |
| ui-fix-04 | `cost-categories-ui-fix-04.md` | AI flow: badge, alert, `CostCategoryQuickAddModal` |
| ui-fix-05 | `cost-categories-ui-fix-05.md` | `CostCategoryPieChart` + `FinanceTab` + typy dashboard |
| ui-fix-06 | `cost-categories-ui-fix-06.md` | Testy AXE + testy hooków jeśli wymagane |

### Kolejność wykonania (zależności API)

```
api-fix-01..03 → ui-fix-01
api-fix-05..07 → ui-fix-03 → ui-fix-04
api-fix-08      → ui-fix-05
                → ui-fix-02 (równolegle po ui-fix-01)
                → ui-fix-06
```

---

## Pytania otwarte — rekomendacje

### 1. Kategoria w listach kosztów (`CostsTab`, `RecentCostsList`)?
**Rekomendacja: Faza 2** — MVP: picker w modalu + wykres na Finanse.

### 2. Picker koloru w managerze — presety czy presety + custom?
**Rekomendacja: Oba** — swatches z `CHART_PALETTE` + opcjonalny `type="color"` (jak harmonogram).

### 3. Invalidacja dashboard po quick-add kategorii z AI?
**Rekomendacja:** Minimum invalidacja `projectCostCategories`; opcjonalnie `projectDashboard` jeśli wykres już widoczny.

---

## Następny krok

Po zatwierdzeniu raportu UI → **generowanie promptów implementacyjnych** (api-fix-01..09, ui-fix-01..06) i zatwierdzenie planu implementacji.
