# UI Audit — feature: dashboard-ux

Data audytu: 2026-07-07  
Audytor: ui-audit-agent

---

## 1. Executive summary — top 5 problemów UX

| # | Problem | Wpływ |
|---|---------|-------|
| **1** | Potrójna duplikacja KPI finansowych i postępu — `DashboardHeader` → `FinancialOverview` / `TimelineOverview` → `FinanceTab` | Przeciążenie poznawcze |
| **2** | Duplikacja wykresów między zakładkami Ogólne/Harmonogramy i Finanse/Koszty | Szum informacyjny |
| **3** | Alert krytyczny tylko na zakładce Finanse, domyślna to Ogólne | Użytkownik nie widzi ostrzeżeń |
| **4** | Niespójne puste stany — `RecentCostsList` zwraca `null` | Martwe sekcje |
| **5** | Mobile + a11y — 8 KPI na mobile, brak keyboard na wierszach `CostsTab` | Słaba czytelność, WCAG |

---

## 2. BLOK 1 — Stan obecny UI

| Komponent | Lokalizacja | Opis |
|-----------|------------|------|
| `ProjectDashboard` | `components/ProjectDashboard.tsx` | Orkiestrator: fetch, loading/error, header, tabs |
| `DashboardHeader` | `components/DashboardHeader.tsx` | 5–6 kart KPI globalnych |
| `DashboardMainTabs` | `components/DashboardMainTabs.tsx` | 4 zakładki z badge count |
| `GeneralTab` | `tabs/GeneralTab.tsx` | Overview + wykresy |
| `FinanceTab` | `tabs/FinanceTab.tsx` | Alert, 8 KPI, kosztorysy, analityka |
| `FinancialOverview` | `components/FinancialOverview.tsx` | 6 KPI + pasek pokrycia |
| `TimelineOverview` | `components/TimelineOverview.tsx` | 4 KPI + pasek postępu |
| `RecentCostsList` | `components/RecentCostsList.tsx` | Max 5 kosztów; `null` gdy pusto |

---

## 3. BLOK 2 — Problemy UX

### P0
- P0-1: Potrójna hierarchia KPI
- P0-2: Alert niewidoczny na starcie
- P0-3: Klikalne wiersze tabeli bez klawiatury (`CostsTab`)
- P0-4: Martwa sekcja „Ostatnie koszty”
- P0-5: Mylący link „zakładka Kosztorysy” w `EstimateBudgetBarChart`

### P1
- P1-1: Duplikacja wykresów Ogólne ↔ Harmonogramy
- P1-4: „Koszty dodatkowe” vs „Koszty główne”
- P1-5: Niespójne puste stany
- P1-6: Błąd bez retry
- P1-8: Ikony tabów bez `aria-hidden`

---

## 4. BLOK 3 — Plan implementacji

| Plik fix | Zakres |
|----------|--------|
| `dashboard-ux-fix-01.md` | Hierarchia KPI, deduplikacja, nazewnictwo |
| `dashboard-ux-fix-02.md` | Alerty globalne, empty states, error retry |
| `dashboard-ux-fix-03.md` | Mobile, a11y, testy AXE |

---

## Pytania domenowe (założenia dla refaktoru)

1. Header = jedyne miejsce globalnych KPI; zakładki = szczegóły domenowe
2. Wykresy kosztów: pełna analityka na Kosztach; Finanse = skrót + kosztorysy
3. Etykieta kanoniczna: **„Koszty dodatkowe”** (zgodnie z `DashboardHeader`)
