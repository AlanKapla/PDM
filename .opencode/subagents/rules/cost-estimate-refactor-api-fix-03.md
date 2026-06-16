# Prompt implementacyjny: api-fix-03 — Zastąpienie `DetermineFieldType` prawdziwym typem z definicji pola

## Cel

Obecnie oba handlery (`UpsertCostEstimateItemField`, `UpsertCostEstimateGroupField`) mają metodę `DetermineFieldType` która **zgaduje** typ pola po tym która wartość (decimal, bool, dateTime, string) nie jest nullem. To jest błędne — typ pola jest zdefiniowany w `FieldDefinition.FieldType` i musi być odczytany stamtąd.

Po zmianach z api-fix-01 i api-fix-02 oba handlery mają już załadowany `fieldDef` (w `AddFieldValue`) lub mają dostępny `fieldValue.FieldDefinition` (w `UpdateFieldValue`). Trzeba wykorzystać to do zastąpienia `DetermineFieldType`.

## Pliki do zmiany

### Plik 1: `UpsertCostEstimateItemFieldCommandHandler.cs`

**Ścieżka:** `02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/CostEstimates/UpsertCostEstimateItemField/UpsertCostEstimateItemFieldCommandHandler.cs`

#### Zmiana 1a — `AddFieldValue` (existing field case, linia 129)

**Stan obecny:**
```csharp
FieldValueConverter.SetTypedValue(
    existingFieldValue,
    DetermineFieldType(request),
    request.StringValue,
    request.DecimalValue,
    request.BoolValue,
    request.DateTimeValue);
```

**Stan docelowy:** Użyj `(int)fieldDef.FieldType` zamiast `DetermineFieldType(request)`:
```csharp
FieldValueConverter.SetTypedValue(
    existingFieldValue,
    (int)fieldDef.FieldType,
    request.StringValue,
    request.DecimalValue,
    request.BoolValue,
    request.DateTimeValue);
```

#### Zmiana 1b — `AddFieldValue` (UpdateItemNameAsync, linia 143)

**Stan obecny:**
```csharp
await UpdateItemNameAsync((FieldType)DetermineFieldType(request), request, cancellationToken);
```

**Stan docelowy:**
```csharp
await UpdateItemNameAsync(fieldDef.FieldType, request, cancellationToken);
```

#### Zmiana 1c — `AddFieldValue` (new field case, linia 155-157)

**Stan obecny:**
```csharp
FieldValueConverter.SetTypedValue(
    fieldValue,
    DetermineFieldType(request),
    ...
```

**Stan docelowy:**
```csharp
FieldValueConverter.SetTypedValue(
    fieldValue,
    (int)fieldDef.FieldType,
    ...
```

#### Zmiana 1d — `AddFieldValue` (UpdateItemNameAsync new field, linia 169)

**Stan obecny:**
```csharp
await UpdateItemNameAsync((FieldType)DetermineFieldType(request), request, cancellationToken);
```

**Stan docelowy:**
```csharp
await UpdateItemNameAsync(fieldDef.FieldType, request, cancellationToken);
```

#### Zmiana 1e — `UpdateFieldValue` (SetTypedValue, linia 195-197)

**Stan obecny:**
```csharp
FieldValueConverter.SetTypedValue(
    fieldValue,
    DetermineFieldType(request),
    ...
```

**Stan docelowy:** Użyj `(int)fieldValue.FieldDefinition.FieldType`:
```csharp
FieldValueConverter.SetTypedValue(
    fieldValue,
    (int)fieldValue.FieldDefinition.FieldType,
    ...
```

#### Zmiana 1f — Usunięcie metody `DetermineFieldType` (linie 262-273)

Usuń całą metodę:
```csharp
private static int DetermineFieldType(UpsertCostEstimateItemFieldCommand request)
{
    // Simplified type inference from non-null value
    if (request.DecimalValue.HasValue)
        return 102; // ItemSystemQuantity or numeric
    if (request.BoolValue.HasValue)
        return 199; // Generic bool
    if (request.DateTimeValue.HasValue)
        return 198; // Generic date
    
    return 100; // ItemSystemName (string default)
}
```

### Plik 2: `UpsertCostEstimateGroupFieldCommandHandler.cs`

**Ścieżka:** `02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/CostEstimates/UpsertCostEstimateGroupField/UpsertCostEstimateGroupFieldCommandHandler.cs`

#### Zmiana 2a — `AddFieldValue` (existing field case, linia 120-125)

**Stan obecny:**
```csharp
// Simplified type inference - use non-null value type
int fieldType = DetermineFieldType(request);

FieldValueConverter.SetTypedValue(
    existingFieldValue,
    fieldType,
    ...
```

**Stan docelowy:** Użyj `(int)fieldDef.FieldType`:
```csharp
int fieldType = (int)fieldDef.FieldType;

FieldValueConverter.SetTypedValue(
    existingFieldValue,
    fieldType,
    ...
```

#### Zmiana 2b — `AddFieldValue` (new field case, linia 155-159)

**Stan obecny:**
```csharp
int newFieldType = DetermineFieldType(request);

FieldValueConverter.SetTypedValue(
    fieldValue,
    newFieldType,
    ...
```

**Stan docelowy:**
```csharp
int newFieldType = (int)fieldDef.FieldType;

FieldValueConverter.SetTypedValue(
    fieldValue,
    newFieldType,
    ...
```

#### Zmiana 2c — Usunięcie metody `DetermineFieldType` (linie 217-230)

Usuń całą metodę:
```csharp
private static int DetermineFieldType(UpsertCostEstimateGroupFieldCommand request)
{
    // Simplified type inference from non-null value
    // Priority: DecimalValue > BoolValue > DateTimeValue > StringValue (default)
    if (request.DecimalValue.HasValue)
        return 102; // ItemSystemQuantity or similar numeric type
    if (request.BoolValue.HasValue)
        return 199; // Generic bool type
    if (request.DateTimeValue.HasValue)
        return 198; // Generic date type
    
    // Default to string/text type (GroupName or generic text)
    return 0; // GroupName type
}
```

## Zasady wykonania

1. Nie zmieniaj niczego poza opisanymi zmianami
2. Zachowaj istniejącą strukturę kodu i konwencje
3. Po zmianie uruchom `dotnet build --configuration Release` w katalogu rozwiązania
4. Jeśli build nie przejdzie — raportuj błędy

## Weryfikacja

- Build przechodzi
- `DetermineFieldType` nie istnieje w żadnym z dwóch handlerów
- Wszystkie wywołania `SetTypedValue` używają rzeczywistego `FieldType` z definicji pola
- `UpdateItemNameAsync` otrzymuje `fieldDef.FieldType` zamiast castowanego `DetermineFieldType`
