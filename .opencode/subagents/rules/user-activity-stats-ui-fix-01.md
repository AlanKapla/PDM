# Prompt implementacyjny UI — user-activity-stats-ui-fix-01

## Cel
Wdrożyć pełną warstwę UI dla feature `user-activity-stats` wg:
- `.opencode/features/user-activity-stats.md`
- `.opencode/subagents/rules/user-activity-stats-ui-audit.md`

Przed implementacją przeczytaj skills: `.opencode/skills/ui-api-client`, `ui-hooks`, `ui-components`, `ui-types`, `ui-accessibility`.

**Wymaga API już wdrożonego** (endpointy `/activity/login`, `/activity/demo`, `/admin/activity-logs`).

## Decyzje zatwierdzone
1. Login: `AuthContext` po pierwszym udanym `/user/me`, gate `sessionStorage` key `pdm:loginActivityRecorded`
2. Demo: w `DemoContext.applyDemoModeChange` gdy `next === true`, wywołaj POST **przed** `setStorage(true)` (żeby mock nie przejął requestu)
3. Admin toggle ON = DemoEnter (ten sam applyDemoModeChange)
4. MVP: brak filtra w UI; tabela read-only
5. Fire-and-forget: `.catch(() => {})` — nie blokuje UX
6. Zakaz `any`; named exports domenowe; `React.ReactElement`

## Zakres

### 1. Typy — `src/types/activity.types.ts`
```ts
export const UserActivityEventType = {
  Login: "Login",
  DemoEnter: "DemoEnter",
} as const;
export type UserActivityEventType =
  (typeof UserActivityEventType)[keyof typeof UserActivityEventType];

export interface RecordActivityRequest {
  route?: string;
}

export interface UserActivityLogWeb {
  id: string;
  eventType: UserActivityEventType | string;
  ipAddress: string;
  occurredAtUtc: string;
  route: string | null;
  userId: string | null;
  azureAdB2CObjectId: string | null;
}
```
Dostosuj nazwy pól do faktycznego JSON z API (camelCase).

### 2. API
- `src/api/activityApi.ts`:
  - `recordLogin(body?: RecordActivityRequest): Promise<void>` → POST `activity/login`
  - `recordDemo(body?: RecordActivityRequest): Promise<void>` → POST `activity/demo`
- `adminApi.ts`: dodaj `getActivityLogs(): Promise<UserActivityLogWeb[]>` → GET `admin/activity-logs`

Ścieżki bez `/api` (baseURL axios już ma `/api`). Wzoruj `adminApi` / cold-mail.

### 3. AuthContext — login activity
Po udanym fetch `/user/me` gdy `isAuthenticated`:
- jeśli `sessionStorage.getItem("pdm:loginActivityRecorded")` brak:
  - `activityApi.recordLogin({ route: window.location.pathname }).catch(() => {})`
  - `sessionStorage.setItem("pdm:loginActivityRecorded", "1")`
Nie wołaj z AuthCallback / Home / LoggedOut.
Logout już czyści sessionStorage — OK.

### 4. DemoContext — demo activity
W `applyDemoModeChange(next)`:
```
if (next === true) {
  void activityApi.recordDemo({ route: window.location.pathname }).catch(() => {});
}
// dopiero potem setStorage / setState / clear queries
```
Krytyczne: **przed** `setStorage(true)`.

### 5. Hook — `src/hooks/useActivityLogs.ts`
React Query jak `useAdminUsers`:
- queryKey: `["activityLogs"]`
- queryFn: `adminApi.getActivityLogs`

### 6. Admin UI
Wzorce: `ColdMailsAdminPanel`, `AdminColdMailsPage`, `ColdMailHistoryTable` / Users table.

Utwórz:
- `ActivityLogsAdminPanel.tsx` — karta hub → `navigate("/admin/activity-logs")`
- `ActivityLogsTable.tsx` — kolumny: data (`formatDate`), typ, IP, route, user (userId lub OID skrócony)
- `AdminActivityLogsPage.tsx` — loading / error / empty / tabela; Back do `/admin`

Zmodyfikuj:
- `AdminPage.tsx` — dodaj kartę Activity
- `AppRouter.tsx` — route `/admin/activity-logs` w `SuperAdminRoute`

Kontrast: `neutral.600+` / `gray.600` dla treści (nie `gray.500`).

### 7. Mock — `mockHandlers.ts`
| Path | Method | Response |
|------|--------|----------|
| `/api/activity/login` | post | 204 |
| `/api/activity/demo` | post | 204 |
| `/api/admin/activity-logs` | get | 1–2 sample logi |

### 8. Build / lint
`npm run build` w ProjectDataManagementUI (lub przynajmniej `tsc`). Napraw błędy typów.

## Poza zakresem
- Filtry / paginacja UI
- AXE test (opcjonalnie jeśli szybki)
- Zmiany Home / AuthCallback / DemoModeHomeToggle (tylko context)

## Raport zwrotny
Utworzone/zmienione pliki, jak podpięte login/demo, wynik buildu.
