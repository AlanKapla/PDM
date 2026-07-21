# Feature summary — user-activity-stats

Data: 2026-07-21  
Status: **MVP wdrożony** (API + UI)

## Co zostało zrobione

### API
- Encja `UserActivityLog` + enum `UserActivityEventType` (Login | DemoEnter) + konfiguracja EF
- Migracja `20260721131145_add-user-activity-logs` (bez `database update`)
- Commands: `RecordLoginActivity`, `RecordDemoActivity`
- Query: `GetUserActivityLogs` (SuperAdmin, max 500, DESC)
- `ActivityController`: POST login (JWT), POST demo (AllowAnonymous)
- `AdminController`: GET `activity-logs`
- `ForwardedHeaders` w `Program.cs` (IP klienta za nginx)
- Testy CQRS: 11/11 OK; build API OK

### UI
- `activityApi.recordLogin` / `recordDemo` (fire-and-forget)
- Login: `AuthContext` po `/user/me` + gate `sessionStorage` `pdm:loginActivityRecorded`
- Demo: `DemoContext.applyDemoModeChange(true)` **przed** `setStorage(true)`
- Admin: karta hub + strona `/admin/activity-logs` + tabela
- Mock stuby dla demo mode
- `npm run build` OK

## Endpointy

| Method | Route | Auth | Opis |
|--------|-------|------|------|
| POST | `/api/activity/login` | JWT | Zapis Login; IP z serwera; body `{ route? }` |
| POST | `/api/activity/demo` | AllowAnonymous | Zapis DemoEnter; IP z serwera |
| GET | `/api/admin/activity-logs` | SuperAdminOnly | Lista logów (max 500) |

## Jak zbierane są eventy

| Event | Trigger UI | API |
|-------|------------|-----|
| Login | Po pierwszym udanym `/user/me` w sesji tabu (`AuthContext`) | `POST /activity/login` |
| DemoEnter | Wejście w demo (`enterDemoMode` / Admin toggle ON) przed włączeniem mock | `POST /activity/demo` |

Błędy POST są ignorowane (`.catch(() => {})`) — nie blokują UX.

## Poza MVP
- Middleware na wszystkie requesty
- Rate limiting / retencja RODO
- Filtry / paginacja UI
- UserAgent / geoIP
- AXE test tabeli (opcjonalny)

## Deploy
Przed deployem: zastosować migrację DB `add-user-activity-logs`.

## Ścieżki artefaktów

| Artefakt | Ścieżka |
|----------|---------|
| Feature spec | `.opencode/features/user-activity-stats.md` |
| Audyt API | `.opencode/subagents/rules/user-activity-stats-api-audit.md` |
| Audyt UI | `.opencode/subagents/rules/user-activity-stats-ui-audit.md` |
| Prompt API | `.opencode/subagents/rules/user-activity-stats-api-fix-01.md` |
| Prompt UI | `.opencode/subagents/rules/user-activity-stats-ui-fix-01.md` |
| To summary | `.opencode/subagents/rules/user-activity-stats-summary.md` |

## Blokery
Brak.
