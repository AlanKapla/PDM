# API Fix 08: Aktualizacja handlerów CQRS do nowej struktury

## Kontekst
Feature: costestimate-full-refactor — patrz `.opencode/features/costestimate-full-refactor.md`

Po zmianie encji i dodaniu nowych endpointów, trzeba zaktualizować wszystkie istniejące handlery CQRS.
Zmiany wynikają z: (1) braku FieldValues, (2) direct properties, (3) AdditionalFieldValues, (4) nowego web modelu.

## Do zrobienia

### 1. `GetCostEstimateDetailsQueryHandler.cs`

**Kluczowa zmiana** — mapowanie z nowej struktury:

```csharp
// Mapowanie grupy — bez FieldValues
private static CostEstimateGroupWeb MapGroup(CostEstimateGroup group)
{
    return new CostEstimateGroupWeb(
        Id: group.Id,
        ParentGroupId: group.ParentGroupId,
        Level: group.Level,
        Order: group.Order,
        Name: group.Name,  // Zamiast z FieldValues
        TotalNet: group.TotalNet,
        TotalGross: group.TotalGross,
        TotalVat: group.TotalVat,
        AdditionalFieldValues: group.AdditionalFieldValues?.Select(MapAdditionalFieldValue).ToList() ?? new(),
        LastCalculatedAt: group.LastCalculatedAt,
        ChildGroups: group.ChildGroups?.Where(g => !g.IsDeleted).Select(MapGroup).ToList() ?? new(),
        Items: group.Items?.Where(i => !i.IsDeleted).Select(MapItem).ToList() ?? new(),
        CreatedAt: group.CreatedAt,
        UpdatedAt: group.UpdatedAt
    );
}

// Mapowanie pozycji — direct properties zamiast FieldValues
private static CostEstimateItemWeb MapItem(CostEstimateItem item)
{
    return new CostEstimateItemWeb(
        Id: item.Id,
        GroupId: item.GroupId,
        ParentItemId: item.ParentItemId,
        RelationType: (int)item.RelationType,
        Order: item.Order,
        Name: item.Name,
        Quantity: item.Quantity,
        Unit: item.Unit,
        UnitPriceNet: item.UnitPriceNet,
        VatRate: item.VatRate,
        UnitPriceGross: item.UnitPriceGross,
        NetValue: item.NetValue,
        GrossValue: item.GrossValue,
        VatValue: item.VatValue,
        IsSelected: item.IsSelected,
        IsStageWork: item.IsStageWork,
        AdditionalFieldValues: item.AdditionalFieldValues?.Select(MapAdditionalFieldValue).ToList() ?? new(),
        Options: item.Options?.Where(o => !o.IsDeleted).Select(MapItem).ToList(),
        Components: item.Components?.Where(c => !c.IsDeleted).Select(MapItem).ToList(),
        Files: item.Files?.Where(f => !f.IsDeleted).Select(MapItemFile).ToList(),
        CreatedAt: item.CreatedAt,
        UpdatedAt: item.UpdatedAt
    );
}

// Mapowanie wartości dodatkowego pola
private static CostEstimateAdditionalFieldValueWeb MapAdditionalFieldValue(CostEstimateAdditionalFieldValue v)
{
    return new CostEstimateAdditionalFieldValueWeb(
        Id: v.Id,
        AdditionalFieldId: v.AdditionalFieldId,
        StringValue: v.StringValue,
        DecimalValue: v.DecimalValue,
        BoolValue: v.BoolValue,
        DateTimeValue: v.DateTimeValue
    );
}

// Mapowanie pliku
private static CostEstimateItemFileWeb MapItemFile(CostEstimateItemFile f)
{
    return new CostEstimateItemFileWeb(
        Id: f.Id,
        ItemId: f.ItemId,
        OriginalFileName: f.OriginalFileName,
        ContentType: f.ContentType,
        FileSize: f.FileSize,
        Order: f.Order,
        SasUriPreview: null, // Generuj SAS URI jeśli masz blob storage
        SasUriDownload: null,
        CreatedAt: f.CreatedAt
    );
}
```

Również w `CostEstimateDetailsWeb` — dodaj pole na dodatkowe pola:
```csharp
List<CostEstimateAdditionalFieldWeb> AdditionalFields,  // Schema pól dodatkowych
```

### 2. `AddCostEstimateItemCommandHandler.cs`

- Ustaw nowe properties: `IsSelected = true` (dla None/Component), `IsStageWork = false`
- Nie twórz FieldValues — nie są już potrzebne
- Zachowaj tworzenie `AdditionalFieldValues` jeśli przesłane w DTO

### 3. `AddCostEstimateGroupCommandHandler.cs`

- Ustaw `Name` bezpośrednio (nie przez FieldValues)
- Zachowaj możliwość tworzenia z `AdditionalFieldValues`

### 4. `DeleteCostEstimateGroupCommandHandler.cs`

- Usuń hard-delete item field values (nie ma już FieldValues)
- Zachowaj soft-delete grup i itemów
- Dodaj soft-delete plików (`CostEstimateItemFile`)
- Zachowaj kaskadowe usuwanie podgrup i pozycji

### 5. `DeleteCostEstimateItemCommandHandler.cs`

- Soft-delete item
- Soft-delete pliki
- Obsłuż usuwanie opcji/komponentów jeśli to pozycja nadrzędna

### 6. `CreateCostEstimateCommandHandler.cs`

- Uprość tworzenie default schema — nie twórz już `FieldDefinitions`
- Twórz puste `AdditionalFields` (user doda później)
- Grupy i pozycje z direct properties

### 7. `CopyCostEstimateCommandHandler.cs`

- Kopiuj `AdditionalFields`
- Kopiuj `AdditionalFieldValues`
- Kopiuj direct properties
- Nie kopiuj FieldValues (nie istnieją)

### 8. `RecalculateCostEstimateCommandHandler.cs`

- Bez zmian — nadal wywołuje `CostEstimateCalculationService.RecalculateCostEstimate()`

### 9. `CreateCostEstimateFromAIPreviewCommandHandler.cs`

- Dostosuj do direct properties

### 10. Usuń stare handlery (które są już zastąpione)

- `UpsertCostEstimateGroupFieldCommandHandler` — zastąpiony przez `UpsertAdditionalFieldValueCommandHandler`
- `UpsertCostEstimateItemFieldCommandHandler` — zastąpiony przez `UpsertAdditionalFieldValueCommandHandler`
- `AddFieldDefinitionCommandHandler` — zastąpiony przez `AddAdditionalFieldCommand`
- `UpdateFieldDefinitionCommandHandler` — zastąpiony przez `UpdateAdditionalFieldCommand`
- `DeleteFieldDefinitionCommandHandler` — zastąpiony przez `DeleteAdditionalFieldCommand`
- `ReorderFieldDefinitionsCommandHandler` — zastąpiony przez `ReorderAdditionalFieldsCommand`

Po usunięciu, usuń też odpowiednie wpisy z kontrolera (Fix-10).

### Build

```powershell
dotnet build --configuration Release
```
Jeśli build failed, przerwij i zgłoś błędy.
