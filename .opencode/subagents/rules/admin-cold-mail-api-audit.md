# Audyt API — admin-cold-mail

Data: 2026-07-16  
Źródło: api-audit-agent + feature `.opencode/features/admin-cold-mail.md`

## Podsumowanie

| Poziom | Liczba / opis |
|--------|----------------|
| Krytyczne | Encja `ColdMailHistory` + migracja; `SendColdMailsCommand` + validator |
| Wysokie | `GetColdMailHistoryQuery`; WebModele; 2 endpointy w `AdminController` |
| Normalne | Testy; hard cap listy GET; sanitizacja HTML later |

## Co już istnieje (reuse)

1. **`AdminController`** (`[Route("api/admin")]`, `[Authorize(Policy = SuperAdminOnly)]`) — dodać endpointy tutaj.
2. **Defense-in-depth** w handlerach Admin — sprawdzenie SuperAdmin (wzorzec z welcome emails).
3. **`IEmailSender` + `EmailMessageDto`** — enqueue custom subject/body (wzorzec Invite, **nie** welcome).
4. **`QueuedEmailSender` / `SmtpEmailSender`** — transport już skonfigurowany w DI.
5. **CQRS layout** `src/CQRS/Admin/{Area}/...` — analogicznie `Admin/ColdMails/...`.

## Czego NIE reuse'ować

- `IWelcomeEmailService` / szablony welcome / `WelcomeEmailSentAt`
- Batch po encji `User` — cold mail idzie na **zewnętrzne** adresy, nie do User tabeli

## Co dodać

### Encja (systemowa — bez TenantId/ProjectId)

```
ColdMailHistory : BaseEntity
- BatchId (Guid)           // grupuje jedną wysyłkę bulk
- RecipientEmail (string, max 320)
- Subject (string, max 500)
- Body (string, max — np. 100_000)
- Status (string/enum: Queued | Failed)
- ErrorMessage (string?, nullable)
- SentByUserId (Guid) → Users
- SentAt (DateTime)
```

Indeksy: RecipientEmail, SentAt DESC, BatchId.  
DbSet + EF config + migracja.

**Uwaga statusu v1:** `IEmailSender` tylko enqueue — status = `Queued` (sukces enqueue) lub `Failed` (błąd przed/podczas enqueue). Nie „Delivered”.

### CQRS

| Typ | Ścieżka |
|-----|---------|
| Command | `src/CQRS/Admin/ColdMails/SendColdMails/` |
| Query | `src/CQRS/Admin/ColdMails/GetColdMailHistory/` |

### Endpointy

| Method | Route | Body / Query |
|--------|-------|--------------|
| POST | `/api/admin/cold-mails/send` | `{ emails: string[], subject, body }` → result (batchId, queued, failed) |
| GET | `/api/admin/cold-mails?email=` | opcjonalny filtr → lista historii |

### Walidacja (FluentValidation)

- Każdy email: format + niepusty
- Deduplikacja (case-insensitive) przed wysyłką
- Max **50** adresów per request (jak BatchSize welcome)
- Subject: required, max 500
- Body: required, max długość

### WebModele

`Business/Interfaces/WebModels/Admin/`:
- `SendColdMailsRequest` / command body
- `SendColdMailsResultWeb` (BatchId, QueuedCount, FailedCount, items?)
- `ColdMailHistoryWeb` (Id, BatchId, RecipientEmail, Subject, Body, Status, ErrorMessage, SentByUserId, SentAt)

### Serwis domenowy

**Brak nowego serwisu** — handler woła bezpośrednio `IEmailSender.SendEmailAsync` + `IRepository<ColdMailHistory>`.

## Decyzje domyślne (nieblokujące — przyjęte do implementacji)

1. Filtr email: **contains**, case-insensitive
2. GET zwraca **pełne Body** (v1, lista adminowa)
3. Hard cap GET: **500** najnowszych rekordów (ORDER BY SentAt DESC)

## Max adresów

**50** per request.

## Testy (zalecane)

- Validator: invalid email, >50, empty subject/body
- Handler Send: zapis historii Queued; Failed przy wyjątku sendera
- Handler Get: filtr email
- Controller: SuperAdminOnly (istniejący wzorzec AdminControllerTests)

## Ryzyka

- Enqueue ≠ delivered — UI powinno komunikować „w kolejce”, nie „dostarczono”
- Duże body w liście GET — OK v1; później pagination/detail
- Brak sanitizacji HTML — admin-only, akceptowalne v1
