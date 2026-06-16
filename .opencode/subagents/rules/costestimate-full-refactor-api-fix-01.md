# API Fix 01: Nowe encje — CostEstimateAdditionalField, CostEstimateAdditionalFieldValue, CostEstimateItemFile

## Kontekst
Feature: costestimate-full-refactor — patrz `.opencode/features/costestimate-full-refactor.md`

Usuwamy starą strukturę FieldDefinition/FieldValue. Wprowadzamy nową, płaską strukturę dla pól dodatkowych i plików.

## Do zrobienia

### 1. Nowy enum: `AdditionalFieldType` w `CostEstimateEnums.cs`

```csharp
public enum AdditionalFieldType
{
    String = 0,
    Decimal = 1,
    Boolean = 2,
    DateTime = 3
}
```

### 2. Nowa encja: `CostEstimateAdditionalField`

Utwórz w `Entities/Models/CostEstimates/CostEstimateAdditionalField.cs`:

```csharp
public class CostEstimateAdditionalField : BaseEntity
{
    public Guid CostEstimateId { get; set; }
    public string Name { get; set; } = default!; // e.g. "Kod CPV", "Uwagi"
    public AdditionalFieldType FieldType { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual CostEstimate CostEstimate { get; set; } = default!;
    public virtual ICollection<CostEstimateAdditionalFieldValue> Values { get; set; } = new List<CostEstimateAdditionalFieldValue>();
}
```

### 3. Nowa encja: `CostEstimateAdditionalFieldValue`

Utwórz w `Entities/Models/CostEstimates/CostEstimateAdditionalFieldValue.cs`:

```csharp
public class CostEstimateAdditionalFieldValue : BaseEntity
{
    public Guid AdditionalFieldId { get; set; }
    public Guid? GroupId { get; set; } // Wartość dla grupy (nullable)
    public Guid? ItemId { get; set; }  // Wartość dla pozycji (nullable)
    
    // Typowane wartości (tylko jedna wypełniona, zależnie od AdditionalFieldType)
    public string? StringValue { get; set; }
    public decimal? DecimalValue { get; set; }
    public bool? BoolValue { get; set; }
    public DateTime? DateTimeValue { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual CostEstimateAdditionalField AdditionalField { get; set; } = default!;
    public virtual CostEstimateGroup? Group { get; set; }
    public virtual CostEstimateItem? Item { get; set; }
}
```

**Ważne**: Tylko jedna z wartości (StringValue/DecimalValue/BoolValue/DateTimeValue) powinna być wypełniona, zgodnie z `AdditionalFieldType` definicji pola.

### 4. Nowa encja: `CostEstimateItemFile`

Utwórz w `Entities/Models/CostEstimates/CostEstimateItemFile.cs`:

```csharp
public class CostEstimateItemFile : DeletableEntity
{
    public Guid ItemId { get; set; }
    public Guid CostEstimateId { get; set; } // Denormalizacja
    
    public string OriginalFileName { get; set; } = default!;
    public string BlobName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long FileSize { get; set; }
    public int Order { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }

    // Navigation
    public virtual CostEstimateItem Item { get; set; } = default!;
    public virtual CostEstimate CostEstimate { get; set; } = default!;
    public virtual User CreatedByUser { get; set; } = default!;
}
```

### 5. Konfiguracje EF

Dla każdej nowej encji utwórz konfigurację w `Entities/Configurations/`:

**CostEstimateAdditionalFieldConfiguration.cs**:
- Table: `CostEstimateAdditionalFields`
- Required: `Name` (max 256)
- Index na `CostEstimateId` + `Order`
- Relacja: `HasMany(f => f.Values).WithOne(v => v.AdditionalField).HasForeignKey(v => v.AdditionalFieldId)`

**CostEstimateAdditionalFieldValueConfiguration.cs**:
- Table: `CostEstimateAdditionalFieldValues`
- Optional: StringValue max 4000
- Index na `AdditionalFieldId`, `GroupId`, `ItemId`
- Relacje: nullable do Group i Item

**CostEstimateItemFileConfiguration.cs**:
- Table: `CostEstimateItemFiles`
- Required: OriginalFileName (max 512), BlobName (max 1024), ContentType (max 128)
- Index na `ItemId`, `CostEstimateId`
- Relacja: `HasOne(f => f.Item).WithMany(i => i.Files).HasForeignKey(f => f.ItemId)`

### 6. Web models (DTOs)

Utwórz w `Business/Interfaces/WebModels/CostEstimates/`:

**CostEstimateAdditionalFieldWeb.cs**:
```csharp
public sealed record CostEstimateAdditionalFieldWeb(
    Guid Id,
    Guid CostEstimateId,
    string Name,
    int FieldType, // AdditionalFieldType as int
    int Order,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
```

**CostEstimateAdditionalFieldValueWeb.cs**:
```csharp
public sealed record CostEstimateAdditionalFieldValueWeb(
    Guid Id,
    Guid AdditionalFieldId,
    string? StringValue,
    decimal? DecimalValue,
    bool? BoolValue,
    DateTime? DateTimeValue
);
```

**CostEstimateItemFileWeb.cs** (zastępuje CostEstimateFieldFileWeb):
```csharp
public sealed record CostEstimateItemFileWeb(
    Guid Id,
    Guid ItemId,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    int Order,
    string? SasUriPreview,
    string? SasUriDownload,
    DateTime CreatedAt
);
```

### 7. Dodaj nawigacje do istniejących encji

- `CostEstimate.cs` — dodaj `public virtual ICollection<CostEstimateAdditionalField> AdditionalFields { get; set; } = new List<CostEstimateAdditionalField>();`
- `CostEstimateItem.cs` — dodaj `public virtual ICollection<CostEstimateAdditionalFieldValue> AdditionalFieldValues { get; set; } = new List<CostEstimateAdditionalFieldValue>();` i `public virtual ICollection<CostEstimateItemFile> Files { get; set; } = new List<CostEstimateItemFile>();`
- `CostEstimateGroup.cs` — dodaj `public virtual ICollection<CostEstimateAdditionalFieldValue> AdditionalFieldValues { get; set; } = new List<CostEstimateAdditionalFieldValue>();`

### 8. Rejestracja w DbContext

Entity Framework: w `AppDbContext.cs` dodaj:
```csharp
public DbSet<CostEstimateAdditionalField> CostEstimateAdditionalFields => Set<CostEstimateAdditionalField>();
public DbSet<CostEstimateAdditionalFieldValue> CostEstimateAdditionalFieldValues => Set<CostEstimateAdditionalFieldValue>();
public DbSet<CostEstimateItemFile> CostEstimateItemFiles => Set<CostEstimateItemFile>();
```

### Build

Po zakończeniu uruchom:
```powershell
dotnet build --configuration Release
```
Jeśli build failed, przerwij i zgłoś błędy.
