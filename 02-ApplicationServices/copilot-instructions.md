# 02-ApplicationServices — Backend: instrukcje dla GitHub Copilot

## Stack technologiczny

| Technologia | Wersja |
|-------------|--------|
| .NET | 10.0 (`net10.0`) |
| ASP.NET Core | 10.0.1 |
| MediatR | 13.0.0 |
| FluentValidation | 12.0.0 |
| Microsoft.EntityFrameworkCore + SqlServer | 10.0.1 |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.1 |
| Microsoft.Identity.Web | 4.2.0 |
| Azure.Identity | 1.17.1 |
| Swashbuckle.AspNetCore | 6.6.2 |
| SignalR | wbudowany w ASP.NET Core 10 |

---

## Struktura projektów (`src/`)

```
src/
├── WebApi/                     # punkt wejścia — controllers, middleware, program
│   ├── Program.cs
│   ├── Controllers/            # BaseApiController + kontrolery endpointów
│   ├── Authorization/          # PermissionAuthorizationHandler, PermissionRequirement
│   ├── Extensions/             # ServiceCollectionExtensions, ApplicationBuilderExtensions
│   ├── Hubs/                   # SignalR hubs
│   ├── Middleware/             # ApiExceptionMiddleware
│   ├── Services/               # serwisy webowe (np. CurrentUser z HttpContext)
│   └── appsettings.json
├── CQRS/                       # Commands, Queries, Handlers, Behaviours
│   ├── IRequestCommand.cs
│   ├── IRequestQuery.cs
│   ├── IAuthorizableRequest.cs
│   ├── Behaviours/             # ValidationBehavior, AuthorizationBehavior, TransactionBehavior
│   ├── Projects/               # Commands + Queries pogrupowane per feature
│   ├── CostEstimates/
│   ├── CostEstimateTemplates/
│   ├── CostTrackers/
│   ├── WorkSchedules/
│   ├── Files/
│   ├── Chats/
│   ├── Notifications/
│   ├── Tenants/
│   ├── Users/
│   ├── Roles/
│   ├── ProjectCosts/
│   ├── Messages/
│   └── AI/
├── Business/                   # logika domenowa, serwisy, walidatory
│   ├── Interfaces/
│   │   ├── Constants/          # PermissionCodes.cs, RoleCodes.cs, PermissionScope.cs, ResourceScope.cs
│   │   ├── Exceptions/         # ApiException.cs, ApiExceptionReason.cs
│   │   ├── WebModels/          # web modele per domena (Projects/, CostEstimates/, itd.)
│   │   └── Model/              # ICurrentUser, interfejsy domenowe
│   └── Implementation/
│       ├── Services/           # AccessService, CurrentUser i inne
│       └── Validators/         # FluentValidation validators
├── Business.Contracts/         # Messages (zdarzenia domenowe / wiadomości SignalR)
├── Entities/                   # EF Core — DbContext, encje, migracje, konfiguracje
│   ├── Context/                # AppDbContext.cs, AppDbContextFactory.cs
│   ├── Models/                 # encje (BaseEntity, Project, Tenant, User, itd.)
│   ├── Configurations/         # IEntityTypeConfiguration per encjo
│   ├── Migrations/             # EF Core migracje
│   └── Enums/                  # RoleScope.cs, SystemRole.cs
├── Repositories/               # implementacja repozytoriów
│   └── Repository/
│       ├── Interfaces/         # IRepository<T>, IReadRepository<T>
│       └── Repositories/       # implementacje
├── Chat/                       # rejestracja SignalR chat hub
└── FileUpload/                 # logika uploadów do Azure Blob
```

---

## Zmienne środowiskowe (klucze z `appsettings.json`)

```json
ConnectionStrings__DefaultConnection
AzureAdB2C__Instance
AzureAdB2C__Domain
AzureAdB2C__ClientId
AzureAdB2C__TenantId
AzureAdB2C__ClientSecret
Azure__ClientId
Azure__TenantId
Azure__ClientSecret
Redis__ConnectionString
Redis__IsEnabled
Redis__DefaultExpirationMinutes
BlobStorage__ContainerUrl
BlobStorage__QueueUrl
CorsSettings__AllowedOrigins
EmailSettings__SendGrid__ApiKey
EmailSettings__DefaultFromEmail
Frontend__BaseUrl
```

---

## Wzorzec CQRS

### Controller → BaseApiController

Każdy kontroler dziedziczy `BaseApiController(IMediator mediator)`:

```csharp
[Route("api/tenants/{tenantId}/project")]
[ApiController]
public class ProjectController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [Authorize(Policy = PermissionCodes.TenantView)]
    public async Task<IActionResult> GetTenantProjects([FromRoute] Guid tenantId)
    {
        GetTenantProjectsQuery query = new GetTenantProjectsQuery(tenantId);
        ProjectDetailsWeb result = await Send(query);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.TenantProjectCreate)]
    public async Task<IActionResult> CreateProject(
        [FromRoute] Guid tenantId,
        [FromBody] CreateProjectCommand command)
    {
        command = command with { TenantId = tenantId };
        ProjectDetailsWeb result = await Send(command);
        return CreatedAtAction(nameof(GetTenantProjects), new { tenantId }, result);
    }
}
```

HTTP status konwencje:
- `200 OK` — odczyt danych
- `201 Created` — tworzenie zasobu z `CreatedAtAction`
- `204 NoContent` — operacja bez wyniku (delete, update bez zwracania)
- `400 Bad Request` — błąd walidacji (przez `ValidationApiException`)
- `401 Unauthorized` — brak ważnego tokenu
- `403 Forbidden` — brak uprawnień
- `404 Not Found` — zasób nie istnieje (`NotFoundApiException`)
- `409 Conflict` — konflikt stanu
- `500 Internal Server Error` — nieobsłużony wyjątek

URL pattern: `/api/tenants/{tenantId}/[zasób]` lub `/api/tenants/{tenantId}/project/{projectId}/[zasób]`

### Commands i Queries — zawsze `record`

```csharp
// Command (zmienia stan)
public record CreateProjectCommand : IRequestCommand<ProjectDetailsWeb>
{
    public required string Name { get; init; }
    public required Guid TenantId { get; init; }
}

// Query (odczyt)
public record GetProjectDetailsQuery(Guid TenantId, Guid ProjectId)
    : IRequestQuery<ProjectDetailsWeb>;
```

Reguły:
- Commands i Queries to zawsze `record` (immutable, `{ get; init; }`)
- Nigdy nie używaj `class` dla Command / Query / Web modeli
- `var` jest zakazany — zawsze explicit type
- Zawsze używaj nawiasów `{}` przy każdym bloku — nawet jednoliniowe `if`/`for`

### Handlery — cienki orkiestrator

```csharp
public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectDetailsWeb>
{
    private readonly IRepository<Project> projectRepository;
    private readonly ICurrentUser currentUser;

    public CreateProjectCommandHandler(
        IRepository<Project> projectRepository,
        ICurrentUser currentUser)
    {
        this.projectRepository = projectRepository;
        this.currentUser = currentUser;
    }

    public async Task<ProjectDetailsWeb> Handle(
        CreateProjectCommand request,
        CancellationToken cancellationToken)
    {
        Project project = BuildProject(request);
        await projectRepository.Insert(project);
        await projectRepository.SaveChangesAsync(cancellationToken);
        return MapToWeb(project);
    }

    private Project BuildProject(CreateProjectCommand request) { ... }
    private ProjectDetailsWeb MapToWeb(Project project) { ... }
}
```

Handler `Handle` zawiera wyłącznie:
1. Ładowanie i walidację danych (przez prywatne metody)
2. Wykonanie logiki biznesowej
3. Zwrócenie wyniku

Prywatne metody: `GetAndValidate{Entity}Async`, `Validate{Rule}`, `Map{Entity}To{Web}`

Gdy logika jest współdzielona przez wiele handlerów jednej domeny — wydzielaj do klasy bazowej:
```csharp
public abstract class CostTrackerHandlerBase
{
    protected async Task<CostTracker> GetAndValidateCostTrackerAsync(Guid id, ...) { ... }
}
```

---

## Walidacja (FluentValidation)

Jeden validator per command, przez MediatR pipeline (`ValidationBehavior`):

```csharp
public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
```

Pipeline behaviors (w kolejności):
1. `ValidationBehavior<TRequest, TResponse>` — sprawdza wszystkie validatory FluentValidation, rzuca `ValidationApiException` przy błędach
2. `AuthorizationBehavior<TRequest, TResponse>` — sprawdza `IAuthorizableRequest.PermissionCode` przez `AccessService`
3. `TransactionBehavior<TRequest, TResponse>` — transakcja EF Core dla Commands

---

## Autoryzacja

Kontrolery używają `[Authorize(Policy = PermissionCodes.XxxYyy)]`:

```csharp
[Authorize(Policy = PermissionCodes.ProjectEdit)]
[HttpPut("{projectId}")]
public async Task<IActionResult> UpdateProject(...)
```

`PermissionAuthorizationHandler` pobiera `tenantId` / `projectId` z route data i sprawdza uprawnienia przez `AccessService`. Kody uprawnień zdefiniowane w `Business.Interfaces.Constants.PermissionCodes`:

```
TENANT.LIST.AVAILABLE / TENANT.VIEW / TENANT.EDIT / TENANT.MEMBERS.MANAGE / TENANT.PROJECT.CREATE
PROJECT.VIEW / PROJECT.EDIT / PROJECT.MEMBERS.VIEW / PROJECT.MEMBERS.MANAGE / PROJECT.STATUS.MANAGE
PROJECT.RESOURCES.READ / PROJECT.RESOURCES.WRITE / PROJECT.RESOURCES.SHARE
PROJECT.RESOURCES.READ_SHARED / PROJECT.RESOURCES.WRITE_SHARED
PROJECT.RESOURCES.READ_ALL / PROJECT.RESOURCES.WRITE_ALL
PROJECT.MESSAGES.READ / PROJECT.MESSAGES.WRITE / PROJECT.MESSAGES.DELETE
ROLE.LIST
```

Role kody: `SYSTEM.SUPERADMIN`, `TENANT.ADMIN`, `TENANT.MEMBER`, `PROJECT.ADMIN`, `PROJECT.EDITOR`, `PROJECT.VIEWER`

---

## Obsługa błędów (`ApiExceptionMiddleware`)

Wyjątki rzucaj przez klasy z `Business.Interfaces.Exceptions`:

```csharp
throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());
throw new ValidationApiException("Błąd walidacji: ...");
throw new ForbiddenApiException();
throw new ConflictApiException("Zasób już istnieje");
```

Middleware serializuje odpowiedź:
```json
{
  "error": "NotFound",           // ApiExceptionReason jako string
  "message": "...",
  "objectType": "Project",       // opcjonalne
  "objectId": "..."              // opcjonalne
}
```

`ApiExceptionReason`: `ValidationError`, `NotFound`, `Unauthorized`, `Forbidden`, `Conflict`, `InvalidOperation`

---

## Repozytoria

Interfejs `IRepository<T>` (R/W) i `IReadRepository<T>` (tylko odczyt):

```csharp
// Pobierz z include
Project project = await projectRepository.GetFirstBySearch(
    p => p.TenantId == tenantId && p.Id == projectId,
    include => include.Include(p => p.Members))
    ?? throw new NotFoundApiException(nameof(Project), projectId.ToString());

// Projekcja (optymalizacja zapytań)
List<Guid> ids = await projectRepository.SelectAsync(
    p => p.TenantId == tenantId,
    p => p.Id);

// Bulk delete
await projectRepository.ExecuteDeleteAsync(p => p.ProjectId == projectId);

// Zapis
await projectRepository.Insert(entity);
await projectRepository.SaveChangesAsync(cancellationToken);
```

---

## Encje EF Core

Wszystkie encje dziedziczą `BaseEntity` (`Guid Id = Guid.NewGuid()`). Konfiguracja przez `IEntityTypeConfiguration<T>` w `Entities/Configurations/`.

Istniejące encje:
`Project`, `Tenant`, `TenantMember`, `TenantInvitation`, `User`, `ProjectMember`, `ProjectGroup`, `ProjectGroupMember`, `ProjectFile`, `ProjectFileVersion`, `ProjectFilePackage`, `ProjectCost`, `SharedProjectCost`, `SharedProjectFile`, `Role`, `RolePermission`, `Permission`, `WorkSchedule`, `WorkScheduleStage`, `WorkScheduleStageWork`, `WorkScheduleStageWorkAssignment`, `WorkScheduleStageWorkDependency`, `WorkSchedulePeriod`, `CostTracker` (CostTrackers/), kosztorysy (CostEstimates/), szablony (CostEstimateTemplates/), `Chat`, `ChatMember`, `MessageHistory`, `Notification`

Migracje w `Entities/Migrations/` — nazwy opisowe: `migration-{n}` lub `add-{feature}`.

---

## Web modele

Web modele to **kontrakty API** — zawsze immutable `record` z sufiksem `Web`:

```csharp
public record ProjectDetailsWeb
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
    public required string UserRoleCode { get; init; }
    public required string[] UserPermissions { get; init; }
}
```

- Nigdy nie zwracaj encji EF Core bezpośrednio
- Web model nie powinien zawierać pól technicznych (wewnętrzne ID relacji, klucze hashujące, itd.)

---

## Konwencje C# — obligatoryjne

```csharp
// ZAKAZANE — var
var project = await projectRepository.GetFirstBySearch(...);

// POPRAWNIE — explicit type
Project project = await projectRepository.GetFirstBySearch(...);
```

```csharp
// ZAKAZANE — brak nawiasów
if (project == null) throw new NotFoundApiException(...);

// POPRAWNIE
if (project == null)
{
    throw new NotFoundApiException(...);
}
```

```csharp
// ZAKAZANE — mutable class jako DTO
public class ProjectDto { public Guid Id { get; set; } }

// POPRAWNIE — immutable record
public record ProjectDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}
```

---

## appsettings.json — sekcje konfiguracyjne

```json
{
  "ConnectionStrings": { "DefaultConnection": "" },
  "AzureAdB2C": { "Instance": "", "Domain": "", "ClientId": "", "TenantId": "", "ClientSecret": "" },
  "Redis": { "ConnectionString": "", "IsEnabled": true, "DefaultExpirationMinutes": 60 },
  "BlobStorage": { "ContainerUrl": "", "QueueUrl": "" },
  "CorsSettings": { "AllowedOrigins": [] },
  "Frontend": { "BaseUrl": "", "HomePath": "/" },
  "EmailSettings": { "Provider": "SendGrid", "SendGrid": { "ApiKey": "", "DefaultFromEmail": "" } },
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } }
}
```
