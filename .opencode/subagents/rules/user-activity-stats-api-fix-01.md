# Prompt implementacyjny API — user-activity-stats-api-fix-01

## Cel
Wdrożyć pełną warstwę API dla feature `user-activity-stats` wg:
- `.opencode/features/user-activity-stats.md`
- `.opencode/subagents/rules/user-activity-stats-api-audit.md`

Przed implementacją przeczytaj skills: `.opencode/skills/api-entities`, `api-cqrs`, `api-controllers`, `api-validators`, `api-repositories`, `api-unit-tests`.

## Decyzje zatwierdzone
- Encja `UserActivityLog` **bez FK** do Users
- DemoEnter: UserId i AzureAdB2CObjectId zawsze null
- Login: lookup User po OID (nie `currentUser.Id`); OID z `ICurrentUser` + fallback claim `"oid"`
- IP tylko z serwera (kontroler → command)
- POST → 204 NoContent; GET → lista max 500 DESC
- ForwardedHeaders w Program.cs

## Zakres — zrób wszystko w tym prompcie

### 1. Entities
- `Entities/Enums/UserActivityEventType.cs` — `Login = 0`, `DemoEnter = 1`
- `Entities/Models/Activity/UserActivityLog.cs` — dziedziczy `BaseEntity`; pola: EventType, IpAddress, OccurredAtUtc, Route?, UserId?, AzureAdB2CObjectId?
- `Entities/Configurations/UserActivityLogConfiguration.cs` (lub lokalizacja jak ColdMail):
  - EventType: string conversion, MaxLength 50
  - IpAddress: required, MaxLength 45
  - Route: nullable, MaxLength 500
  - AzureAdB2CObjectId: nullable, MaxLength 64
  - Indeks na OccurredAtUtc
  - Tabela `UserActivityLogs`
- `AppDbContext` — `DbSet<UserActivityLog>`
- Migracja EF: `add-user-activity-logs` (`dotnet-ef` 10.0.1, `--startup-project ../WebApi`). **Nie** uruchamiaj `database update`.

Wzoruj się na `ColdMailHistory` + jego Configuration.

### 2. Business WebModel
- `Business/WebModels/Admin/UserActivityLogWeb.cs` — Id, EventType (string lub enum serializowany), IpAddress, OccurredAtUtc, Route, UserId?, AzureAdB2CObjectId?

### 3. CQRS

#### RecordLoginActivity
Folder: `CQRS/Activity/RecordLoginActivity/`
- Command: `required string IpAddress`, `string? Route`
- Validator: Route MaxLength(500)
- Handler sealed: EventType=Login, OccurredAtUtc=UtcNow, resolve OID z ICurrentUser (+ fallback oid), lookup User po OID przez IReadRepository&lt;User&gt;, UserId nullable, AddAsync + Save

#### RecordDemoActivity
Folder: `CQRS/Activity/RecordDemoActivity/`
- Command: `required string IpAddress`, `string? Route`
- Validator: Route MaxLength(500)
- Handler sealed: EventType=DemoEnter, UserId=null, AzureAdB2CObjectId=null

#### GetUserActivityLogs
Folder: `CQRS/Admin/ActivityLogs/GetUserActivityLogs/`
- Query: opcjonalnie `UserActivityEventType? EventType` (MVP może bez filtra, ale query param OK)
- Handler: EnsureSuperAdmin (jak GetColdMailHistory), IReadRepository, sort OccurredAtUtc DESC, take 500, map do UserActivityLogWeb

Commands **bez** IAuthorizableRequest. Konwencje: no `var`, `is null`, `{}` na blokach, metody krótkie.

### 4. WebApi

#### ActivityController (nowy)
- Route: `api/activity`
- POST `login` — `[Authorize]`, body opcjonalne `{ route }`, IP z HttpContext (RemoteIpAddress / Connection), MediatR RecordLoginActivityCommand, 204
- POST `demo` — `[AllowAnonymous]`, to samo z RecordDemoActivityCommand, 204
- Helper prywatny ResolveClientIp(HttpContext) — map IPv4-mapped IPv6 jeśli potrzeba; fallback `"unknown"`

Body DTO: mały record/class `RecordActivityRequest` z `string? Route` w WebApi lub CQRS.

#### AdminController (rozszerzenie)
- GET `activity-logs` — zwraca listę UserActivityLogWeb; opcjonalny query `eventType`

#### DI
- `AddRepository<UserActivityLog>()` w ServiceCollectionExtensions (obok ColdMailHistory)

#### Program.cs / Startup
- Skonfiguruj `ForwardedHeadersOptions` (XForwardedFor | XForwardedProto), `KnownNetworks.Clear()` + `KnownProxies.Clear()` (za nginx w Docker), `UseForwardedHeaders()` **na początku** pipeline (przed auth).

### 5. Testy (podstawowe)
- Handler tests: RecordLogin (z mock User), RecordDemo, GetUserActivityLogs (EnsureSuperAdmin)
- Validator: Route za długa → invalid
- Controller test opcjonalnie jeśli wzorzec ColdMail jest prosty

Wzorce: `tests/CQRS.Tests` + Moq + FluentAssertions. Czytaj skill `api-unit-tests`.

### 6. Build
Po zmianach: `dotnet build` w solution ProductDataManagementWebAPI. Napraw błędy kompilacji.

## Poza zakresem
- Rate limiting
- Retencja RODO
- FK do User
- UI

## Raport zwrotny
Wymień utworzone/zmienione pliki, nazwę migracji, endpointy, wynik buildu.
