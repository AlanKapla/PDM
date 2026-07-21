# Audyt UI — user-activity-stats

Data: 2026-07-21  
Źródło: ui-audit-agent + feature `.opencode/features/user-activity-stats.md`  
Skills: `ui-api-client`, `ui-hooks`, `ui-components`, `ui-types`, `ui-forms-modals`

## Podsumowanie

Feature nie istnieje w UI. Trzeba dodać: `activityApi` (2× POST fire-and-forget + GET w adminApi), podpięcie login/demo, hub-kartę + stronę `/admin/activity-logs`, hook React Query, mocki demo, typy.

| Metryka | Wartość |
|---------|---------|
| Nowe komponenty | 3 + 1 page |
| Zmodyfikowane pliki | ~7 |
| Nowe hooki | 1 (GET lista) |
| Nowe typy TypeScript | 3–4 |
| Nowe wywołania API | 3 |
| Pytania domenowe | 3 — **zatwierdzone defaulty poniżej** |

---

## BLOK 1 — Stan obecny UI

| Komponent/Strona | Lokalizacja | Opis | Powiązane z feature |
|-----------------|------------|------|---------------------|
| `AuthCallback` | `src/pages/AuthCallback.tsx` | OAuth redirect; navigate po `isAuthenticated` | Nie wołać activity tutaj |
| `AuthProvider` | `src/context/AuthContext.tsx` | Po MSAL: `sync-b2c` + `/user/me`; logout czyści `sessionStorage` | **Login once-per-session** |
| `DemoProvider` | `src/context/DemoContext.tsx` | `enterDemoMode` / `toggleDemoMode` → `applyDemoModeChange` | **Jedyny** punkt DemoEnter |
| `DemoModePanel` | `src/components/admin/DemoModePanel.tsx` | Switch → `toggleDemoMode()` | Też rejestruje DemoEnter przy ON |
| `AdminPage` | `src/pages/AdminPage.tsx` | Hub: Demo / Users / Cold mail | Dodać kartę Activity |
| `adminApi` / mockHandlers | `src/api/` | SuperAdmin + demo intercept | GET + stuby |

---

## BLOK 2 — Luki i braki w UI

| Brak / Luka | Typ | Priorytet | Opis |
|-------------|-----|----------|------|
| Brak `activityApi` | api | P0 | `recordLogin`, `recordDemo` |
| Brak typów activity | typ | P0 | Request + `UserActivityLogWeb` + enum |
| Brak fire-and-forget login | context | P0 | Po sukcesie sesji MSAL, raz na sesję |
| Brak fire-and-forget demo | context | P0 | Przy wejściu w demo (Home + Admin) |
| Brak karty hub + strony + tabeli | UI | P0 | `/admin/activity-logs` |
| Brak `useActivityLogs` | hook | P0 | React Query GET |
| Brak route | routing | P0 | `SuperAdminRoute` |
| Brak mock stubs | mock | P1 | GET/POST |

---

## Rekomendacje podpięcia (kluczowe) — ZATWIERDZONE

### Login — `AuthContext` po pierwszym udanym `/user/me` + `sessionStorage` gate `pdm:loginActivityRecorded`
JWT gotowy; raz na sesję tabu; logout czyści storage. Nie Home/LoggedOut/AuthCallback.

### Demo — `DemoContext` przy `next === true`, **przed** `setStorage(true)`
Pokrywa Home i Admin switch; AllowAnonymous trafia w real API zanim mock przejmie ruch. Admin toggle ON = DemoEnter.

### MVP admin
Bez filtra eventType/daty; tabela read-only; hard cap z API (500).

---

## Plan UI (kolejność)

1. Typy `activity.types.ts`
2. `activityApi` (POST) + `adminApi.getActivityLogs`
3. AuthContext + DemoContext
4. `useActivityLogs`
5. Panel + Table + Page
6. Router + AdminPage
7. mockHandlers
8. AXE (opcjonalnie w MVP jeśli czas)

### Pliki do utworzenia
- `src/types/activity.types.ts`
- `src/api/activityApi.ts`
- `src/hooks/useActivityLogs.ts`
- `src/components/admin/ActivityLogsAdminPanel.tsx`
- `src/components/admin/ActivityLogsTable.tsx`
- `src/pages/AdminActivityLogsPage.tsx`

### Pliki do zmiany
- `AuthContext.tsx`, `DemoContext.tsx`, `adminApi.ts`, `AdminPage.tsx`, `AppRouter.tsx`, `mockHandlers.ts`

### Mock stubs (wymagane)
| Path | Method | Response |
|------|--------|----------|
| `/api/activity/login` | post | 204 |
| `/api/activity/demo` | post | 204 |
| `/api/admin/activity-logs` | get | sample array |

---

## Pytania domenowe — DECYZJE (Feature Planner)
1. Login once-per-tab-session (`sessionStorage`) → **tak**
2. Admin toggle demo ON = `DemoEnter` → **tak**
3. MVP bez filtra eventType/daty → **tak**
