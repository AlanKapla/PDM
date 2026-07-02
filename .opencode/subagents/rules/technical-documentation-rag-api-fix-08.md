# API Fix 08 — SignalR Hub + `TechnicalDocumentationController` + mapowanie endpointów

## Cel
Warstwa WebApi: hub SignalR, kontroler REST z `202 Accepted`, rejestracja w `Program.cs`.

## Decyzje MVP
- Endpointy: GET list, GET count, GET details, POST create (202), POST retry (202)
- **Brak DELETE**
- `POST create` → `AcceptedAtAction` wskazujący GET details
- Hub path: `/api/hubs/technical-documentation`

## Workspace
`C:\Users\kapla\source\repos\PDM\02-ApplicationServices\ProductDataManagementWebAPI`

## Skills
- `.cursor/skills/api-controllers/SKILL.md`

## Zależności
- **api-fix-03** (Queries)
- **api-fix-04** (Commands)
- **api-fix-07** (Dispatcher + DTO SignalR)

## Pliki referencyjne
- `src/WebApi/Hubs/NotificationHub.cs`
- `src/WebApi/Controllers/AICostController.cs` — multipart
- `src/WebApi/Program.cs` — `MapHub`

---

## 1. `ITechnicalDocumentationClient`

Plik: `src/WebApi/Hubs/ITechnicalDocumentationClient.cs`

```csharp
public interface ITechnicalDocumentationClient
{
    Task ProcessingCompleted(TechnicalDocumentationProcessingResultDto result);
}
```

## 2. `TechnicalDocumentationHub`

Plik: `src/WebApi/Hubs/TechnicalDocumentationHub.cs`

```csharp
[Authorize]
public sealed class TechnicalDocumentationHub : Hub<ITechnicalDocumentationClient>
{
    // MVP: hub pasywny — push tylko z serwera (dispatcher)
    // Opcjonalnie: OnConnectedAsync — bez grup jeśli używamy Clients.User
}
```

## 3. `TechnicalDocumentationController`

Plik: `src/WebApi/Controllers/TechnicalDocumentationController.cs`

```csharp
[ApiController]
[Route("api/tenants/{tenantId:guid}/projects/{projectId:guid}/technical-documentation")]
public sealed class TechnicalDocumentationController(IMediator mediator) : BaseApiController(mediator)
```

### Endpointy

| Method | Route | Query/Command | Response |
|--------|-------|---------------|----------|
| GET | `` | `GetTechnicalDocumentationListQuery` | 200 `List<TechnicalDocumentationListItemWeb>` |
| GET | `count` | `GetTechnicalDocumentationCountQuery` | 200 `int` |
| GET | `{id:guid}` | `GetTechnicalDocumentationDetailsQuery` | 200 `TechnicalDocumentationDetailsWeb` |
| POST | `` | `CreateTechnicalDocumentationCommand` | **202** `TechnicalDocumentationCreatedWeb` |
| POST | `{id:guid}/retry` | `RetryTechnicalDocumentationCommand` | **202** (empty body lub minimal DTO) |

### POST create — szczegóły
```csharp
[HttpPost]
[Authorize(Policy = PermissionCodes.ProjectTechnicalDocumentation)]
[RequestSizeLimit(52_428_800)]
[ProducesResponseType(typeof(TechnicalDocumentationCreatedWeb), StatusCodes.Status202Accepted)]
public async Task<IActionResult> Create(
    [FromRoute] Guid tenantId,
    [FromRoute] Guid projectId,
    [FromForm] string name,
    [FromForm] string? description,
    [FromForm] List<IFormFile> files,
    CancellationToken cancellationToken)
{
    CreateTechnicalDocumentationCommand command = new()
    {
        TenantId = tenantId,
        ProjectId = projectId,
        Name = name,
        Description = description,
        Files = files
    };
    TechnicalDocumentationCreatedWeb result = await Send(command, cancellationToken);
    return AcceptedAtAction(
        nameof(GetDetails),
        new { tenantId, projectId, id = result.Id },
        result);
}
```

### POST retry
```csharp
[HttpPost("{id:guid}/retry")]
[Authorize(Policy = PermissionCodes.ProjectTechnicalDocumentation)]
[ProducesResponseType(StatusCodes.Status202Accepted)]
public async Task<IActionResult> Retry(...)
{
    await Send(new RetryTechnicalDocumentationCommand(tenantId, projectId, id), cancellationToken);
    return Accepted();
}
```

## 4. `Program.cs`

Dodaj po istniejących hubach:
```csharp
app.MapHub<TechnicalDocumentationHub>("/api/hubs/technical-documentation")
    .RequireAuthorization();
```

## 5. Policy autoryzacji

Sprawdź czy `PermissionCodes.ProjectTechnicalDocumentation` jest zarejestrowany w `PermissionAuthorizationHandler` / seed permissions — jeśli system wymaga wpisu w tabeli `Permissions`, dodaj migrację seed lub użyj istniejącego mechanizmu (sprawdź jak dodano `PROJECT.DASHBOARD_TRACKER`).

## Weryfikacja
```powershell
dotnet build --configuration Release
dotnet test tests/WebApi.Tests --configuration Release --no-build
```
(Jeśli brak testów — sam build wystarczy)

## Test manualny (po deploy lokalnym)
1. Swagger (Development): POST multipart → 202 + `Location` header
2. GET list / count / details
3. POST retry na Failed → 202

## Koniec warstwy API
Po tym kroku API jest gotowe do integracji z UI (**ui-fix-02+**).
