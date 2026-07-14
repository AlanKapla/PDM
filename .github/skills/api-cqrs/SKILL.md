---
name: api-cqrs
description: "Tworzenie i modyfikacja Commands, Queries, Handlerów i Web modeli w warstwie CQRS. Użyj gdy tworzysz lub modyfikujesz Command, Query, Handler lub Web model (DTO)."
---

# Skill: API / CQRS

## Opis
Tworzenie i modyfikacja Commands, Queries, Handlerów i Web modeli w warstwie CQRS.

## Kiedy używać
Użyj tego skilla gdy tworzysz lub modyfikujesz Command, Query, Handler lub Web model (DTO).

---

## Lokalizacja plików

```
src/CQRS/{Domena}/{NazwaOperacji}/
  {Nazwa}Command.cs         lub {Nazwa}Query.cs
  {Nazwa}CommandHandler.cs  lub {Nazwa}QueryHandler.cs
  {Nazwa}CommandValidator.cs (opcjonalnie)
```

## Command

```csharp
// Primary constructor (preferuj dla prostych)
public record CreateProjectCommand(Guid TenantId, string Name)
    : IRequestCommand<ProjectDetailsWeb>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.TenantProjectCreate;
    public ResourceRef GetResource() => new(TenantId: TenantId);
}

// Explicit properties (gdy pola z route + body)
public sealed record UpdateProjectCommand : IRequestCommand<Unit>, IAuthorizableRequest
{
    public required Guid TenantId { get; init; }
    public required Guid ProjectId { get; init; }
    public required string Name { get; init; }
    public string PermissionCode => PermissionCodes.ProjectEdit;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
```

## Query

```csharp
public record GetProjectDetailsQuery(Guid TenantId, Guid ProjectId)
    : IRequestQuery<ProjectDetailsWeb>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.ProjectView;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
```

## Handler

```csharp
public sealed class GetProjectDetailsQueryHandler
    : IRequestHandler<GetProjectDetailsQuery, ProjectDetailsWeb>
{
    private readonly IReadRepository<Project> projectRepository;
    private readonly ICurrentUser currentUser;

    public GetProjectDetailsQueryHandler(
        IReadRepository<Project> projectRepository,
        ICurrentUser currentUser)
    {
        this.projectRepository = projectRepository;
        this.currentUser = currentUser;
    }

    public async Task<ProjectDetailsWeb> Handle(
        GetProjectDetailsQuery request,
        CancellationToken cancellationToken)
    {
        Project project = await GetAndValidateProjectAsync(
            request.TenantId, request.ProjectId, cancellationToken);

        return MapProjectToWeb(project);
    }

    private async Task<Project> GetAndValidateProjectAsync(
        Guid tenantId, Guid projectId, CancellationToken cancellationToken)
    {
        Project? project = await projectRepository.GetFirstBySearch(
            p => p.TenantId == tenantId && p.Id == projectId,
            cancellationToken);

        if (project is null)
        {
            throw new NotFoundApiException(nameof(Project), projectId.ToString());
        }

        return project;
    }

    private static ProjectDetailsWeb MapProjectToWeb(Project project) =>
        new(Id: project.Id, Name: project.Name /* ... */);
}
```

## Zasady

- Handler `sealed` zawsze
- `Handle()` to orkiestrator — max ~20 linii
- Logika w prywatnych metodach: `GetAndValidate*Async`, `Build*`, `Map*ToWeb`
- `IReadRepository<T>` gdy tylko odczyt, `IRepository<T>` gdy zapis
- Zakaz `var` — zawsze explicit type
- `is null` / `is not null` — zakaz `== null`
- Klamry `{}` przy każdym bloku
- Wspólna logika domeny → klasa bazowa `{Domain}HandlerBase`

## Web Model

```csharp
// Business/Interfaces/WebModels/{Domena}/{Nazwa}Web.cs
public record ProjectDetailsWeb(
    Guid Id,
    Guid TenantId,
    string Name,
    bool IsActive
);
// lub sealed record z required { get; init; }
```

## Wyjątki

```csharp
throw new NotFoundApiException(nameof(Project), projectId.ToString());
throw new ForbiddenApiException("Message in English.");
throw new ConflictApiException("Message in English.");
// NIE używaj InvalidOperationException jako błędu domenowego
```

## Pipeline behaviors (kolejność)

1. `ValidationBehavior` — FluentValidation
2. `AuthorizationBehavior` — `IAuthorizableRequest.PermissionCode`
3. `AssignedAuthorizationBehavior` — `IAssignedAuthorizableRequest`
4. `TransactionBehavior` — transakcja EF Core dla Commands
