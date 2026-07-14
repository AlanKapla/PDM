# API Fix 04: CQRS dla schema pól dodatkowych (CRUD)

## Kontekst
Feature: costestimate-full-refactor — patrz `.opencode/features/costestimate-full-refactor.md`

Nowe endpointy i CQRS dla zarządzania polami dodatkowymi na kosztorysie.
Zastępuje stare `AddFieldDefinition`/`UpdateFieldDefinition`/`DeleteFieldDefinition`/`ReorderFieldDefinitions`.

## Do zrobienia

### 1. Commands/Queries

#### `GetAdditionalFieldsQuery`
```csharp
public sealed record GetAdditionalFieldsQuery : IRequest<List<CostEstimateAdditionalFieldWeb>>
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid CostEstimateId { get; init; }
}
```
Handler: pobiera wszystkie `CostEstimateAdditionalField` dla danego kosztorysu, sortuje po `Order`.

#### `AddAdditionalFieldCommand`
```csharp
public sealed record AddAdditionalFieldCommand : IRequest<Guid>
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid CostEstimateId { get; init; }
    public string Name { get; init; } = default!;
    public AdditionalFieldType FieldType { get; init; }
    public int? Order { get; init; } // Jeśli null, dodaj na koniec
}
```
Handler:
- Sprawdź czy kosztorys istnieje (NotFoundApiException jeśli nie)
- Jeśli Order == null, ustaw Order = max istniejący + 1
- Utwórz `CostEstimateAdditionalField`
- Zapisz
- Zwróć Id

Walidator (`AddAdditionalFieldCommandValidator`):
- Name: not empty, max 256 znaków
- FieldType: musi być poprawnym AdditionalFieldType (0-3)

#### `UpdateAdditionalFieldCommand`
```csharp
public sealed record UpdateAdditionalFieldCommand : IRequest
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid CostEstimateId { get; init; }
    public Guid FieldId { get; init; }
    public string? Name { get; init; }
    public AdditionalFieldType? FieldType { get; init; }
    public int? Order { get; init; }
}
```
Handler:
- Znajdź pole (NotFoundApiException jeśli nie istnieje)
- Zaktualizuj tylko nie-null properties
- Zapisz

#### `DeleteAdditionalFieldCommand`
```csharp
public sealed record DeleteAdditionalFieldCommand : IRequest
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid CostEstimateId { get; init; }
    public Guid FieldId { get; init; }
}
```
Handler:
- Znajdź pole
- Usuń fizycznie (bez soft delete — to tylko definicja)
- Usuń też wszystkie wartości tego pola (`CostEstimateAdditionalFieldValue`) — kaskada
- Reorganizuj Order pozostałych pól

#### `ReorderAdditionalFieldsCommand`
```csharp
public sealed record ReorderAdditionalFieldsCommand : IRequest
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid CostEstimateId { get; init; }
    public List<Guid> FieldIds { get; init; } = default!; // Kolejność ID pól
}
```
Handler:
- Pobierz wszystkie pola dla kosztorysu
- Dla każdego ID w liście, ustaw Order = index na liście
- Zapisz

### 2. Kontroler — nowe endpointy

Dodaj do `CostEstimateController.cs`:

```csharp
// ========================================================================
// ADDITIONAL FIELDS (schema) ENDPOINTS
// ========================================================================

/// <summary>
/// Pobierz wszystkie pola dodatkowe kosztorysu
/// </summary>
[HttpGet("{id:guid}/additional-fields")]
[Authorize(Policy = PermissionCodes.ProjectEstimates)]
public async Task<IActionResult> GetAdditionalFields(...)
{
    var query = new GetAdditionalFieldsQuery { ... };
    return Ok(await Send(query));
}

/// <summary>
/// Dodaj nowe pole dodatkowe do kosztorysu
/// </summary>
[HttpPost("{id:guid}/additional-fields")]
[Authorize(Policy = PermissionCodes.ProjectEstimates)]
public async Task<IActionResult> AddAdditionalField(...)
{
    var command = ...;
    var fieldId = await Send(command);
    return CreatedAtAction(nameof(GetCostEstimateDetails), new { tenantId, projectId, id }, fieldId);
}

/// <summary>
/// Edytuj pole dodatkowe
/// </summary>
[HttpPut("{id:guid}/additional-fields/{fieldId:guid}")]
[Authorize(Policy = PermissionCodes.ProjectEstimates)]
public async Task<IActionResult> UpdateAdditionalField(...)
{
    await Send(command);
    return NoContent();
}

/// <summary>
/// Usuń pole dodatkowe
/// </summary>
[HttpDelete("{id:guid}/additional-fields/{fieldId:guid}")]
[Authorize(Policy = PermissionCodes.ProjectEstimates)]
public async Task<IActionResult> DeleteAdditionalField(...)
{
    await Send(command);
    return NoContent();
}

/// <summary>
/// Zmień kolejność pól dodatkowych
/// </summary>
[HttpPost("{id:guid}/additional-fields/reorder")]
[Authorize(Policy = PermissionCodes.ProjectEstimates)]
public async Task<IActionResult> ReorderAdditionalFields(...)
{
    await Send(command);
    return NoContent();
}
```

### 3. Usuń stare endpointy schema

**NIE usuwaj jeszcze** starych endpointów (AddFieldDefinition, UpdateFieldDefinition, DeleteFieldDefinition, ReorderFieldDefinitions) — zostaną usunięte w Fix-10.

Po prostu dodaj nowe endpointy.

### Build

```powershell
dotnet build --configuration Release
```
Jeśli build failed, przerwij i zgłoś błędy.
