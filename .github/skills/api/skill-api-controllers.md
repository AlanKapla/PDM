# Skill: API / Kontrolery

## Opis
Tworzenie i modyfikacja kontrolerów ASP.NET Core — routing, HTTP status, autoryzacja.

## Kiedy używać
Użyj tego skilla gdy tworzysz lub modyfikujesz kontroler (*Controller.cs) lub endpoint.

---

## Lokalizacja

```
src/WebApi/Controllers/{Domena}Controller.cs
```

## Wzorzec

```csharp
[Route("api/tenants/{tenantId}/projects")]
[ApiController]
public sealed class ProjectController(ISender sender) : BaseApiController(sender)
{
    [HttpGet("{projectId}")]
    [Authorize(Policy = PermissionCodes.ProjectView)]
    [ProducesResponseType(typeof(ProjectDetailsWeb), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectDetails(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        GetProjectDetailsQuery query = new(tenantId, projectId);
        ProjectDetailsWeb result = await Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.TenantProjectCreate)]
    [ProducesResponseType(typeof(ProjectDetailsWeb), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProject(
        [FromRoute] Guid tenantId,
        [FromBody] CreateProjectCommand command,
        CancellationToken cancellationToken)
    {
        command = command with { TenantId = tenantId };
        ProjectDetailsWeb result = await Send(command, cancellationToken);
        return CreatedAtAction(
            nameof(GetProjectDetails),
            new { tenantId, projectId = result.Id },
            result);
    }

    [HttpPut("{projectId}")]
    [Authorize(Policy = PermissionCodes.ProjectEdit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProject(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid projectId,
        [FromBody] UpdateProjectCommand command,
        CancellationToken cancellationToken)
    {
        command = command with { TenantId = tenantId, ProjectId = projectId };
        await Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{projectId}")]
    [Authorize(Policy = PermissionCodes.ProjectEdit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteProject(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        DeleteProjectCommand command = new()
        {
            TenantId = tenantId,
            ProjectId = projectId
        };
        await Send(command, cancellationToken);
        return NoContent();
    }
}
```

## Routing konwencje

```
GET    /api/tenants/{tenantId}/projects              → lista
GET    /api/tenants/{tenantId}/projects/{projectId}  → szczegóły
POST   /api/tenants/{tenantId}/projects              → tworzenie → 201
PUT    /api/tenants/{tenantId}/projects/{projectId}  → aktualizacja → 204
DELETE /api/tenants/{tenantId}/projects/{projectId}  → usunięcie → 204
PATCH  /api/tenants/{tenantId}/projects/{projectId}/status → częściowa zmiana → 204
```

## HTTP status konwencje

| Operacja | Kod |
|----------|-----|
| GET (lista/szczegóły) | 200 OK |
| POST (tworzenie) | 201 Created + `CreatedAtAction` |
| PUT/PATCH (bez body) | 204 NoContent |
| DELETE | 204 NoContent |
| POST (akcja bez zasobu) | 204 NoContent |

## Zasady

- Kontroler zawsze `sealed`
- Jeden `[Authorize(Policy = ...)]` per endpoint — używaj `PermissionCodes.*`
- Parametry z route przez `[FromRoute]`, body przez `[FromBody]`
- Parametry route → Command przez `command with { TenantId = tenantId }`
- Zakaz logiki biznesowej w kontrolerze — tylko routing do MediatR
- `ProducesResponseType` dla 200/201 i błędów (400, 404)
