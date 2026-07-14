# Refactor — dashboard-ux-fix-03

**Cel:** Mobile, dostępność (a11y), wizualna spójność KPI, testy AXE.  
**Priorytet:** P0-3, P1-3, P1-7, P2-2, P2-3

**Wymaga:** Wykonaj po `dashboard-ux-fix-01.md` i `dashboard-ux-fix-02.md`.

---

## Krok 1 — `CostsTab.tsx` — keyboard a11y na wierszach tabeli

Wzorzec jak `EstimateProgressRow` w `EstimateProgressList.tsx`:
- Wydziel `CostTableRow` jako osobny komponent w tym samym pliku.
- Na `<Tr>` dodaj: `tabIndex={0}`, `role="button"`, `aria-label={`Otwórz koszt ${cost.name}`}`, `onKeyDown` (Enter + Space).
- Zachowaj `cursor="pointer"`, `_hover`, `onClick`.
- Przycisk usuwania: `e.stopPropagation()` (już jest).
- Zmień `neutral.400` na numerze faktury → `neutral.600`.

## Krok 2 — `dashboard.css` — responsywny grid KPI

Dodaj klasę:

```css
.dashboard-kpi-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 140px), 1fr));
  gap: 12px;
  width: 100%;
}
```

Dodaj w `@media (max-width: 640px)`:
```css
.dashboard-kpi-grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}
```

## Krok 3 — Zastosuj `dashboard-kpi-grid` zamiast sztywnych SimpleGrid

W plikach:
- `FinanceTab.tsx` — zamień `SimpleGrid columns={{ base: 2, md: 4, lg: 4 }}` na `<Box className="dashboard-kpi-grid" mb={6}>`
- `CostsTab.tsx` — zamień `SimpleGrid columns={{ base: 2, md: 5 }}` na `<Box className="dashboard-kpi-grid" mb={6}>`
- `SchedulesTab.tsx` — zamień `SimpleGrid columns={{ base: 2, md: 4, lg: 8 }}` na `<Box className="dashboard-kpi-grid" mb={6}>`

Usuń nieużywane importy `SimpleGrid` jeśli nie są już potrzebne.

## Krok 4 — `KpiCard.tsx` — kontrast i spójność

- Wariant `small`: `smallLabelColor` gdy brak colorScheme zmień z `neutral.500` na `neutral.600`.
- Tekst `sub` w obu wariantach: `neutral.400` → `neutral.600`.

## Krok 5 — `DashboardMainTabs.tsx` — mobile

- Importuj `useBreakpointValue` z Chakra.
- `const showTabBadges = useBreakpointValue({ base: false, md: true }) ?? true;`
- Renderuj `<Badge>` tylko gdy `showTabBadges && count != null`.

## Krok 6 — Testy AXE `dashboard-main.axe.test.tsx`

Utwórz `src/features/dashboard/__tests__/dashboard-main.axe.test.tsx`:

Wzorzec jak `dashboard-charts.axe.test.tsx` — użyj `renderWithChakra` + `DashboardCurrencyProvider`.

Minimalne mocki danych (inline w teście, bez osobnego pliku):

```typescript
// Minimalne dane dla DashboardHeader
const mockFinancialSummary = { ... };
const mockTimelineSummary = { ... };
const mockData: ProjectDashboardWeb = { ... };
```

Testy (każdy z `axe(container)` + `toHaveNoViolations`):
1. `DashboardHeader_brakNaruszen` — render z mock `data`
2. `DashboardMainTabs_brakNaruszen` — render z 4 TabPanel children (puste Box)
3. `FinanceTab_brakNaruszen` — mock data z pustą listą kosztorysów
4. `CostsTab_brakNaruszen_pustaLista` — costs=[]
5. `CostsTab_brakNaruszen_zKosztami` — 1-2 mock TrackedCostWeb

Mock `useProjectPermissions` w FinanceTab:
```typescript
vi.mock('../../../../hooks/useProjectPermissions', () => ({
  useProjectPermissions: () => ({ canViewEstimates: true }),
}));
```

Mock `useNavigate` w FinanceTab:
```typescript
vi.mock('react-router-dom', () => ({
  useNavigate: () => vi.fn(),
}));
```

## Krok 7 — Build i testy

```powershell
cd 01-Applications/ProjectDataManagementUI
npm run build
npm run test:run -- src/features/dashboard/__tests__/dashboard-main.axe.test.tsx
```

Napraw błędy przed zakończeniem.

---

## Kryterium done

- Wiersze `CostsTab` dostępne z klawiatury.
- KPI grid responsywny na 375px.
- Testy AXE zielone dla 5 przypadków.
- Build bez błędów.
