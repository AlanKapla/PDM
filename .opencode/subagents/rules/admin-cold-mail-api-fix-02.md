# Prompt: admin-cold-mail-api-fix-02 — CQRS Send + Get + AdminController

## Cel
Zaimplementować wysyłkę cold maili, query historii oraz endpointy w AdminController.

## Spec / audyt
- `.opencode/features/admin-cold-mail.md`
- `.opencode/subagents/rules/admin-cold-mail-api-audit.md`
- Skills: `api-cqrs`, `api-controllers`, `api-validators`, `api-repositories`
- Zależność: **admin-cold-mail-api-fix-01** (encja musi istnieć)

## Endpointy (AdminController, SuperAdminOnly już na kontrolerze)

### POST `/api/admin/cold-mails/send`
- Body: `{ emails: string[], subject: string, body: string }`
- Handler:
  1. Sprawdź SuperAdmin (defense-in-depth jak welcome emails)
  2. Znormalizuj: trim, dedupe case-insensitive
  3. Dla każdego emaila: wywołaj `IEmailSender.SendEmailAsync` z `EmailMessageDto` (To, Subject, Body — wzorzec Invite, **NIE** IWelcomeEmailService)
  4. Zapisz `ColdMailHistory` per odbiorca: Status `Queued` przy sukcesie enqueue, `Failed` + ErrorMessage przy wyjątku
  5. Wspólny `BatchId` dla całej operacji; `SentByUserId` = current user; `SentAt` = UtcNow
- Response: `SendColdMailsResultWeb` (BatchId, QueuedCount, FailedCount, ewentualnie lista per-item)

### GET `/api/admin/cold-mails?email=`
- Opcjonalny filtr: contains, case-insensitive na RecipientEmail
- ORDER BY SentAt DESC
- Hard cap **500** rekordów
- Response: `IReadOnlyList<ColdMailHistoryWeb>`

## Walidacja (FluentValidation) — Send
- Subject: required, max 500
- Body: required, max (zgodny z encją)
- Emails: niepusta lista, max **50**, każdy poprawny format email

## Pliki (proponowane)
```
src/CQRS/Admin/ColdMails/SendColdMails/
  SendColdMailsCommand.cs
  SendColdMailsCommandHandler.cs
  SendColdMailsCommandValidator.cs
src/CQRS/Admin/ColdMails/GetColdMailHistory/
  GetColdMailHistoryQuery.cs
  GetColdMailHistoryQueryHandler.cs
  GetColdMailHistoryQueryValidator.cs (opcjonalnie — email filtr max length)
src/Business/Interfaces/WebModels/Admin/
  ColdMailHistoryWeb.cs
  SendColdMailsResultWeb.cs
  (request może być samym Commandem)
src/WebApi/Controllers/AdminController.cs — 2 akcje
```

## Konwencje
- Handlers `sealed`, jawne typy, `IRepository` / `IReadRepository`
- Predykaty: brak TenantId/ProjectId (encja systemowa)
- Domain exceptions wg konwencji repo
- Brak nowego serwisu domenowego

## Testy (minimum)
- Validator: invalid email, >50, empty subject
- Send handler: zapis Queued; Failed gdy sender rzuca
- Get handler: filtr email
- Controller tests: endpointy istnieją / authorize (wzorzec AdminControllerTests)

## Definition of done
- Endpointy działają
- `dotnet build` + `dotnet test` (CQRS.Tests / WebApi.Tests) przechodzą dla nowych testów
