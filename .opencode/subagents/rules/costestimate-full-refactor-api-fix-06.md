# API Fix 06: SetItemIsSelected handler — auto-deselect dla opcji

## Kontekst
Feature: costestimate-full-refactor — patrz `.opencode/features/costestimate-full-refactor.md`

Nowy endpoint do zmiany `IsSelected` na itemie z auto-deselect dla opcji exclusive.

## Do zrobienia

### 1. Nowy Command: `SetItemIsSelectedCommand`

```csharp
public sealed record SetItemIsSelectedCommand : IRequest
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid CostEstimateId { get; init; }
    public Guid ItemId { get; init; }
    public bool IsSelected { get; init; }
}
```

### 2. Handler: `SetItemIsSelectedCommandHandler`

Logika:

1. **Znajdź item** — `GetItemAsync(itemId)`, NotFoundApiException jeśli nie istnieje

2. **Sprawdź RelationType**:
   - `RelationType.None (pozycja główna)`:
     - Ustaw `IsSelected` na wartość z requestu
     - Jeśli odznaczono (IsSelected=false), item nie będzie sumowany do grupy
   - `RelationType.Option (opcja)`:
     - Jeśli zaznaczono (IsSelected=true):
       - Znajdź wszystkie opcje rodzica (te same ParentItemId)
       - Ustaw `IsSelected = false` dla wszystkich opcji (auto-deselect)
       - Ustaw `IsSelected = true` dla tej opcji
       - **Kopiuj wartości finansowe** z opcji do rodzica:
         - Skopiuj Quantity, Unit, UnitPriceNet, VatRate z opcji do rodzica
         - Rodzic.ParentItemId wskazuje na pozycję nadrzędną
     - Jeśli odznaczono (IsSelected=false):
       - Jeśli to była jedyna zaznaczona opcja, przywróć oryginalne wartości rodzica
       - **Oryginalne wartości rodzica**: muszą być zachowane. Proponuję dodać tymczasowe pola `OriginalQuantity`, `OriginalUnitPriceNet` itp. na encji, lub przechowywać w cache.
       
       **Alternatywnie**: odznaczenie opcji przywraca wartości rodzica do NULL (user musiałby wpisać od nowa). To prostsze.
       
       Decyzja: **przywracamy ostatnie znane wartości rodzica** zapisane w handlerze. Jeśli nie ma zapisanych → wartości zostają NULL.
   - `RelationType.Component (komponent)`:
     - Ustaw `IsSelected` na wartość z requestu
     - Jeśli odznaczono, komponent nie jest sumowany do rodzica

3. **Po zmianie**:
   - Ustaw `UpdatedAt = DateTime.UtcNow`
   - Zapisz w bazie
   - Wywołaj `RecalculateCostEstimate` dla całego kosztorysu

### 3. Obsługa auto-deselect dla opcji (szczegóły)

W handlerze, gdy `RelationType == Option && IsSelected == true`:

```csharp
// Znajdź wszystkie opcje rodzica
var parentItem = await GetItemAsync(item.ParentItemId!.Value);
var allOptions = await GetOptionsAsync(parentItem.Id);

// Odznacz wszystkie
foreach (var option in allOptions)
{
    if (option.Id != itemId)
    {
        option.IsSelected = false;
    }
}

// Zaznacz tę opcję
item.IsSelected = true;

// Zapisz oryginalne wartości rodzica (jeśli nie są jeszcze zapisane)
// i skopiuj wartości z opcji do rodzica
parentItem.Quantity = item.Quantity;
parentItem.Unit = item.Unit;
parentItem.UnitPriceNet = item.UnitPriceNet;
parentItem.VatRate = item.VatRate;
// Pozostałe będą obliczone przez Recalculate
```

Gdy opcja jest odznaczana:
```csharp
// Jeśli odznaczamy ostatnią zaznaczoną opcję
item.IsSelected = false;

// Sprawdź czy żadna inna opcja nie jest zaznaczona
bool otherSelected = allOptions.Any(o => o.Id != itemId && o.IsSelected);
if (!otherSelected)
{
    // Przywróć wartości rodzica do NULL (niech user wpisze od nowa)
    parentItem.Quantity = null;
    parentItem.UnitPriceNet = null;
    // ...inne pola
}
```

### 4. Walidator

```csharp
public sealed class SetItemIsSelectedCommandValidator : AbstractValidator<SetItemIsSelectedCommand>
{
    public SetItemIsSelectedCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
    }
}
```

### 5. Kontroler — nowy endpoint

Dodaj do `CostEstimateController.cs`:

```csharp
/// <summary>
/// Zmień IsSelected dla pozycji/opcji/komponentu
/// Dla opcji: auto-deselect pozostałych opcji (exclusive)
/// Dla pozycji/komponentu: zmiana checkboxa do sumowania
/// </summary>
[HttpPatch("{id:guid}/items/{itemId:guid}/select")]
[Authorize(Policy = PermissionCodes.ProjectEstimates)]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> SetItemIsSelected(
    [FromRoute] Guid tenantId,
    [FromRoute] Guid projectId,
    [FromRoute] Guid id,
    [FromRoute] Guid itemId,
    [FromBody] SetItemIsSelectedCommand command)
{
    command = command with
    {
        TenantId = tenantId,
        ProjectId = projectId,
        CostEstimateId = id,
        ItemId = itemId
    };
    
    await Send(command);
    return NoContent();
}
```

### 6. Obsługa w UI — optimistic update

Endpoint zwraca `204 NoContent`. UI po otrzymaniu odpowiedzi aktualizuje local state.
Przy błędzie (np. 404) UI przywraca poprzednią wartość.

### Build

```powershell
dotnet build --configuration Release
```
Jeśli build failed, przerwij i zgłoś błędy.
