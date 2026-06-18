# Feature: Wykresy na dashboardzie projektu

## Opis

Rozszerzenie dashboardu projektu (`/projects/{projectId}/dashboard`) o wizualizacje statystyk finansowych i czasowych. Dane pochodzą z istniejącego endpointu `GetProjectDashboardQuery` — agregacja po stronie klienta, **bez zmian API** w tej fazie.

## Problem

Overview pokazuje wyłącznie KPI i paski postępu. Użytkownik nie widzi na pierwszy rzut oka rozkładów budżetu, trendów kosztów, postępu harmonogramów ani osi czasu projektu. Brak biblioteki wykresów; `FinancialOverview` i `TimelineOverview` używają inline styles.

## Cel

1. Dodać wykresy finansowe (F1–F9) i czasowe (T1–T5, T7 opcjonalnie) na podstawie danych już zwracanych przez API.
2. Wydzielić dwie osobne sekcje: **FinanceSection** i **SchedulesSection** (nie jedna `ChartsSection`).
3. Wprowadzić globalny przełącznik netto/brutto w `DashboardCurrencyProvider`.
4. Użyć biblioteki **recharts** dla wykresów wymagających interakcji (serie czasowe, tooltips).
5. Zrefaktorować `FinancialOverview` / `TimelineOverview` — usunąć inline styles w tym samym PR.

## Decyzje użytkownika (zatwierdzone)

| # | Decyzja | Wartość |
|---|---------|---------|
| 1 | Zakres | F1–F9, T1–T5, T7 (opcjonalnie rozwijany); **poza zakresem**: F10, T6, T8 (faza 2 — wymaga API) |
| 2 | Umiejscowienie | Osobne sekcje **FinanceSection** i **SchedulesSection** |
| 3 | Gantt-lite (T4) | Tylko **etapy** harmonogramu (`stages`), bez pojedynczych prac |
| 4 | Net vs brutto | Tak — globalny toggle w `DashboardCurrencyProvider` |
| 5 | Biblioteka | **recharts** od razu (`package.json`) |
| 6 | Refaktor inline styles | W **tym samym PR** co wykresy |
| 7 | Limit F4 | Top 5 kosztorysów + link „Pokaż wszystkie" → zakładka Estimates |

## Zakres poza feature (faza 2 — API)

| ID | Wykres | Powód wyłączenia |
|----|--------|------------------|
| **F10** | Plan vs rzeczywisty wydatek (2 serie czasowe) | Brak danych planowanych wydatków w czasie w API |
| **T6** | Pełny Gantt (wszystkie prace) | Wymaga dedykowanego payloadu / optymalizacji API |
| **T8** | Heatmapa obciążenia zasobów | Brak danych zasobów w `ProjectDashboardWeb` |

## Źródła danych (bez zmian API)

| Pole dashboardu | Użycie |
|-----------------|--------|
| `financialSummary` | F1, F2, F3, F9 (agregat) |
| `timelineSummary` | T1, T3, T5 (agregat), T7 |
| `costEstimateSummaries[]` | F4, F9 |
| `scheduleSummaries[]` | F6, T2, T4, T5, T7 |
| `allCosts[]` | F5, F7, F8 |
| `projectAdditionalCosts` | F3 (kontekst zakładki Additional) |
| `referenceDate` | T3 (marker „dziś") |

## Architektura UI docelowa

```
DashboardHeader (+ toggle waluta netto/brutto)
┌─────────────────────┬─────────────────────┐
│ FinancialOverview   │ TimelineOverview    │  ← KPI (bez wykresów)
├─────────────────────┴─────────────────────┤
│ FinanceSection                          │
│  F1, F2 | F3, F6 | F4 (full) | F7, F8   │
│  F5 (full) | F9 (full)                  │
├─────────────────────────────────────────┤
│ SchedulesSection                        │
│  T1, T2 | T3 (full) | T4 (full)         │
│  T5 (full) | T7 (collapsible, optional) │
├─────────────────────────────────────────┤
│ DashboardTabs + wykresy kontekstowe   │
└─────────────────────────────────────────┘
```

### Co zostaje w Overview

| Panel | Zawartość (bez zmian funkcjonalnych) |
|-------|--------------------------------------|
| **FinancialOverview** | Badge statusu, 4 KPI (budżet, koszty, pozostało, koszty główne), pasek pokrycia budżetu, podział budżet kosztorysów / rezerwa, przycisk edycji rezerwy, licznik kosztorysów |
| **TimelineOverview** | Badge statusu, 4 KPI (postęp, opóźnione, w toku, czas projektu), pasek postępu, badge'e statusów prac, licznik harmonogramów |

Wykresy **nie** trafiają do paneli Overview — przeniesione do dedykowanych sekcji poniżej.

### FinanceSection — layout grid

| Wiersz | Siatka | Wykresy | Uwagi |
|--------|--------|---------|-------|
| 1 | 2 kolumny (`1fr 1fr`) | **F1** \| **F2** | Donut + donut/stacked bar |
| 2 | 2 kolumny | **F3** \| **F6** | Źródła kosztów + koszty per harmonogram |
| 3 | pełna szerokość | **F4** | Top 5 kosztorysów; link do zakładki Estimates |
| 4 | 2 kolumny | **F7** \| **F8** | Źródło (`sourceType`) + top 5 wykonawców |
| 5 | pełna szerokość | **F5** | Kumulacja kosztów w czasie (recharts AreaChart) |
| 6 | pełna szerokość | **F9** | Odchylenia per kosztorys (pozytywne/negatywne kolory) |

**Responsywność:** `≥ md` — 2 kolumny; `< md` — 1 kolumna (stack). Klasa CSS: `dashboard-finance-grid`.

### SchedulesSection — layout grid

| Wiersz | Siatka | Wykresy | Uwagi |
|--------|--------|---------|-------|
| 1 | 2 kolumny | **T1** \| **T2** | Rozkład statusów + postęp per harmonogram |
| 2 | pełna szerokość | **T3** | Oś czasu projektu (`earliestStart` → `latestEnd`, marker `referenceDate`) |
| 3 | pełna szerokość | **T4** | Gantt-lite **tylko etapy** (wszystkie harmonogramy, płaskie `stages`) |
| 4 | pełna szerokość | **T5** | Opóźnienia per etap (etapy z `delayDays > 0` lub status Delayed) |
| 5 | collapsible (domyślnie zwinięty) | **T7** | S-curve postępu — opcjonalny, rozwijany |

**Responsywność:** jak FinanceSection. Klasa CSS: `dashboard-schedules-grid`.

### Wykresy kontekstowe w zakładkach

| Zakładka | Wykresy | Zachowanie |
|----------|---------|------------|
| **Estimates** | F4 (pełna lista), F9 (szczegółowy) | F4 bez limitu top 5; sortowanie po odchyleniu/kosztach |
| **Schedules** | T2 (per harmonogram w `ScheduleBlock`), T4 (per harmonogram), T5 (per harmonogram) | Zastąpić/refaktorować istniejący `MiniGantt` → wersja tylko etapy |
| **All Costs** | F5 (z filtrem), F7, F8 | F5 z tooltipem per miesiąc; F8 top 10 wykonawców |
| **Additional** | F3 (wariant rezerwa vs koszty dodatkowe) | Donut: `projectReserveBudget` vs `additionalCosts` |

## Katalog wykresów (finalne przypisanie)

### Finansowe

| ID | Nazwa | Typ | Źródło danych | Sekcja główna | Zakładka kontekstowa |
|----|-------|-----|---------------|---------------|----------------------|
| **F1** | Pokrycie budżetu | Donut (recharts Pie) | `financialSummary.coveredPercent`, `isBudgetExceeded` | FinanceSection r1 | — |
| **F2** | Skład budżetu | Donut / stacked bar | `estimateBudgetNet/Gross`, `projectReserveBudgetNet/Gross` | FinanceSection r1 | — |
| **F3** | Źródła kosztów (powiązane vs dodatkowe) | Donut | `linkedCostsNet/Gross`, `additionalCostsNet/Gross` | FinanceSection r2 | Additional (wariant rezerwa) |
| **F4** | Budżet vs koszty per kosztorys | Horizontal BarChart | `costEstimateSummaries[]` — top 5 + link | FinanceSection r3 | Estimates (wszystkie) |
| **F5** | Kumulacja kosztów w czasie | AreaChart | `allCosts[]` — agregacja miesięczna po `date` (fallback `createdAt`) | FinanceSection r5 | All Costs |
| **F6** | Koszty per harmonogram | Horizontal BarChart | `scheduleSummaries[].totalCostsNet/Gross` | FinanceSection r2 | Schedules (w `ScheduleBlock`) |
| **F7** | Koszty wg typu źródła | Donut / BarChart | `allCosts[]` — grupowanie po `sourceType` | FinanceSection r4 | All Costs |
| **F8** | Top wykonawcy | Horizontal BarChart | `allCosts[]` — grupowanie po `contractorName`, top 5/10 | FinanceSection r4 | All Costs (top 10) |
| **F9** | Odchylenia budżetowe per kosztorys | BarChart (±) | `costEstimateSummaries[].deviationNet/Gross`, `deviationPercent` | FinanceSection r6 | Estimates |
| ~~F10~~ | ~~Plan vs rzeczywisty~~ | — | — | **Faza 2** | — |

### Czasowe

| ID | Nazwa | Typ | Źródło danych | Sekcja główna | Zakładka kontekstowa |
|----|-------|-----|---------------|---------------|----------------------|
| **T1** | Rozkład statusów prac | Donut | `timelineSummary` — completed/inProgress/delayed/notStarted/completedLate | SchedulesSection r1 | — |
| **T2** | Postęp per harmonogram | Horizontal BarChart (% postępu) | `scheduleSummaries[].timeline.progressPercent` | SchedulesSection r1 | Schedules (`ScheduleBlock`) |
| **T3** | Oś czasu projektu | Timeline span (custom + recharts ReferenceLine) | `earliestStart`, `latestEnd`, `referenceDate`, `delayDays` | SchedulesSection r2 | — |
| **T4** | Gantt-lite etapów | Custom horizontal bars (**tylko stages**) | `scheduleSummaries[].stages[]` — `timelinePlannedStart/End`, `timelineStatus` | SchedulesSection r3 | Schedules (per harmonogram) |
| **T5** | Opóźnienia per etap | BarChart | Spłaszczone `stages[]` z `timeline.delayDays > 0` | SchedulesSection r4 | Schedules (per harmonogram) |
| ~~T6~~ | ~~Pełny Gantt (prace)~~ | — | — | **Faza 2** | — |
| **T7** | S-curve postępu | LineChart (recharts) | Agregacja kumulatywnego postępu z etapów/prac w czasie | SchedulesSection r5 (collapsible) | — |
| ~~T8~~ | ~~Heatmapa obciążenia~~ | — | — | **Faza 2** | — |

## Rozszerzenie DashboardCurrencyProvider

```typescript
export type DashboardAmountMode = 'net' | 'gross';

export interface DashboardDisplayContext {
  currencySymbol: string;
  amountMode: DashboardAmountMode;
  setAmountMode: (mode: DashboardAmountMode) => void;
  /** Zwraca net lub gross z pary — używane przez wykresy i formatowanie */
  pickAmount: (net: number | null, gross: number | null) => number | null;
}
```

- Toggle **Netto / Brutto** w `DashboardHeader` (segmented control Chakra).
- Domyślnie: **netto**.
- Wykresy pokazują jedną serię wg `amountMode`.
- `NetGrossAmount` — nadal obie wartości; wyróżnienie aktywnego trybu (grubsza czcionka / kolor akcentu).
- Persystencja opcjonalna: `sessionStorage` klucz `dashboard-amount-mode` (nice-to-have w fix-01).

## Infrastruktura techniczna

### Nowe zależności

```bash
npm install recharts
```

### Nowe pliki (planowane)

```
features/dashboard/
├── context/DashboardCurrencyContext.tsx     # rozszerzony provider
├── components/
│   ├── FinanceSection.tsx
│   ├── SchedulesSection.tsx
│   ├── charts/
│   │   ├── ChartCard.tsx                    # wrapper: tytuł, empty, loading
│   │   ├── BudgetCoverageDonut.tsx          # F1
│   │   ├── BudgetCompositionChart.tsx       # F2
│   │   ├── CostSourcesDonut.tsx             # F3
│   │   ├── EstimateBudgetBarChart.tsx       # F4, F9
│   │   ├── CostTimeSeriesChart.tsx          # F5
│   │   ├── ScheduleCostsBarChart.tsx        # F6
│   │   ├── CostSourceTypeChart.tsx          # F7
│   │   ├── TopContractorsChart.tsx          # F8
│   │   ├── WorkStatusDonut.tsx              # T1
│   │   ├── ScheduleProgressBarChart.tsx     # T2
│   │   ├── ProjectTimelineSpan.tsx          # T3
│   │   ├── StageGanttLite.tsx               # T4 (zastępuje logikę workItems w MiniGantt)
│   │   ├── StageDelaysChart.tsx             # T5
│   │   └── ProgressSCurveChart.tsx          # T7
│   └── shared/NetGrossToggle.tsx
├── hooks/
│   ├── useCostTimeSeries.ts                 # F5
│   ├── useChartAmount.ts                    # pickAmount helper
│   └── useFlattenedStages.ts                # T4, T5
├── utils/
│   └── chartAggregations.ts                 # grupowania, sortowania, top-N
└── dashboard.css                            # grid sekcji wykresów
```

### Konwencje

- Kolory z tokenów Chakra (`primary`, `level1`, `orange`, `red`, `neutral`) — mapowanie w `chartTheme.ts`.
- Każdy wykres: stan empty (`EmptyState`), aria-label, `role="img"` lub opis dla screen readerów.
- Recharts: `ResponsiveContainer` width="100%" height={240–320px}.
- Brak inline styles — Chakra + CSS classes.

## Plan implementacji UI

### Zależności wstępne

1. `npm install recharts` w `ProjectDataManagementUI`
2. Rozszerzenie `DashboardCurrencyProvider` + toggle w headerze
3. Utils agregacji (`chartAggregations.ts`) — przed komponentami wykresów

### Prompty UI

| # | Plik promptu | Zakres |
|---|--------------|--------|
| 1 | `dashboard-charts-ui-fix-01.md` | `recharts` install, rozszerzony `DashboardCurrencyContext`, `NetGrossToggle` w `DashboardHeader`, `chartAggregations.ts`, `ChartCard`, `chartTheme.ts`, CSS grid klas |
| 2 | `dashboard-charts-ui-fix-02.md` | Refaktor `FinancialOverview` + `TimelineOverview` (Chakra, bez inline styles); szkielety `FinanceSection` + `SchedulesSection`; integracja w `ProjectDashboard` |
| 3 | `dashboard-charts-ui-fix-03.md` | Wykresy finansowe F1–F5 + `FinanceSection` layout |
| 4 | `dashboard-charts-ui-fix-04.md` | Wykresy finansowe F6–F9 + wykresy kontekstowe zakładek Estimates, All Costs, Additional |
| 5 | `dashboard-charts-ui-fix-05.md` | Wykresy czasowe T1–T5, `StageGanttLite` (tylko etapy), `SchedulesSection` layout |
| 6 | `dashboard-charts-ui-fix-06.md` | T7 S-curve (collapsible), wykresy kontekstowe Schedules tab, refaktor/usunięcie starego `MiniGantt` (workItems), testy axe, build |

### Kolejność wykonania

```
fix-01 (infra)
  ↓
fix-02 (overview refactor + section shells)
  ↓
fix-03 (F1–F5) ──┐
fix-05 (T1–T5)  ─┤  równolegle po fix-02
  ↓               ↓
fix-04 (F6–F9 + tabs finance)
  ↓
fix-06 (T7 + tabs schedules + polish)
```

**Uwaga:** fix-03 i fix-05 mogą być wykonywane równolegle po fix-02. fix-04 i fix-06 wymagają gotowych utils i `ChartCard` z fix-01.

### Typ zmiany

| Aspekt | Wartość |
|--------|---------|
| Warstwa | **UI-only** |
| API | Brak zmian w fazie 1 |
| Testy | `npm run test:axe` dla nowych komponentów wykresów; `npm run build` |

## Kryteria akceptacji

### Funkcjonalne

1. Na dashboardzie widoczne sekcje **FinanceSection** i **SchedulesSection** pod Overview.
2. Wszystkie wykresy F1–F9 i T1–T5 renderują się z danych `GetProjectDashboardQuery` bez dodatkowych wywołań API.
3. Toggle netto/brutto w headerze zmienia wartości na wszystkich wykresach finansowych.
4. F4 na sekcji głównej pokazuje max 5 kosztorysów + link „Pokaż wszystkie" przełączający na zakładkę Estimates.
5. T4 pokazuje **wyłącznie etapy** harmonogramu — brak wierszy pojedynczych prac.
6. T7 jest domyślnie zwinięty (Collapsible); rozwinięcie pokazuje S-curve.
7. Wykresy kontekstowe w zakładkach zgodnie z tabelą powyżej.

### Techniczne

1. `recharts` dodany do `package.json`; `npm run build` przechodzi.
2. `FinancialOverview` i `TimelineOverview` — zero inline `style={{}}`; Chakra props + CSS.
3. Brak `any` w TypeScript; explicit types dla danych wykresów.
4. Wykresy mają stany empty (brak danych) z komunikatem po polsku.
5. Testy axe dla `FinanceSection`, `SchedulesSection` i min. 2 komponentów recharts — brak naruszeń WCAG AA.

### Poza zakresem (nie blokuje akceptacji)

- F10, T6, T8 — dokumentowane jako faza 2.
- Persystencja toggle netto/brutto między sesjami (opcjonalne).
- Eksport wykresów do PNG/PDF.

## Następne kroki po zatwierdzeniu planu implementacji

1. Audyt UI (`ui-audit-agent`) — opcjonalnie, lightweight (inline styles, struktura dashboardu).
2. Generacja plików `.opencode/subagents/rules/dashboard-charts-ui-fix-0N.md`.
3. Implementacja przez `ui-refactor-agent` w kolejności fix-01 → fix-06.
