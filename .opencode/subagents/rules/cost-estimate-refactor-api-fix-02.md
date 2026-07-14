# Prompt implementacyjny: api-fix-02 — Readonly checks + walidacje pozostałych handlerów

## Cel

Przywrócenie walidacji w pozostałych handlerach kosztorysów:
1. **`UpsertCostEstimateGroupFieldCommandHandler`** — dodanie `IsReadonly` check w `AddFieldValue`, usunięcie TODO/MVP
2. **`UploadCostEstimateFieldFilesCommandHandler`** — dodanie `IsReadonly` check dla Restricted users

## Pliki do zmiany

### Plik 1: `UpsertCostEstimateGroupFieldCommandHandler.cs`

**Ścieżka:** `02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/CostEstimates/UpsertCostEstimateGroupField/UpsertCostEstimateGroupFieldCommandHandler.cs`

#### Zmiana 1a — Dodanie `fieldDefRepository` do konstruktora

Dodaj `IReadRepository<CostEstimateFieldDefinition>` — analogicznie jak w api-fix-01 dla UpsertCostEstimateItemField:

```csharp
private readonly IReadRepository<CostEstimateFieldDefinition> fieldDefRepository;
```

Dodaj do konstruktora:
```csharp
IReadRepository<CostEstimateFieldDefinition> fieldDefRepository
```

Dodaj przypisanie w ciele konstruktora:
```csharp
this.fieldDefRepository = fieldDefRepository;
```

#### Zmiana 1b — `AddFieldValue`: dodanie lookup fieldDef i IsReadonly check

**Lokalizacja:** Linie 100-107

**Stan obecny:**
```csharp
// Field definition validation simplified for MVP
// Assumes FieldDefinitionId is valid (validated in Command Validator)

// Field value validation removed for MVP
```

**Stan docelowy:**
Zastąpić komentarze kodem:
1. Pobrać `fieldDef` z repozytorium
2. Rzucić `NotFoundApiException` jeśli nie istnieje
3. Sprawdzić `fieldDef.IsReadonly` — jeśli true, rzucić `ForbiddenApiException("This field is read-only and cannot be modified.")`
4. Usunąć komentarze TODO/MVP

```csharp
CostEstimateFieldDefinition fieldDef = await fieldDefRepository.GetFirstBySearch(
    f => f.Id == request.FieldDefinitionId!.Value)
    ?? throw new NotFoundApiException(nameof(CostEstimateFieldDefinition), request.FieldDefinitionId!.Value.ToString());

if (fieldDef.IsReadonly)
{
    throw new ForbiddenApiException("This field is read-only and cannot be modified.");
}
```

#### Zmiana 1c — `UpdateFieldValue`: usunięcie komentarza MVP, dodanie walidacji

**Lokalizacja:** Linie 188-190

**Stan obecny:**
```csharp
// Field value validation removed for MVP
```

**Stan docelowy:**
- Sprawdź czy w UpdateFieldValue jest już read-only check (linie 186-187: `if (accessLevel == CostEstimateAccessLevel.Restricted && fieldValue.FieldDefinition.IsReadonly)`). Jeśli tak — kod jest OK, tylko usuń komentarz MVP.
- Jeśli brakuje checka `IsReadonly` dla Restricted — dodaj analogiczny check jak w UpsertItemField.

**Uwaga:** W UpdateFieldValue `fieldValue.FieldDefinition` jest już załadowany przez Include na linii 184.

#### Zmiana 1d — usunięcie TODO z Handle

**Lokalizacja:** Linie 73-74

**Stan obecny:**
```csharp
// Template lookup removed - schema-based validation for MVP
// TODO: Add read-only field check from schema if needed
```

**Stan docelowy:** Usunąć oba komentarze.

### Plik 2: `UploadCostEstimateFieldFilesCommandHandler.cs`

**Ścieżka:** `02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/CostEstimates/UploadCostEstimateFieldFiles/UploadCostEstimateFieldFilesCommandHandler.cs`

#### Zmiana 2a — Dodanie fieldDef lookup i IsReadonly check

**Lokalizacja:** Linie 78-84

**Stan obecny:**
```csharp
CostEstimateItemStructureGuard.EnsureItemHasNoComponents(request.ItemId, itemsDict);

// TODO: Template lookup removed for MVP
// Field definition validation moved to CommandValidator

// TODO: Readonly check removed for MVP (fieldDef not available)
// Restricted users can upload files for now
```

**Stan docelowy:**
1. Dodać pole `IReadRepository<CostEstimateFieldDefinition> fieldDefRepository` (wstrzyknięte przez konstruktor)
2. Po guardzie `EnsureItemHasNoComponents`:
   ```csharp
   CostEstimateFieldDefinition fieldDef = await fieldDefRepository.GetFirstBySearch(
       f => f.Id == request.FieldDefinitionId)
       ?? throw new NotFoundApiException(nameof(CostEstimateFieldDefinition), request.FieldDefinitionId.ToString());

   if (accessLevel == CostEstimateAccessLevel.Restricted && fieldDef.IsReadonly)
   {
       throw new ForbiddenApiException("This field is read-only and cannot be modified.");
   }
   ```
3. Usunąć komentarze TODO/MVP

## Zasady wykonania

1. Nie zmieniaj niczego poza opisanymi zmianami
2. Zachowaj istniejącą strukturę kodu i konwencje (explicit types, `is null`, `{}` na każdym bloku)
3. Po zmianie uruchom `dotnet build --configuration Release` w katalogu rozwiązania
4. Jeśli build nie przejdzie — raportuj błędy

## Weryfikacja

- Build przechodzi
- `UpsertCostEstimateGroupField.AddFieldValue` rzuci `ForbiddenApiException` gdy pole jest readonly
- `UploadCostEstimateFieldFiles` rzuci `ForbiddenApiException` gdy Restricted user próbuje uploadować na readonly field
- Wszystkie komentarze "removed for MVP" usunięte
