# API Fix 02: Modyfikacja CostEstimateItem — direct properties, usuń FieldValues

## Kontekst
Feature: costestimate-full-refactor — patrz `.opencode/features/costestimate-full-refactor.md`

Pola podstawowe (systemowe i kalkulowane) stają się bezpośrednimi właściwościami encji `CostEstimateItem`.
Usuwamy `FieldValues` (które było `ICollection<CostEstimateItemFieldValue>`).

## Do zrobienia

### 1. Modyfikacja `CostEstimateItem.cs`

Dodaj następujące właściwości:
```csharp
// === NOWE POLA PODSTAWOWE (zamiast FieldValues) ===

/// <summary>
/// Ilość (decimal)
/// </summary>
public decimal? Quantity { get; set; }

/// <summary>
/// Jednostka miary (string) — szt, m, m², m³, kg, mb, godz, kpl
/// </summary>
public string? Unit { get; set; }

/// <summary>
/// Cena jednostkowa netto
/// </summary>
public decimal? UnitPriceNet { get; set; }

/// <summary>
/// Stawka VAT (decimal, zakres 0–1, gdzie 0.23 = 23%)
/// </summary>
public decimal? VatRate { get; set; }

/// <summary>
/// Cena jednostkowa brutto — obliczana: UnitPriceNet * (1 + VatRate)
/// </summary>
public decimal? UnitPriceGross { get; set; }

/// <summary>
/// Czy pozycja/opcja/komponent jest wybrana do sumowania:
/// - RelationType=None: checkbox do sumowania w etapie (default: true)
/// - RelationType=Option: radio button do wyboru wariantu (exclusive)
/// - RelationType=Component: checkbox do sumowania w pozycji (default: true)
/// </summary>
public bool IsSelected { get; set; } = true;

/// <summary>
/// Czy pozycja główna (RelationType=None) ma być dodana jako zakres pracy w harmonogramie.
/// Tylko dla pozycji głównych — ignorowane dla opcji i komponentów.
/// </summary>
public bool IsStageWork { get; set; } = false;
```

Usuń/wykomentuj:
- `public virtual ICollection<CostEstimateItemFieldValue> FieldValues { get; set; }` — zastąpione przez direct properties + AdditionalFieldValues

Dodaj kolekcje (które już masz z Fix-01, ale upewnij się):
```csharp
public virtual ICollection<CostEstimateAdditionalFieldValue> AdditionalFieldValues { get; set; } = new List<CostEstimateAdditionalFieldValue>();
public virtual ICollection<CostEstimateItemFile> Files { get; set; } = new List<CostEstimateItemFile>();
```

### 2. Modyfikacja `CostEstimateItemConfiguration.cs`

Dodaj konfigurację dla nowych pól:
```csharp
builder.Property(i => i.Quantity)
    .HasPrecision(18, 4);

builder.Property(i => i.Unit)
    .HasMaxLength(50);

builder.Property(i => i.UnitPriceNet)
    .HasPrecision(18, 2);

builder.Property(i => i.VatRate)
    .HasPrecision(5, 4); // 0.0000 to 9.9999

builder.Property(i => i.UnitPriceGross)
    .HasPrecision(18, 2);

builder.Property(i => i.IsSelected)
    .HasDefaultValue(true);

builder.Property(i => i.IsStageWork)
    .HasDefaultValue(false);
```

### 3. Modyfikacja `CostEstimateItemFieldValue.cs`

Nie usuwaj jeszcze tego pliku (będzie usunięty w Fix-10, żeby nie łamać builda). 
Dodaj adnotację `[Obsolete]` na klasie:
```csharp
[Obsolete("Zastąpione przez direct properties na CostEstimateItem + CostEstimateAdditionalFieldValue")]
public class CostEstimateItemFieldValue : ...
```

Analogicznie dla `CostEstimateGroupFieldValue.cs`:
```csharp
[Obsolete("Zastąpione przez direct properties na CostEstimateGroup + CostEstimateAdditionalFieldValue")]
public class CostEstimateGroupFieldValue : ...
```

### 4. Web model: `CostEstimateItemWeb` (w `CostEstimateDataWeb.cs`)

Zaktualizuj record:
```csharp
public sealed record CostEstimateItemWeb(
    Guid Id,
    Guid GroupId,
    Guid? ParentItemId,
    int RelationType,
    int Order,
    string Name,                    // Zamiast w FieldValues
    decimal? Quantity,              // NOWE — direct property
    string? Unit,                   // NOWE
    decimal? UnitPriceNet,          // NOWE
    decimal? VatRate,               // NOWE
    decimal? UnitPriceGross,        // NOWE
    decimal? NetValue,
    decimal? GrossValue,
    decimal? VatValue,
    bool IsSelected,                // NOWE
    bool IsStageWork,               // NOWE
    List<CostEstimateAdditionalFieldValueWeb> AdditionalFieldValues, // NOWE
    List<CostEstimateItemWeb>? Options,
    List<CostEstimateItemWeb>? Components,
    List<CostEstimateItemFileWeb>? Files,   // NOWE
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
```

**Usuń**: `List<CostEstimateFieldValueWeb> FieldValues` z tego rekordu.

### 5. Web model: `CostEstimateGroupWeb` (w `CostEstimateDataWeb.cs`)

Zaktualizuj:
```csharp
public sealed record CostEstimateGroupWeb(
    Guid Id,
    Guid? ParentGroupId,
    int Level,
    int Order,
    string Name,                    // Zamiast w FieldValues
    decimal? TotalNet,
    decimal? TotalGross,
    decimal? TotalVat,
    List<CostEstimateAdditionalFieldValueWeb> AdditionalFieldValues, // NOWE
    DateTime? LastCalculatedAt,
    List<CostEstimateGroupWeb> ChildGroups,
    List<CostEstimateItemWeb> Items,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
```

**Usuń**: `List<CostEstimateFieldValueWeb> FieldValues` z tego rekordu.

### 6. DTO: `CostEstimateMutationDto.cs`

Zaktualizuj `CostEstimateItemDto`:
```csharp
public sealed record CostEstimateItemDto(
    Guid? Id,
    Guid? ParentItemId,
    int RelationType,
    int Order,
    string? Name,
    decimal? Quantity,
    string? Unit,
    decimal? UnitPriceNet,
    decimal? VatRate,
    List<CostEstimateAdditionalFieldValueDto> AdditionalFieldValues, // NOWE
    List<CostEstimateItemDto>? Options,
    List<CostEstimateItemDto>? Components
);
```

Stwórz `CostEstimateAdditionalFieldValueDto`:
```csharp
public sealed record CostEstimateAdditionalFieldValueDto(
    Guid? Id,
    Guid AdditionalFieldId,
    string? StringValue,
    decimal? DecimalValue,
    bool? BoolValue,
    DateTime? DateTimeValue
);
```

### 7. `CostEstimateGroupDto`:
Zaktualizuj — usuń `fieldValues`, dodaj `additionalFieldValues`:
```csharp
public sealed record CostEstimateGroupDto(
    Guid? Id,
    Guid? ParentGroupId,
    int Level,
    int Order,
    string? Name,
    List<CostEstimateAdditionalFieldValueDto> AdditionalFieldValues,
    List<CostEstimateItemDto> Items,
    List<CostEstimateGroupDto> ChildGroups
);
```

### 8. `UpdateCostEstimateCommand.cs`

Zaktualizuj — dodaj nowe DTO zamiast starych.

### Build

```powershell
dotnet build --configuration Release
```
Jeśli build failed, przerwij i zgłoś błędy.
