# Audyt API — user-activity-stats

Data: 2026-07-21  
Źródło: api-audit-agent + feature `.opencode/features/user-activity-stats.md`  
Decyzje domenowe: zatwierdzone (POST login JWT, POST demo AllowAnonymous, GET admin SuperAdminOnly, IP z serwera, body opcjonalnie `route`)

---

## BLOK 1 — Stan obecny

### Encje / DB
- Brak encji aktywności użytkowników. Najbliższy wzorzec historii: **`ColdMailHistory`** (`Entities/Models/ColdMails/`) — `BaseEntity`, konfiguracja EF, `DbSet` w `AppDbContext`, repo DI `AddRepository<ColdMailHistory>()`, indeksy + enum jako string.
- `UserSession` istnieje, ale to sesje refresh-token — **nie reuse** pod activity logs.
- Encje systemowe (admin/cold-mail) **nie mają** `TenantId`/`ProjectId` — `UserActivityLog` powinien iść tą samą ścieżką (log globalny, nie projektowy).

### Auth / identity
| Element | Lokalizacja | Zachowanie |
|---------|-------------|------------|
| JWT Bearer (Azure AD B2C / CIAM) | `ServiceCollectionExtensions.AddAzureAdB2C` | Authority + audience z `AzureAdB2C` config |
| Claims OID | `ClaimNames.Oid` = long URI `objectidentifier` | `CurrentUser` czyta **tylko** ten claim |
| SignalR OID | `AzureAdB2CUserIdProvider` | Czyta `"oid"` **oraz** fallback `ClaimNames.Oid` |
| User identity w handlerach | `ICurrentUser` (`Business.Implementation.Model.CurrentUser`) | `AzureAdB2CObjectId` z claim (bez DB); `Id` ładuje `User` po OID i **rzuca `UnauthorizedApiException` jeśli brak lokalnego User** |
| Sync użytkownika | `POST /api/user/sync-b2c` | Tworzy/linkuje `User` w DB |

### Endpointy Admin
- `AdminController` — `[Route("api/admin")]` + `[Authorize(Policy = SuperAdminOnly)]` na klasie.
- Wzorzec listy historii: `GET /api/admin/cold-mails` → `GetColdMailHistoryQuery` → hard cap **500**, sort DESC, opcjonalny filtr.
- Handlery Admin robią **defense-in-depth** `EnsureSuperAdmin()` mimo policy na kontrolerze.

### IP klienta
- **nginx** ustawia `X-Forwarded-For` / `X-Real-IP` (`03-Deployment/nginx.conf`).
- W API: **brak** `UseForwardedHeaders`, brak `ForwardedHeadersOptions`, brak użycia `RemoteIpAddress` w kodzie.
- `Program.cs` pipeline: ExceptionHandling → WebSockets → Localization → Routing → Https → Cors → Swagger → Auth → Controllers. **Bez ForwardedHeaders.**

### AllowAnonymous
- W całym WebApi **zero** użyć `[AllowAnonymous]`.
- Brak globalnego `FallbackPolicy` / `RequireAuthenticatedUser` — endpoint **bez** `[Authorize]` jest publiczny.
- Kontrolery chronione per-endpoint lub per-class (`[Authorize]` / policy).

### CQRS / kontrolery
- 17 kontrolerów; brak `ActivityController`.
- Skills: `.opencode/skills/api-{cqrs,entities,controllers,validators,repositories,unit-tests}`.
- Konwencje: sealed handlers, no `var`, `is null`, `IRepository`/`IReadRepository`, Commands bez `IAuthorizableRequest` dla operacji SuperAdmin/system (jak ColdMails).

### Co już jest vs czego brakuje
| Już jest | Brakuje |
|----------|---------|
| Auth JWT + `ICurrentUser` | Encja `UserActivityLog` + migracja |
| `AdminController` + SuperAdminOnly | `GET activity-logs` |
| Wzorzec historii (ColdMail) | `POST /api/activity/login` i `/demo` |
| nginx X-Forwarded-* | `ForwardedHeaders` w ASP.NET |
| DI repo pattern | Rejestracja `UserActivityLog` |

---

## BLOK 2 — Luki i braki

| Brak / Luka | Warstwa | Priorytet | Opis |
|-------------|---------|-----------|------|
| Encja + config + migracja `UserActivityLog` | Entities | Krytyczne | Tabela logów MVP |
| Enum `UserActivityEventType` | Entities | Krytyczne | `Login` \| `DemoEnter` |
| `RecordLoginActivityCommand` + handler | CQRS | Krytyczne | Zapis Login z IP + identity |
| `RecordDemoActivityCommand` + handler | CQRS | Krytyczne | Zapis DemoEnter, bez wymogu JWT |
| `GetUserActivityLogsQuery` + handler | CQRS | Krytyczne | Lista dla SuperAdmin |
| `ActivityController` | WebApi | Krytyczne | POST login / demo |
| Endpoint w `AdminController` | WebApi | Krytyczne | GET `activity-logs` |
| `UseForwardedHeaders` | WebApi | **Wysokie** | Bez tego IP = IP nginx/Docker, nie klienta |
| Bezpieczne pobranie `UserId` (nie `currentUser.Id`) | CQRS | Wysokie | `Id` rzuca gdy User jeszcze nie zsynchronizowany |
| Odczyt claim `"oid"` vs long URI | Business/CQRS | Wysokie | Niespójność SignalR vs `CurrentUser` — ryzyko pustego `AzureAdB2CObjectId` |
| Walidatory `route` max length | CQRS | Normalne | FluentValidation |
| Web model `UserActivityLogWeb` | Business | Normalne | DTO listy admin |
| Rejestracja DI `AddRepository<UserActivityLog>()` | WebApi | Krytyczne | Bez tego handler nie wstanie |
| Testy handler/validator/controller | Tests | Normalne | Jak ColdMail |
| Rate limiting POST (szczególnie demo) | WebApi | Normalne | Poza MVP — ryzyko spam |

---

## BLOK 3 — Zmiany w encjach/DB

| Encja | Zmiana | Typ | Wymaga migracji |
|-------|--------|-----|-----------------|
| `UserActivityEventType` | Nowy enum | Enum | nie (w config encji) |
| `UserActivityLog` | Nowa encja | Nowa encja | **tak** |
| `UserActivityLogConfiguration` | Nowa config | Konfiguracja EF | z migracją |
| `AppDbContext` | `DbSet<UserActivityLog>` | DbSet | z migracją |

### Rekomendowany model (wzorzec ColdMailHistory)

```csharp
// Entities/Models/Activity/UserActivityLog.cs
public class UserActivityLog : BaseEntity
{
    public UserActivityEventType EventType { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public string? Route { get; set; }
    public Guid? UserId { get; set; }
    public string? AzureAdB2CObjectId { get; set; }
}
```

```csharp
// Entities/Enums/UserActivityEventType.cs
public enum UserActivityEventType
{
    Login = 0,
    DemoEnter = 1
}
```

### Konfiguracja EF (zalecenia)
| Pole | Config |
|------|--------|
| `EventType` | `HasConversion<string>().HasMaxLength(50)` (jak ColdMailStatus) |
| `IpAddress` | required, `HasMaxLength(45)` (IPv6) |
| `OccurredAtUtc` | required |
| `Route` | nullable, `HasMaxLength(500)` |
| `UserId` | nullable; **bez FK** do `Users` w MVP |
| `AzureAdB2CObjectId` | nullable, `HasMaxLength(64)` |
| Indeksy | `OccurredAtUtc` DESC; opcjonalnie `(EventType, OccurredAtUtc)` |
| Tabela | `UserActivityLogs` |

Migracja: `dotnet ef migrations add add-user-activity-logs --project src/Entities --startup-project src/WebApi`  
CI pin: `dotnet-ef` **10.0.1**. Nie uruchamiać `database update` w PR — tylko generacja.

---

## BLOK 4 — Nowe Commands/Queries

| Command/Query | Typ | Opis | Handler |
|---------------|-----|------|---------|
| `RecordLoginActivityCommand` | nowy | Body: `Route?`; IP ustawiane w kontrolerze; EventType=Login; identity z `ICurrentUser` | `RecordLoginActivityCommandHandler` → `Unit` |
| `RecordDemoActivityCommand` | nowy | Body: `Route?`; IP z serwera; EventType=DemoEnter; UserId/OID null (MVP) | `RecordDemoActivityCommandHandler` → `Unit` |
| `GetUserActivityLogsQuery` | nowy | Opcjonalny filtr `eventType?`; lista max 500 DESC | `GetUserActivityLogsQueryHandler` |
| Validators | nowe | `Route` MaxLength(500); puste OK | `*CommandValidator` |

### Lokalizacja plików

```
src/CQRS/Activity/RecordLoginActivity/
src/CQRS/Activity/RecordDemoActivity/
src/CQRS/Admin/ActivityLogs/GetUserActivityLogs/
```

### Identity w handlerze Login — rekomendacja

**Nie** wywoływać `currentUser.Id` bezpośrednio (rzuca gdy User nie ma w DB).

Lookup `User` po `AzureAdB2CObjectId`; `UserId` nullable. Rozważyć fallback claim `"oid"` (jak SignalR).

### GET handler
- Wzorzec 1:1 z `GetColdMailHistoryQueryHandler`: `EnsureSuperAdmin()`, `GetPagedBySearchAsync(..., OccurredAtUtc, descending: true, take: 500)`.
- Brak `IAuthorizableRequest` — auth na kontrolerze + EnsureSuperAdmin.

---

## BLOK 5 — Zmiany w kontrolerach

| Endpoint | HTTP | Nowy/Modyfikacja | Opis |
|----------|------|------------------|------|
| `/api/activity/login` | POST | **Nowy** `ActivityController` | `[Authorize]`; body opcjonalne `{ route }`; IP z `HttpContext`; **204 NoContent** |
| `/api/activity/demo` | POST | **Nowy** `ActivityController` | `[AllowAnonymous]`; IP z serwera; **204** |
| `/api/admin/activity-logs` | GET | **Rozszerzenie** `AdminController` | SuperAdminOnly (klasa); query opcjonalnie `eventType`; **200** + lista |

Rekomendacja: POST → nowy `ActivityController`; GET → `AdminController`.

---

## BLOK 6 — Zmiany w serwisach

Brak nowego serwisu domenowego.  
DI: `AddRepository<UserActivityLog>()`.  
Wymagane: `UseForwardedHeaders` w `Program.cs` (Docker: KnownNetworks/Proxies).

---

## BLOK 7 — Problemy i ryzyka

| # | Problem | Ryzyko | Rekomendacja |
|---|---------|--------|--------------|
| 1 | Brak ForwardedHeaders | Wysokie — IP = proxy | Middleware + Known* |
| 2 | `currentUser.Id` przed sync | Wysokie — 401 | Lookup User po OID |
| 3 | Claim oid vs long URI | Wysokie | Fallback `"oid"` |
| 4 | AllowAnonymous spam | Normalne | Poza MVP rate limit |
| 5 | Spoofing XFF bez KnownProxies | Wysokie | Clear Known* tylko za nginx |
| 6 | Route z body | Niskie | MaxLength |
| 7 | Brak retencji RODO | Normalne | Poza MVP |
| 8 | Race activity vs sync-b2c | Normalne | UserId null + OID OK |
| 9 | GET hard cap 500 | Normalne | Jak cold-mail |

---

## Pliki do utworzenia / zmiany

### Utworzyć
- Entities: enum, model Activity/UserActivityLog, Configuration, migracja
- Business: `WebModels/Admin/UserActivityLogWeb.cs`
- CQRS: RecordLoginActivity/*, RecordDemoActivity/*, Admin/ActivityLogs/GetUserActivityLogs/*
- WebApi: `ActivityController.cs`
- Tests: CQRS Activity + Admin Get; WebApi ActivityController + AdminControllerTests

### Zmodyfikować
- `AppDbContext.cs` — DbSet
- `ServiceCollectionExtensions.cs` — AddRepository
- `AdminController.cs` — GET activity-logs
- `Program.cs` — UseForwardedHeaders

---

## Rekomendacje implementacyjne (konwencje)

1. Handlery `sealed`; explicit types; `is null`; bloki `{}`.
2. Zapis: `IRepository<UserActivityLog>`; odczyt: `IReadRepository`; lookup: `IReadRepository<User>`.
3. `OccurredAtUtc = DateTime.UtcNow` w handlerze.
4. POST → 204 NoContent; GET → `IReadOnlyList<UserActivityLogWeb>`.
5. Commands bez `IAuthorizableRequest`.
6. IP w kontrolerze → `required string IpAddress` na command (nie z body).

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Nowe encje | 1 + 1 enum |
| Nowe Commands | 2 |
| Nowe Queries | 1 |
| Nowe endpointy | 3 |
| Nowe serwisy | 0 |
| Wymaga migracji DB | tak |
| Pytania domenowe | 3 (nieblokujące) — **zatwierdzone defaulty poniżej** |

### Pytania domenowe — DECYZJE (Feature Planner)
1. FK UserId → Users? → **bez FK**
2. Demo + opcjonalny JWT wzbogaca identity? → **nie** (UserId/OID zawsze null na DemoEnter)
3. Kolejność vs sync-b2c? → activity OK przed sync (OID + nullable UserId)
