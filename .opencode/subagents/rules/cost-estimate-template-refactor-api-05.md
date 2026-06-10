# API-05: Walidacja kolejności GroupName / ItemSystemName

## Cel
Automatyczne wymuszenie, że GroupName (FieldType=0) jest zawsze pierwszy w group fields, a ItemSystemName (FieldType=100) zawsze pierwszy w item fields. Zgodnie z decyzją usera — wymuszamy automatycznie, bez rzucania wyjątku.

## Pliki do zmiany

### 1. `CQRS/CostEstimateTemplates/Shared/CostEstimateTemplateHandlerBase.cs`

Dodaj metodę walidującą i wymuszającą kolejność:

```csharp
/// <summary>
/// Wymusza, że GroupName jest pierwszy w group fields, a ItemSystemName pierwszy w item fields.
/// Automatycznie ustawia Order=0 dla tych pól w swoich zakresach.
/// </summary>
protected static void EnforceRequiredFieldOrder(
    List<FieldDefinitionDto> groupFields,
    List<FieldDefinitionDto> systemFields)
{
    // GroupName (FieldType = 0) musi być pierwszy w groupFields
    var groupName = groupFields?.FirstOrDefault(f => f.FieldType == (int)FieldType.GroupName);
    if (groupName != null)
    {
        groupName.Order = 0;
        // Przesuń pozostałe group fields
        int order = 1;
        foreach (var field in groupFields.Where(f => f.FieldType != (int)FieldType.GroupName))
        {
            if (field.Order <= 0) field.Order = order;
            order++;
        }
    }

    // ItemSystemName (FieldType = 100) musi być pierwszy w systemFields
    var itemSystemName = systemFields?.FirstOrDefault(f => f.FieldType == (int)FieldType.ItemSystemName);
    if (itemSystemName != null)
    {
        itemSystemName.Order = 0;
        // Przesuń pozostałe system fields
        int order = 1;
        foreach (var field in systemFields.Where(f => f.FieldType != (int)FieldType.ItemSystemName))
        {
            if (field.Order <= 0) field.Order = order;
            order++;
        }
    }
}
```

### 2. `CQRS/CostEstimateTemplates/UpdateCostEstimateTemplate/UpdateCostEstimateTemplateCommandHandler.cs`

Dodaj wywołanie `EnforceRequiredFieldOrder` przed `ValidateRequiredTemplateFields`:

```csharp
if (request.UpdateStructure)
{
    // Automatyczne wymuszenie kolejności pól obowiązkowych
    EnforceRequiredFieldOrder(request.GroupHeaderFields, request.SystemFields);

    ValidateRequiredTemplateFields(ExtractFieldTypes(
        request.GroupHeaderFields,
        request.SystemFields,
        request.CalculatedFields));
}
```

### 3. `Business/Implementation/Services/CostEstimateTemplateService.cs`

W metodzie `UpdateTemplateAsync`, przy budowaniu `columnLayoutOrderMaps`, również wymuś że GroupName i ItemSystemName mają Order=0 w swoich layoutach.

W `BuildColumnLayoutOrderMaps` (lub w metodzie która ją woła) — jeśli groupColumnLayout istnieje i nie zaczyna się od GUID GroupName, dodaj log (ale nie zmieniaj automatycznie — layout GUID-ów jest przekazany przez frontend).

**Ważne:** W przypadku gdy frontend nie podał layoutu (null), a używany jest legacy `ColumnLayout` — wymuś że GroupName (pole z FieldType=0) jest pierwsze w group części, a ItemSystemName (FieldType=100) pierwsze w item części.

## Uwagi
- Decyzja usera: **B** — automatycznie wymuszać, nie blokować
- `FieldDefinitionDto` nie ma właściwości `Order`? Sprawdź — jeśli nie ma, dodaj lub użyj `FieldType` do określenia pozycji
- Jeśli `FieldDefinitionDto.Order` nie istnieje — pomiń tę część, skup się na `ColumnLayout` gdzie kolejność wynika z pozycji na liście
