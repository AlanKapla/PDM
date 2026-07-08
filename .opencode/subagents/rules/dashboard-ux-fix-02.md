# Refactor — dashboard-ux-fix-02

**Cel:** Alerty globalne, spójne empty states, error recovery.  
**Priorytet:** P0-2, P0-4, P1-5, P1-6

**Wymaga:** Wykonaj po `dashboard-ux-fix-01.md`.

---

## Krok 1 — `ProjectDashboard.tsx`

- Importuj `computeDashboardAlert` z `../utils/dashboardAlert`.
- Po `DashboardHeader`, przed `DashboardMainTabs`, renderuj globalny `Alert` gdy `computeDashboardAlert(data)` zwraca wartość.
- W stanie błędu dodaj `Button` „Spróbuj ponownie” wywołujący `refetch`.

## Krok 2 — `FinanceTab.tsx`

- Usuń lokalny blok alertu (jeśli jeszcze istnieje po fix-01).
- Sekcja „Ostatnie koszty”: renderuj tylko gdy `costs.length > 0` (ukryj całą sekcję gdy brak kosztów).

## Krok 3 — `RecentCostsList.tsx`

- Zamiast `return null` przy pustej liście, zwróć komponent z tekstem empty state:
  ```tsx
  <Text fontSize="sm" color="neutral.600" fontStyle="italic" p={3}>
    Brak ostatnich kosztów.
  </Text>
  ```
- (FinanceTab ukrywa sekcję gdy pusto — ten fallback na wypadek użycia gdzie indziej.)

## Krok 4 — `EstimateProgressList.tsx`

- Zmień kolor empty message z `neutral.500` na `neutral.600`.

## Krok 5 — `SchedulesTab.tsx`

- Znajdź empty state (italic text) i zmień `neutral.500`/`neutral.400` → `neutral.600`.

## Krok 6 — `CostsTab.tsx`

- Znajdź empty state i ujednolić kolor tekstu na `neutral.600`.

## Krok 7 — `DashboardMainTabs.tsx`

- Dodaj `aria-hidden="true"` na `<Icon as={icon} />`.

## Krok 8 — Build

```powershell
cd 01-Applications/ProjectDataManagementUI
npm run build
```

---

## Kryterium done

- Alert widoczny na każdej zakładce (nad tabs).
- Brak wiszących nagłówków bez treści.
- Error state z przyciskiem retry.
