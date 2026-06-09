# ProjectCost — Fix 02: Commands / Queries / WebModels — struktura

Cel: ujednolicić wszystkie Commands/Queries domeny do wzorca `sealed record` z `required { get; init; }` oraz uporządkować Web modele i nieużywane zależności.

## Zakres zmian

### 1. W1 + W2 — Wszystkie Commands/Queries: `sealed record` + `required init`

Pliki:
- `src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommand.cs`
- `src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommand.cs`
- `src/CQRS/ProjectCosts/DeleteProjectCost/DeleteProjectCostCommand.cs` (positional → explicit)
- `src/CQRS/ProjectCosts/ShareProjectCosts/ShareProjectCostsCommand.cs`
- `src/CQRS/ProjectCosts/UpdateCostShare/UpdateCostShareCommand.cs`
- `src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQuery.cs` (positional → explicit)

Wzorzec docelowy:

```csharp
public sealed record CreateProjectCostCommand
    : IRequestCommand<Guid>, IAuthorizableRequest
{
    public required Guid TenantId { get; init; }
    public required Guid ProjectId { get; init; }
    public required string Name { get; init; }
    public string? Place { get; init; }
    public DateTime? Date { get; init; }
    public string? Description { get; init; }
    public decimal? NetAmount { get; init; }
    public decimal? GrossAmount { get; init; }
    public bool IsAccepted { get; init; }
    public DocumentUploadDto? Document { get; init; }

    public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
```

Reguły:
- `sealed record`, brak positional params.
- Identyfikatory (`TenantId`, `ProjectId`, `CostId`, ewentualne `Name` w Create/Update jeżeli wymagane biznesowo) oznaczyć `required`.
- Pola opcjonalne (`Place`, `Description`, `Date`, `Document`) — bez `required`, typy nullable (`string?`, `DateTime?`).
- Listy (`ProjectCostIds`, `SharedWithUserIds`) oznaczyć `required`, bez wartości domyślnej `= new()`.
- Zachować istniejące interfejsy (`IRequestCommand<...>`, `IRequestQuery<...>`, `IAuthorizableRequest`, `IGetResourceScope` jeżeli był).
- `PermissionCode` — bez zmian (UpdateCostShare pozostaje na `ProjectResourcesWrite` zgodnie z decyzją).

### 2. N7 — Usunąć `SharedProjectCostWeb`

Plik: `src/Business/Interfaces/WebModels/ProjectCosts/SharedProjectCostWeb.cs` — **usunąć**.
Zweryfikować przez search (`SharedProjectCostWeb`) brak referencji; jeśli są (np. zapomniany using) — usunąć.

### 3. N1 — `ProjectCostListItemWeb`: sealed + required

Plik: `src/Business/Interfaces/WebModels/ProjectCosts/ProjectCostListItemWeb.cs`

Wzorzec:
```csharp
public sealed record ProjectCostListItemWeb
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    // opcjonalne: nullable bez required
    public string? Place { get; init; }
    public DateTime? Date { get; init; }
    // listy required, bez = new()
    public required IReadOnlyList<Guid> SharedWithUserIds { get; init; }
    // ...
}
```

Po zmianie — zaktualizować mapowanie w `GetProjectCostsQueryHandler` (object initializer) tak, aby ustawiało wszystkie `required` properties.

### 4. W11 — Usunąć nieużywany `projectRepo` z UpdateCostShare

Plik: `src/CQRS/ProjectCosts/UpdateCostShare/UpdateCostShareCommandHandler.cs`

Usunąć pole/parametr konstruktora `IRepository<Project> projectRepo` jeśli nie jest używany w żadnej metodzie. Usunąć odpowiedni `using`.

## Wymagania techniczne

- Zakaz `var`.
- Build: `dotnet build src\WebApi\WebApi.csproj` w `02-ApplicationServices/ProductDataManagementWebAPI`.
- Sprawdzić czy controller (`ProjectCostController`) i wszystkie miejsca wywołań `Send(new XxxCommand { ... })` zostały zaktualizowane (object initializer z `required`).
- Zwrócić raport: status buildu, lista plików, blokery.
