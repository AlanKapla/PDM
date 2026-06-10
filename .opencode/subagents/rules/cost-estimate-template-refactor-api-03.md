# API-03: MapDefaultTemplateToStructure, DuplicateTemplateAsync, CreateTemplateFromDefaultAsync

## Cel
Dostosowanie pozostałych metod w `CostEstimateTemplateService.cs`, które budują strukturę kolumn, aby używały osobnych list group/item.

## Plik do zmiany

### `Business/Implementation/Services/CostEstimateTemplateService.cs`

#### Miejsce 1: `MapDefaultTemplateToStructure` (ok. linia 1020)

**OBECNIE:**
```csharp
var groupFields = template.GroupHeaderFields
    .Select(f => /* map do FieldDefinitionWeb */).ToList();
var systemFields = template.SystemFields
    .Select(f => /* map do FieldDefinitionWeb */).ToList();
var calcFields = template.CalculatedFields
    .Select(f => /* map do FieldDefinitionWeb */).ToList();
var genericFields = template.GenericFields
    .Select(f => /* map do FieldDefinitionWeb */).ToList();

var allFields = groupFields
    .Concat(systemFields).Concat(calcFields).Concat(genericFields)
    .Where(f => f.IsVisible)  // <-- tu jest niespójne filtrowanie IsVisible
    .Select((f, index) => new ColumnConfigurationWeb(
        f.Id, f.FieldName, (int)f.FieldTypeConfig.FieldType, f.Label,
        (int)f.FieldTypeConfig.FieldScope, index, f.IsVisible
    ))
    .ToList();

var uiConfig = columns.Count > 0 ? new UiConfigurationWeb(columns) : null;
```

**PO ZMIANIE:**
```csharp
// Group columns (usuń .Where(f => f.IsVisible) — filtrowanie tylko w handlerze)
var groupColumns = groupFields
    .Select((f, index) => new ColumnConfigurationWeb(
        f.Id, f.FieldName, (int)f.FieldTypeConfig.FieldType, f.Label,
        (int)f.FieldTypeConfig.FieldScope, index, f.IsVisible
    ))
    .ToList();

// Item columns
var itemFields = systemFields
    .Concat(calcFields).Concat(genericFields)
    .ToList();
var itemColumns = itemFields
    .Select((f, index) => new ColumnConfigurationWeb(
        f.Id, f.FieldName, (int)f.FieldTypeConfig.FieldType, f.Label,
        (int)f.FieldTypeConfig.FieldScope, index + groupColumns.Count, f.IsVisible
    ))
    .ToList();

var uiConfig = (groupColumns.Count > 0 || itemColumns.Count > 0) 
    ? new UiConfigurationWeb(groupColumns, itemColumns) 
    : null;
```

#### Miejsce 2: `DuplicateTemplateAsync` (ok. linia ~1141)

Znajdź miejsce gdzie budowany jest `columnLayout` przy duplikacji szablonu.

**OBECNIE:**
```csharp
var columnLayout = groupFields
    .Concat(systemFields).Concat(calculatedFields).Concat(genericFields)
    .Where(f => f.IsVisible)
    .Select(f => f.FieldName)
    .ToList();
var uiConfig = new UiConfigurationDto(columnLayout);
```

**PO ZMIANIE:**
```csharp
var groupColumnLayout = groupFields
    .Select(f => f.FieldName)
    .ToList();
var itemColumnLayout = systemFields
    .Concat(calculatedFields).Concat(genericFields)
    .Select(f => f.FieldName)
    .ToList();
var uiConfig = new UiConfigurationDto(groupColumnLayout, itemColumnLayout);
```

#### Miejsce 3: `CreateTemplateFromDefaultAsync` (ok. linia ~1224)

Analogiczna zmiana jak w `DuplicateTemplateAsync`.

## Zasady
- Usuń `.Where(f => f.IsVisible)` przy budowaniu kolumn w default templates — filtrowanie IsVisible jest tylko w `GetCostEstimateDetailsQueryHandler`
- Użyj nowego konstruktora `UiConfigurationWeb(groupColumns, itemColumns)`
- Użyj nowego konstruktora `UiConfigurationDto(groupColumnLayout, itemColumnLayout)`
