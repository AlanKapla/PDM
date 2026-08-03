# Prompt: admin-cold-mail-ui-fix-01 — Typy, API client, routing, hub card

## Cel
Warstwa kontraktu UI + nawigacja do cold mail w module admina.

## Spec / audyt
- `.opencode/features/admin-cold-mail.md`
- `.opencode/subagents/rules/admin-cold-mail-ui-audit.md`
- Skills: `ui-api-client`, `ui-types`, `ui-components`
- API: POST `/api/admin/cold-mails/send`, GET `/api/admin/cold-mails?email=`

## Zmiany

### Typy (`src/types/admin.types.ts`)
- `SendColdMailsRequest` / `SendColdMailsResultWeb`
- `ColdMailHistoryWeb` — zgodne z API (Id, BatchId, RecipientEmail, Subject, Body, Status, ErrorMessage, SentByUserId, SentAt)

### API (`src/api/adminApi.ts`)
- `sendColdMails(request)` → POST
- `getColdMails(email?: string)` → GET z query param

### Mocki
- Jeśli `mockHandlers.ts` obsługuje admin — dodaj mocki dla nowych endpointów (demo mode)

### Routing (`AppRouter.tsx`)
- `/admin/cold-mails` → `AdminColdMailsPage` w `SuperAdminRoute` (jak `/admin/users`)

### Hub (`ColdMailsAdminPanel.tsx` + `AdminPage.tsx`)
- Karta jak UsersAdminPanel: tytuł, krótki opis, przycisk → `/admin/cold-mails`
- Dodaj do SimpleGrid na AdminPage

## Poza zakresem
- Pełny formularz i tabela (fix-02)

## Definition of done
- Nawigacja z /admin do /admin/cold-mails działa (strona może być stub z Heading)
- Typy i API client gotowe pod hooki
