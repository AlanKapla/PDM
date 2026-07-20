# Summary — admin-cold-mail

Data: 2026-07-16  
Status: **wdrożony** (API + UI + testy)

## Feature spec
`.opencode/features/admin-cold-mail.md`

## Audyty
- `.opencode/subagents/rules/admin-cold-mail-api-audit.md`
- `.opencode/subagents/rules/admin-cold-mail-ui-audit.md`

## Prompty wykonane
1. `admin-cold-mail-api-fix-01` — encja + migracja ✅
2. `admin-cold-mail-api-fix-02` — CQRS + AdminController + testy ✅
3. `admin-cold-mail-ui-fix-01` — typy, API, routing, hub ✅
4. `admin-cold-mail-ui-fix-02` — formularz, historia, hooki ✅

## Decyzje domenowe
- SuperAdmin only
- Historia DB: 1 wiersz = 1 odbiorca
- Body: textarea (plain text / prosty HTML), bez WYSIWYG
- Bez rate limitu v1; max **50** adresów; filtr email contains; GET hard cap 500
- Status v1: `Queued` | `Failed` (enqueue, nie delivery)

## Co zostało zrobione

### API
- Encja `ColdMailHistory`, enum `ColdMailStatus`, EF config, migracja `AddColdMailHistory`
- `POST /api/admin/cold-mails/send`, `GET /api/admin/cold-mails?email=`
- Handlery + walidatory + WebModele
- Testy: 11 CQRS + rozszerzone AdminControllerTests

### UI
- Karta na `/admin` → `/admin/cold-mails`
- Formularz (maile, subject, body) + confirm
- Historia + filtr + modal szczegółów (body plain text)
- Hooki React Query, mocki demo, AXE tests

## Blokery
Brak.

## Pozostało (ops / poza kodem)
1. **Uruchomić migrację DB** przed deployem (`AddColdMailHistory`)
2. Upewnić się, że SMTP jest skonfigurowany w środowisku
3. Manualny smoke test jako SuperAdmin

## Jak przetestować
### API
```powershell
cd 02-ApplicationServices/ProductDataManagementWebAPI
dotnet test tests/CQRS.Tests --filter ColdMail
dotnet test tests/WebApi.Tests --filter AdminController
```
### UI
```powershell
cd 01-Applications/ProjectDataManagementUI
npm run test:axe -- ColdMail
```
### Manual
1. Zaloguj SuperAdmin → Panel administratora → Cold maile
2. Wklej 1–2 maile, subject, body → Wyślij → potwierdź
3. Sprawdź historię + filtr po fragmencie adresu
4. Non-SuperAdmin: brak wejścia / redirect z `/admin`
