# API Fix 10: Usunięcie zbędnych elementów

## Kontekst
Feature: costestimate-full-refactor — patrz `.opencode/features/costestimate-full-refactor.md`

Ostatni krok API — usunięcie wszystkich zbędnych elementów po refaktorze.
Ten krok może zepsuć build, więc trzeba robić ostrożnie — usuwaj tylko to co nie jest już nigdzie używane.

## Do zrobienia

### 1. Usuń stare web modele (pliki .cs)

- `CostEstimateFieldDefinitionWeb.cs` — cały plik (zastąpiony przez `CostEstimateAdditionalFieldWeb`)
- `CostEstimateSchemaWeb.cs` — cały plik (zastąpiony przez listę `AdditionalFields` na kosztorysie)
- `CostEstimateFieldFileWeb` z `CostEstimateDataWeb.cs` (zastąpiony przez `CostEstimateItemFileWeb`)
- `CostEstimateFieldValueWeb` z `CostEstimateDataWeb.cs` (zastąpione przez direct properties + AdditionalFieldValues)

### 2. Usuń stare encje

- `CostEstimateFieldDefinition.cs` — cały plik
- `CostEstimateFieldSchema.cs` — cały plik
- `CostEstimateFieldFile.cs` — cały plik (zastąpiony przez `CostEstimateItemFile`)
- `CostEstimateItemFieldValue.cs` — cały plik (ma adnotację `[Obsolete]` z Fix-02)
- `CostEstimateGroupFieldValue.cs` — cały plik
- `CostEstimateFieldValueBase.cs` — cały plik

### 3. Usuń stare konfiguracje EF

- `CostEstimateFieldDefinitionConfiguration.cs`
- `CostEstimateFieldSchemaConfiguration.cs`
- `CostEstimateFieldFileConfiguration.cs`
- `CostEstimateItemConfiguration.cs` — usuń konfigurację relacji do ItemFieldValue (jeśli istnieje)
- `CostEstimateGroupConfiguration.cs` — usuń konfigurację relacji do GroupFieldValue (jeśli istnieje)

### 4. Usuń stare CQRS handlery

- `UpsertCostEstimateGroupFieldCommandHandler.cs` — cały folder CQRS
- `UpsertCostEstimateItemFieldCommandHandler.cs` — cały folder CQRS
- `AddFieldDefinitionCommandHandler.cs` — cały folder CQRS
- `UpdateFieldDefinitionCommandHandler.cs` — cały folder CQRS
- `DeleteFieldDefinitionCommandHandler.cs` — cały folder CQRS
- `ReorderFieldDefinitionsCommandHandler.cs` — cały folder CQRS
- `UploadCostEstimateFieldFilesCommandHandler.cs` — cały folder CQRS

### 5. Usuń stare walidatory

- `AddFieldDefinitionCommandValidator.cs`
- itp.

### 6. Usuń stare endpointy z kontrolera

Z `CostEstimateController.cs` usuń:

- `UpsertCostEstimateGroupField` (PATCH .../groups/{groupId}/fields)
- `UpsertCostEstimateItemField` (PATCH .../items/{itemId}/fields)
- `AddFieldDefinition` (POST .../schema/fields)
- `UpdateFieldDefinition` (PUT .../schema/fields/{fieldId})
- `DeleteFieldDefinition` (DELETE .../schema/fields/{fieldId})
- `ReorderFieldDefinitions` (POST .../schema/fields/reorder)
- `UploadCostEstimateFieldFiles` (POST .../items/{itemId}/files) — zastąpiony przez nowy endpoint

### 7. Usuń stare pomocniki

- `FieldTypeConverter.cs` — cały plik
- `CostEstimateFieldUpdateNotificationHelper.cs` — sprawdź czy używany
- `CostEstimateItemStructureGuard.cs` — sprawdź czy używany (może być zbędny bez FieldValues)

### 8. Usuń `FieldScope` z `CostEstimateEnums.cs`

Usuń cały enum `FieldScope`. Zostaw tylko `FieldType` (na razie — może być używany w starych pomocnikach).

**Uwaga**: Nie usuwaj jeszcze całego `FieldType` enuma — może być używany w Helperach. Zostaw do osobnego cleanup.

### 9. Usuń z AppDbContext

Usuń DbSety dla usuniętych encji:
- `DbSet<CostEstimateFieldDefinition>`
- `DbSet<CostEstimateFieldSchema>`
- `DbSet<CostEstimateFieldFile>`
- `DbSet<CostEstimateItemFieldValue>`
- `DbSet<CostEstimateGroupFieldValue>`

### Build

```powershell
dotnet build --configuration Release
```
Jeśli build failed, sprawdź błędy kompilatora — brakujące referencje. 
Prawdopodobnie `FieldType` enum jest jeszcze gdzieś używany — NIE usuwaj go w tym prompcie.

**Jeśli build przejdzie** → API jest gotowe.
**Jeśli build nie przejdzie** → zgłoś listę błędów i opcje naprawy.
