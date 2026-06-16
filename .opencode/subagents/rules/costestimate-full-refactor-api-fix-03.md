# API Fix 03: Modyfikacja CostEstimateGroup — usuń FieldValues

## Kontekst
Feature: costestimate-full-refactor — patrz `.opencode/features/costestimate-full-refactor.md`

Po Fix-02, teraz modyfikujemy `CostEstimateGroup` — usuwamy `FieldValues` i dodajemy `AdditionalFieldValues`.

## Do zrobienia

### 1. Modyfikacja `CostEstimateGroup.cs`

```csharp
// Usuń:
// public virtual ICollection<CostEstimateGroupFieldValue> FieldValues { get; set; } = new List<CostEstimateGroupFieldValue>();

// Dodaj (jeśli jeszcze nie ma z Fix-01):
public virtual ICollection<CostEstimateAdditionalFieldValue> AdditionalFieldValues { get; set; } = new List<CostEstimateAdditionalFieldValue>();
```

### 2. Modyfikacja `CostEstimateGroupConfiguration.cs`

Usuń konfigurację dla `FieldValues` (has many).
Jeśli była konfiguracja relacji do `CostEstimateGroupFieldValue`, usuń ją.

Dodaj konfigurację `HasMany(g => g.AdditionalFieldValues)`:
```csharp
builder.HasMany(g => g.AdditionalFieldValues)
    .WithOne(v => v.Group)
    .HasForeignKey(v => v.GroupId)
    .OnDelete(DeleteBehavior.Cascade);
```

### Build

```powershell
dotnet build --configuration Release
```
Jeśli build failed, przerwij i zgłoś błędy.
