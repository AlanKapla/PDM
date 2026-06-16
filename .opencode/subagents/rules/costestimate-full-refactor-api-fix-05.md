# API Fix 05: CQRS dla wartości pól dodatkowych (upsert)

## Kontekst
Feature: costestimate-full-refactor — patrz `.opencode/features/costestimate-full-refactor.md`

Nowe endpointy do zapisywania wartości pól dodatkowych dla grup i pozycji.
Zastępuje stare `UpsertCostEstimateGroupField` / `UpsertCostEstimateItemField`.

## Do zrobienia

### 1. Nowy DTO: `UpsertAdditionalFieldValueCommand`

Stwórz jeden wspólny command dla grup i pozycji:

```csharp
public sealed record UpsertAdditionalFieldValueCommand : IRequest<Guid>
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid CostEstimateId { get; init; }
    public Guid AdditionalFieldId { get; init; }
    public Guid? GroupId { get; init; } // Null dla pozycji
    public Guid? ItemId { get; init; }  // Null dla grup
    public string? StringValue { get; init; }
    public decimal? DecimalValue { get; init; }
    public bool? BoolValue { get; init; }
    public DateTime? DateTimeValue { get; init; }
}
```

### 2. Handler

Utwórz `UpsertAdditionalFieldValueCommandHandler`:

1. **Walidacja**: sprawdź czy `AdditionalFieldId` istnieje i należy do tego kosztorysu
2. **Sprawdź typ wartości**: 
   - Jeśli FieldType == String → ustaw StringValue
   - Jeśli FieldType == Decimal → ustaw DecimalValue
   - Jeśli FieldType == Boolean → ustaw BoolValue
   - Jeśli FieldType == DateTime → ustaw DateTimeValue
3. **Upsert**:
   - Szukaj istniejącej wartości: `FirstOrDefault(v => v.AdditionalFieldId == fieldId && v.GroupId == groupId && v.ItemId == itemId)`
   - Jeśli istnieje → update wartości
   - Jeśli nie istnieje → create nową z odpowiednim typem wartości
4. **Zapisz**
5. **Trigger recalculation** — wywołaj `RecalculateCostEstimate` (tylko jeśli ItemId != null, bo zmiana wartości na itemie może wpłynąć na kalkulacje)

### 3. Walidator

```csharp
public sealed class UpsertAdditionalFieldValueCommandValidator : AbstractValidator<UpsertAdditionalFieldValueCommand>
{
    public UpsertAdditionalFieldValueCommandValidator()
    {
        RuleFor(x => x.AdditionalFieldId).NotEmpty();
        
        // Jedno z GroupId/ItemId musi być ustawione, ale nie oba
        RuleFor(x => x)
            .Must(x => (x.GroupId.HasValue || x.ItemId.HasValue) && !(x.GroupId.HasValue && x.ItemId.HasValue))
            .WithMessage("Musisz podać GroupId lub ItemId, ale nie oba");
    }
}
```

### 4. Kontroler — nowe endpointy

Dodaj do `CostEstimateController.cs`:

```csharp
/// <summary>
/// Zapisz wartość pola dodatkowego dla grupy
/// </summary>
[HttpPatch("{id:guid}/groups/{groupId:guid}/additional-fields")]
[Authorize(Policy = PermissionCodes.ProjectEstimates)]
public async Task<IActionResult> UpsertGroupAdditionalField(...)
{
    command = command with { CostEstimateId = id, GroupId = groupId };
    var valueId = await Send(command);
    return Ok(valueId);
}

/// <summary>
/// Zapisz wartość pola dodatkowego dla pozycji
/// </summary>
[HttpPatch("{id:guid}/items/{itemId:guid}/additional-fields")]
[Authorize(Policy = PermissionCodes.ProjectEstimates)]
public async Task<IActionResult> UpsertItemAdditionalField(...)
{
    command = command with { CostEstimateId = id, ItemId = itemId };
    var valueId = await Send(command);
    return Ok(valueId);
}
```

### 5. Update podstawowych pól pozycji/grupy

Dodaj też prosty endpoint do update'u podstawowych pól (name, quantity, unit, price, vat):

```csharp
public sealed record UpdateItemBaseFieldsCommand : IRequest
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid CostEstimateId { get; init; }
    public Guid ItemId { get; init; }
    public string? Name { get; init; }
    public decimal? Quantity { get; init; }
    public string? Unit { get; init; }
    public decimal? UnitPriceNet { get; init; }
    public decimal? VatRate { get; init; }
}
```

Handler:
- Znajdź item
- Dla każdego nie-null property, zaktualizuj
- Trigger recalculation jeśli zmieniono pole finansowe

Kontroler:
```csharp
[HttpPatch("{id:guid}/items/{itemId:guid}")]
[Authorize(Policy = PermissionCodes.ProjectEstimates)]
public async Task<IActionResult> UpdateItemBaseFields(...)
```

Analogicznie dla grupy:
```csharp
[HttpPatch("{id:guid}/groups/{groupId:guid}")]
[Authorize(Policy = PermissionCodes.ProjectEstimates)]
public async Task<IActionResult> UpdateGroupBaseFields(...)
```
(Grupa ma tylko Name jako editable base field)

### Build

```powershell
dotnet build --configuration Release
```
Jeśli build failed, przerwij i zgłoś błędy.
