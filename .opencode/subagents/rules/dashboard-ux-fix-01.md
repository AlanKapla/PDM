# Refactor — dashboard-ux-fix-01

**Cel:** Redukcja duplikacji KPI i hierarchia informacji.  
**Priorytet:** P0-1, P0-5, P1-1, P1-4

---

## Krok 1 — `DashboardHeader.tsx`

- Usuń kartę „Koszty dodatkowe” (szczegół przeniesiony do Finanse/Overview).
- Zostaw 5 kart: Budżet łączny, Koszty łączne, Postęp prac, Status finansowy, Status harmonogramu.
- Ustaw `columns={{ base: 1, sm: 2, lg: 5 }}` (bez warunku `showAdditional`).

## Krok 2 — `FinancialOverview.tsx`

- Usuń duplikaty KPI już w headerze: **Budżet łączny**, **Koszty łączne**.
- Zostaw: Pozostało do wydania, Koszty dodatkowe (zmień etykietę z „Koszty główne”), pasek pokrycia, Budżet kosztorysów, Budżet główny, przycisk edycji, licznik kosztorysów.
- Zmień `neutral.400` → `neutral.600` na tekstach pomocniczych (linie z pokryciem budżetu i licznikiem kosztorysów).

## Krok 3 — `TimelineOverview.tsx`

- Usuń `KpiCard` „Postęp ogólny” (duplikat headera).
- Zostaw: Opóźnione, W toku, Czas projektu + pasek postępu + badge statusów + licznik harmonogramów.
- Zmień `neutral.400` → `neutral.600` na tekstach pomocniczych.

## Krok 4 — `FinanceTab.tsx`

- Zredukuj siatkę KPI z 8 do 4: Budżet łączny, Koszty łączne, Pozostało do wydania, Pokrycie budżetu.
- Usuń KPI: Koszty główne/dodatkowe, Kosztorysów, Przekroczonych, Liczba kosztów (dostępne w sekcji Kosztorysy / Koszty).
- Zmień `columns={{ base: 2, md: 4, lg: 4 }}`.
- Usuń sekcję „Analiza” (wykresy `CostTimeSeriesChart`, `CostSourceTypeChart`, `TopContractorsChart`, `ScheduleCostsBarChart`) — pełna analityka zostaje na zakładce Koszty.
- Usuń nieużywane importy wykresów i stałą `ANALYTICS_MIN_COST_COUNT` jeśli niepotrzebna.
- Usuń lokalny alert (`computeDashboardAlert`) — przeniesiony w fix-02.

## Krok 5 — `GeneralChartsSection.tsx`

- Usuń wykresy duplikowane w `SchedulesSection`: `WorkStatusDonut`, `ScheduleProgressBarChart`, `ProjectTimelineSpan` (jeśli są).
- Zostaw wykresy unikalne dla Ogólnego (budżet, koszty, coverage itp.).

## Krok 6 — `EstimateBudgetBarChart.tsx`

- Popraw tekst linku z „zakładce Kosztorysy” na „zakładce Finanse”.

## Krok 7 — Build

```powershell
cd 01-Applications/ProjectDataManagementUI
npm run build
```

Napraw błędy TypeScript/ESLint przed zakończeniem.

---

## Kryterium done

- Budżet/koszty widoczne max 2× (header + jedna zakładka szczegółowa).
- FinanceTab skupia się na kosztorysach i ostatnich kosztach.
- Etykieta „Koszty dodatkowe” spójna w `FinancialOverview`.
