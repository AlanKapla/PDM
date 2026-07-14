# API-02: BuildTemplateStructureAsync — osobne listy kolumn

## Cel
Zmiana metody `BuildTemplateStructureAsync` w `CostEstimateTemplateService.cs`, aby zamiast jednej płaskiej listy kolumn (mieszającej group i item fields) produkowała osobne listy `GroupColumns` i `ItemColumns`.

## Plik do zmiany

### `Business/Implementation/Services/CostEstimateTemplateService.cs`

#### Miejsce 1: `BuildTemplateStructureAsync` (ok. linia 324-362)

**OBECNIE:**
```csharp
var allFieldsList = new List<CostEstimateTemplateFieldDefinitionBase>();
allFieldsList.AddRange(groupHeaderFieldsList);
allFieldsList.AddRange(systemFieldsList);
allFieldsList.AddRange(calculatedFieldsList);
allFieldsList.AddRange(genericFieldsList);

var columns = allFieldsList
    .Where(f => f.ParentFieldId == null)
    .OrderBy(f => f.Order)
    .Select(f => new ColumnConfigurationWeb(
        f.Id, f.FieldName, (int)f.FieldType, f.Label, (int)f.FieldScope, f.Order, f.IsVisible
    ))
    .ToList();

UiConfigurationWeb? uiConfig = columns.Any() 
    ? new UiConfigurationWeb(columns) 
    : null;

return new CostEstimateTemplateStructureWeb(
    template.Id, template.MaxGroupLevel,
    units.OrderBy(u => u.Order).ToList(),
    categories.OrderBy(c => c.Order).ToList(),
    groupHeaderFields, systemFields, calculatedFields, genericFields,
    uiConfig
);
```

**PO ZMIANIE:**
```csharp
// Group columns — tylko pola z FieldScope == Group
var groupColumns = groupHeaderFieldsList
    .Where(f => f.ParentFieldId == null)
    .OrderBy(f => f.Order)
    .ThenBy(f => f.FieldName)  // tiebreaker
    .Select(f => new ColumnConfigurationWeb(
        f.Id, f.FieldName, (int)f.FieldType, f.Label, (int)f.FieldScope, f.Order, f.IsVisible
    ))
    .ToList();

// Item columns — tylko pola z FieldScope == ItemSystem/ItemCalculated/ItemGeneric
var itemFieldsList = new List<CostEstimateTemplateFieldDefinitionBase>();
itemFieldsList.AddRange(systemFieldsList);
itemFieldsList.AddRange(calculatedFieldsList);
itemFieldsList.AddRange(genericFieldsList);

var itemColumns = itemFieldsList
    .Where(f => f.ParentFieldId == null)
    .OrderBy(f => f.Order)
    .ThenBy(f => f.FieldName)  // tiebreaker
    .Select(f => new ColumnConfigurationWeb(
        f.Id, f.FieldName, (int)f.FieldType, f.Label, (int)f.FieldScope, f.Order, f.IsVisible
    ))
    .ToList();

UiConfigurationWeb? uiConfig = (groupColumns.Any() || itemColumns.Any()) 
    ? new UiConfigurationWeb(groupColumns, itemColumns) 
    : null;

return new CostEstimateTemplateStructureWeb(
    template.Id, template.MaxGroupLevel,
    units.OrderBy(u => u.Order).ToList(),
    categories.OrderBy(c => c.Order).ToList(),
    groupHeaderFields, systemFields, calculatedFields, genericFields,
    uiConfig
);
```

#### Miejsce 2: `BuildColumnLayoutOrderMap` (znajdź metodę)

Dodaj obsługę backward compatibility:
- Jeśli `GroupColumnLayout` i `ItemColumnLayout` są null, ale istnieje stary `ColumnLayout` — rozdziel go na group/item na podstawie `FieldScope` z przekazanych list `FieldDefinitionDto`
- Metoda powinna przyjmować osobne listy zamiast jednej

**Nowy sygnatura:**
```csharp
private static (Dictionary<Guid, int> groupOrderMap, Dictionary<Guid, int> itemOrderMap) BuildColumnLayoutOrderMaps(
    List<Guid>? groupColumnLayout,
    List<Guid>? itemColumnLayout,
    List<Guid>? legacyColumnLayout,
    List<FieldDefinitionDto> groupFields,
    List<FieldDefinitionDto> systemFields,
    List<FieldDefinitionDto> calculatedFields,
    List<FieldDefinitionDto> genericFields)
```

Logika:
1. Jeśli `groupColumnLayout` i `itemColumnLayout` są podane → użyj ich wprost
2. Jeśli tylko `legacyColumnLayout` jest podany (backward compat) → rozdziel: group fields to te z FieldScope=Group (0-9), item fields to te z FieldScope >= 100
3. W przeciwnym razie → puste mapy

#### Miejsce 3: `UpsertFieldsInBatchAsync` i metoda która ją woła (ok. linia ~153)

Dostosuj wywołanie `BuildColumnLayoutOrderMap` → `BuildColumnLayoutOrderMaps` i przekaż odpowiednie mapy do `UpsertFieldsInBatchAsync`.

**UpsertFieldsInBatchAsync** — obecnie przyjmuje `columnLayoutOrderMap` dla wszystkich pól. Po zmianie będzie przyjmować odpowiednią mapę (group lub item) w zależności od scope pól.

#### Miejsce 4: `CollectFieldsForUpsert` (znajdź)

Dostosuj aby używała odpowiedniej mapy kolejności dla danego scope.

## Jak znaleźć miejsca do zmiany

Szukaj w pliku `CostEstimateTemplateService.cs`:
- `BuildColumnLayoutOrderMap` — metoda i jej wywołania
- `allFieldsList` — w `BuildTemplateStructureAsync`
- `CollectFieldsForUpsert` — metoda upsertująca pola
