# Prompt implementacyjny: api-fix-01 — Przywrócenie EnsureItemHasNoComponents i CheckExclusiveSelection

## Cel

Przywrócenie dwóch krytycznych walidacji biznesowych w `UpsertCostEstimateItemFieldCommandHandler.cs`,
które zostały wyłączone przez komentarze "removed for MVP":
1. **`EnsureItemHasNoComponents`** — pozycja z komponentami nie może mieć własnych wartości pól
2. **`CheckExclusiveSelectionAsync`** — tylko jedna opcja może być zaznaczona w danej pozycji

## Plik do zmiany

**`02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/CostEstimates/UpsertCostEstimateItemField/UpsertCostEstimateItemFieldCommandHandler.cs`**

## Zmiana 1 — EnsureItemHasNoComponents w AddFieldValue

**Lokalizacja:** Linie 108-113

**Stan obecny:**
```csharp
// TODO: EnsureItemHasNoComponents validation removed for MVP
// CostEstimateItemStructureGuard.EnsureItemHasNoComponents(request.ItemId, itemsDict, fieldDef.FieldType);

// TODO: Field value validation removed for MVP
// CostEstimateFieldValueContext validation removed

// TODO: CheckExclusiveSelection removed for MVP
```

**Stan docelowy:**
1. Pobrać `fieldDef` (FieldDefinition) przed walidacją, żeby mieć `fieldDef.FieldType`
2. Dodać wywołanie `CostEstimateItemStructureGuard.EnsureItemHasNoComponents(request.ItemId, itemsDict, fieldDef.FieldType)`
3. Usunąć komentarze TODO/MVP

**Sposób implementacji:**
- Na początku metody `AddFieldValue`, po access level check (linia 103), pobrać FieldDefinition przez `cacheService.GetFieldDefinitionsDictionaryAsync(...)` lub przez repozytorium — sprawdź jakie metody ma `ICostEstimateCacheService` aby uzyskać słownik definicji pól. Potrzebujesz `fieldDef.FieldType` dla guarda.
- Alternatywnie: przekaż `FieldType` z `request` jeśli możesz go wywnioskować z kontekstu — ale lepiej pobrać z cache.

```csharp
// Dla AddFieldValue — pobierz fieldDef, żeby sprawdzić FieldType
Dictionary<Guid, CostEstimateFieldDefinition> fieldDefsDict = await cacheService.GetFieldDefinitionsDictionaryAsync(
    request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

if (!fieldDefsDict.TryGetValue(request.FieldDefinitionId!.Value, out CostEstimateFieldDefinition? fieldDef))
{
    throw new NotFoundApiException(nameof(CostEstimateFieldDefinition), request.FieldDefinitionId!.Value.ToString());
}

CostEstimateItemStructureGuard.EnsureItemHasNoComponents(request.ItemId, itemsDict, fieldDef.FieldType);
```

**Uwaga:** Sprawdź czy `ICostEstimateCacheService` ma metodę `GetFieldDefinitionsDictionaryAsync`. Jeśli nie — zobacz jakie są dostępne metody w interfejsie i użyj odpowiedniej. Jeśli nie ma cache dla fieldDefs, użyj repozytorium `IReadRepository<CostEstimateFieldDefinition>`.

## Zmiana 2 — CheckExclusiveSelection w AddFieldValue

Po guardzie dodać wywołanie istniejącej metody:
```csharp
await CheckExclusiveSelectionAsync(request, fieldDef.FieldType, request.FieldDefinitionId!.Value, itemsDict, cancellationToken);
```

## Zmiana 3 — EnsureItemHasNoComponents w UpdateFieldValue

**Lokalizacja:** Linie 190-197

**Stan obecny:**
```csharp
// TODO: EnsureItemHasNoComponents validation removed for MVP
// CostEstimateItemStructureGuard.EnsureItemHasNoComponents(request.ItemId, itemsDict, fieldValue.FieldDefinition.FieldType);

// TODO: Field value validation removed for MVP
// CostEstimateFieldValueContext validation removed

// TODO: CheckExclusiveSelection removed for MVP
```

**Stan docelowy:**
- Dodać `CostEstimateItemStructureGuard.EnsureItemHasNoComponents(request.ItemId, itemsDict, fieldValue.FieldDefinition.FieldType)`
- Dodać `await CheckExclusiveSelectionAsync(request, fieldValue.FieldDefinition.FieldType, fieldValue.FieldDefinitionId, itemsDict, cancellationToken)`
- Usunąć komentarze TODO/MVP

W UpdateFieldValue `fieldValue.FieldDefinition` jest już załadowany (Include w GetFirstBySearch na linii 183), więc nie trzeba dodatkowego zapytania.

```csharp
CostEstimateItemStructureGuard.EnsureItemHasNoComponents(request.ItemId, itemsDict, fieldValue.FieldDefinition.FieldType);

await CheckExclusiveSelectionAsync(request, fieldValue.FieldDefinition.FieldType, fieldValue.FieldDefinitionId, itemsDict, cancellationToken);
```

## Zmiana 4 — Dodanie brakującego usinga (jeśli potrzeba)

Sprawdź czy `CostEstimateFieldDefinition` z `Entities.Models.CostEstimates` jest zaimportowany. Jeśli nie — dodaj.

## Zasady wykonania

1. Nie zmieniaj niczego poza opisanymi zmianami
2. Zachowaj istniejącą strukturę kodu i konwencje (explicit types, `is null`, `{}` na każdym bloku)
3. Po zmianie uruchom `dotnet build --configuration Release` w katalogu rozwiązania
4. Jeśli build nie przejdzie — raportuj błędy

## Weryfikacja

Po zmianie:
- `EnsureItemHasNoComponents` rzuci `ValidationApiException` gdy użytkownik spróbuje dodać wartość pola do pozycji która ma komponenty (z wyjątkiem ItemSystemName i ItemSystemSelected)
- `CheckExclusiveSelectionAsync` rzuci `ValidationApiException` gdy użytkownik zaznaczy drugą opcję w tej samej pozycji
- Build przechodzi
