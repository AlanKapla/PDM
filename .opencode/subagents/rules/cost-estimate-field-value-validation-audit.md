# Raport walidacji wartości vs typ pola — Kosztorysy

**Data:** 2026-05-31  
**Zakres:** Wszystkie typy pól (`FieldType` enum) + walidacja wartości przy zapisie  
**Warstwy:** FluentValidation validators → Handlery → `FieldValueConverter` → `CostEstimateValidators` (business)

---

## 1. MODEL DANYCH — TYPY I MAGAZYN WARTOŚCI

Wartości pól zapisywane są w `CostEstimateFieldValueBase` z czterema kolumnami typowanymi:

| Kolumna DB | Typ .NET | Constraint EF | Używana dla typów pól |
|-----------|----------|---------------|----------------------|
| `StringValue` | `string?` | `nvarchar(2000)` | Text: GroupName, GroupDesc, GroupNumber, GroupStatus, GroupNotes, GroupResponsible, ItemSystemName, ItemSystemUnit, ItemSystemCategory, ItemGenericString |
| `DecimalValue` | `decimal?` | `precision(18,6)` | Numeric: GroupBudget, GroupPriority, ItemSystemQuantity, ItemCalculated*, ItemGenericNumber |
| `BoolValue` | `bool?` | brak | Boolean: ItemSystemSelected, ItemSystemIsWorkScope, ItemGenericBoolean |
| `DateTimeValue` | `DateTime?` | brak | Date: GroupStartDate, GroupEndDate, ItemGenericDate, ItemGenericDateTime |

---

## 2. PEŁNA MAPA TYPÓW PÓL I ICH WALIDACJA

### 2.1 GROUP HEADER FIELDS (Scope: Group, Range: 0–9)

| FieldType | Wartość | Przechowywanie | Walidacja FluentValidation | Walidacja handler | Walidacja business (`CostEstimateGroupValidator`) | Status |
|-----------|---------|---------------|---------------------------|-------------------|--------------------------------------------------|--------|
| `GroupName` (0) | string | `StringValue` | ❌ brak limitu wartości | ❌ brak | ⚠️ metoda istnieje ale **nigdy nie wywoływana** | 🔴 Brak |
| `GroupDescription` (1) | string | `StringValue` | ❌ brak | ❌ brak | ⚠️ jw. | 🔴 Brak |
| `GroupNumber` (2) | string | `StringValue` | ❌ brak | ❌ brak | ⚠️ jw. | 🔴 Brak |
| `GroupStartDate` (3) | DateTime | `DateTimeValue` | ❌ brak | ❌ brak | ⚠️ jw. | 🔴 Brak |
| `GroupEndDate` (4) | DateTime | `DateTimeValue` | ❌ brak (StartDate < EndDate?) | ❌ brak | ⚠️ jw. | 🔴 Brak |
| `GroupStatus` (5) | string | `StringValue` | ❌ brak | ❌ brak | ⚠️ jw. | 🔴 Brak |
| `GroupNotes` (6) | string | `StringValue` | ❌ brak | ❌ brak | ⚠️ jw. | 🔴 Brak |
| `GroupResponsible` (7) | string | `StringValue` | ❌ brak | ❌ brak | ⚠️ jw. | 🔴 Brak |
| `GroupBudget` (8) | decimal | `DecimalValue` | ❌ brak | ❌ brak | ⚠️ jw. | 🔴 Brak |
| `GroupPriority` (9) | int (decimal) | `DecimalValue` | ❌ brak zakresu | ❌ brak | ⚠️ jw. | 🔴 Brak |

**Uwaga ogólna dla Group:** `UpsertCostEstimateGroupFieldCommandValidator` nie waliduje wartości — tylko IDs. Cała logika walidacji wartości Group jest w `CostEstimateGroupValidator.ValidateGroupFieldValues()` która zawiera tylko komentarz `// TODO: Dodatkowa walidacja wartości według typu pola` i **nigdy nie jest wywoływana** przez żaden handler.

---

### 2.2 ITEM SYSTEM FIELDS (Scope: ItemSystem, Range: 100–107)

| FieldType | Wartość | Przechowywanie | Walidacja FluentValidation | Walidacja handler (AddFieldValue) | Walidacja handler (UpdateFieldValue) | `CostEstimateItemValidator` | Status |
|-----------|---------|---------------|---------------------------|----------------------------------|--------------------------------------|----------------------------|--------|
| `ItemSystemName` (100) | string | `StringValue` | ❌ brak NotEmpty/MaxLength | ❌ brak | ❌ brak | ⚠️ wywoływana nigdzie | 🟠 |
| `ItemSystemQuantity` (101) | decimal | `DecimalValue` | ❌ brak | ❌ brak | ❌ brak | Waliduje ≥ 0 ale **nigdy nie wywołana** | 🔴 |
| `ItemSystemUnit` (102) | string | `StringValue` | ❌ brak | ❌ brak | ❌ brak | ⚠️ jw. | 🔴 |
| `ItemSystemOptions` (103) | collection | brak (IsCollection) | N/A — pole specjalne | Pomijane (IsCollection check) | Pomijane | ⚠️ jw. | 🟡 |
| `ItemSystemSelected` (104) | bool | `BoolValue` | ❌ brak | ❌ brak | ❌ brak | ⚠️ jw. | 🔴 |
| `ItemSystemFiles` (105) | file | osobny endpoint | N/A — osobny flow | N/A | N/A | ⚠️ jw. | 🟢 (własny validator) |
| `ItemSystemCategory` (106) | string | `StringValue` | ❌ brak | ❌ brak | ❌ brak | ⚠️ jw. | 🔴 |
| `ItemSystemIsWorkScope` (107) | bool | `BoolValue` | ❌ brak | ❌ brak | ❌ brak | ⚠️ jw. | 🔴 |

---

### 2.3 ITEM CALCULATED FIELDS (Scope: ItemCalculated, Range: 200–206)

| FieldType | Wartość | Przechowywanie | Walidacja FluentValidation | Walidacja handler `AddFieldValue` | Walidacja handler `UpdateFieldValue` | `CostEstimateItemValidator` | Status |
|-----------|---------|---------------|---------------------------|----------------------------------|--------------------------------------|----------------------------|--------|
| `ItemCalculatedUnitPriceNet` (200) | decimal | `DecimalValue` | ❌ brak | ❌ brak (tylko IsReadonly check) | ❌ brak | Waliduje ≥ 0 — **NIGDY nie wywołana** | 🔴 |
| `ItemCalculatedVatRate` (201) | decimal [0,1] | `DecimalValue` | ❌ brak | ✅ sprawdza [0,1] | ❌ **BRAK** — tylko [0, 100] w `CostEstimateItemValidator` | ⚠️ sprzeczne zakresy | 🔴 |
| `ItemCalculatedUnitPriceGross` (202) | decimal | `DecimalValue` | ❌ brak | ❌ brak | ❌ brak | Waliduje ≥ 0 — **NIGDY nie wywołana** | 🔴 |
| `ItemCalculatedValueNet` (203) | decimal | `DecimalValue` | ❌ brak | ❌ brak | ❌ brak | Waliduje ≥ 0 — **NIGDY nie wywołana** | 🔴 |
| `ItemCalculatedValueGross` (204) | decimal | `DecimalValue` | ❌ brak | ❌ brak | ❌ brak | Waliduje ≥ 0 — **NIGDY nie wywołana** | 🔴 |
| `ItemCalculatedUnitVat` (205) | decimal | `DecimalValue` | ❌ brak | ❌ brak | ❌ brak | Waliduje ≥ 0 — **NIGDY nie wywołana** | 🔴 |
| `ItemCalculatedTotalVat` (206) | decimal | `DecimalValue` | ❌ brak | ❌ brak | ❌ brak | Waliduje ≥ 0 — **NIGDY nie wywołana** | 🔴 |

---

### 2.4 ITEM GENERIC FIELDS (Scope: ItemGeneric, Range: 300–304)

| FieldType | Wartość | Przechowywanie | Walidacja FluentValidation | Walidacja handler | `CostEstimateItemValidator` | Status |
|-----------|---------|---------------|---------------------------|-------------------|-----------------------------|--------|
| `ItemGenericNumber` (300) | decimal | `DecimalValue` | ❌ brak zakresu | ❌ brak | Waliduje ≥ 0 — **NIGDY nie wywołana** | 🔴 |
| `ItemGenericString` (301) | string | `StringValue` | ❌ brak MaxLength | ❌ brak | ❌ brak | 🟠 |
| `ItemGenericBoolean` (302) | bool | `BoolValue` | ❌ brak | ❌ brak | ❌ brak | 🟡 (bool nie wymaga zakresu) |
| `ItemGenericDate` (303) | DateTime | `DateTimeValue` | ❌ brak | ❌ brak | ❌ brak | 🟡 |
| `ItemGenericDateTime` (304) | DateTime | `DateTimeValue` | ❌ brak | ❌ brak | ❌ brak | 🟡 |

---

## 3. KLUCZOWE ODKRYCIA

### 3.1 🔴 KRYTYCZNE — `CostEstimateGroupValidator` i `CostEstimateItemValidator` są MARTWYM KODEM

**Problem:** Obie klasy walidatorów domenowych:
- Są zarejestrowane w DI (`services.AddScoped<CostEstimateGroupValidator>()`)
- Mają testy jednostkowe
- **NIE SĄ nigdzie wstrzykiwane ani wywoływane** w handlerach CQRS

Wyszukiwanie użycia w całym projekcie: 0 wywołań `CostEstimateGroupValidator` / `CostEstimateItemValidator` w handlerach.

```csharp
// CostEstimateValidators.cs — ValidateGroupFieldValues()
// TODO: Dodatkowa walidacja wartości według typu pola  ← PUSTE, nigdy nie wywołane
```

**Efekt:** Cała logika walidacji zakresów wartości jest napisana, przetestowana, ale **nigdy nie wykonywana w produkcji**.

---

### 3.2 🔴 KRYTYCZNE — Sprzeczne zakresy `VatRate` między handlerami a validatorem domenowym

| Miejsce | Zakres VatRate |
|---------|---------------|
| `UpsertCostEstimateItemFieldCommandHandler.AddFieldValue` | `[0, 1]` (0.23 = 23%) |
| `CostEstimateItemValidator.ValidateDecimalRange` | `[0, 100]` (23 = 23%) |

Komentarz w enum mówi `0.23 = 23%` — więc handler ma rację, a `CostEstimateItemValidator` jest błędny. Ale ponieważ validator jest martwy, nie powoduje problemu dziś — jednak stanowi pułapkę jeśli ktoś go wywoła.

---

### 3.3 🔴 KRYTYCZNE — Brak walidacji typu danych wejściowych vs FieldType

Przy zapisie pola (Add/Update) `FieldValueConverter.SetTypedValue` nie rzuca błędu gdy:
- Wysłano `stringValue` dla pola numerycznego — po prostu nie zapisuje nic (`DecimalValue = null`)
- Wysłano `decimalValue` dla pola tekstowego — ignoruje i nie zapisuje
- Wysłano NULL dla wszystkich czterech wartości — zapisuje "pusty" rekord bez błędu

**Obecna logika `SetTypedValue`:**
```csharp
// config.IsNumeric → zapisuje decimalValue (nie waliduje null)
// config.IsText    → zapisuje stringValue (nie waliduje null)
// config.IsBoolean → zapisuje boolValue (nie waliduje null)
// config.IsDate    → zapisuje dateTimeValue (nie waliduje null)
```
Brak wyjątku/walidacji gdy wysłano wartość w złym polu.

---

### 3.4 🟠 WYSOKI — `StringValue` ograniczone do 2000 znaków w DB, ale brak walidacji w API

DB: `nvarchar(2000)` → przy przekroczeniu nastąpi runtime exception z EF Core (nie 400 Bad Request).

Dotyczy pól: `GroupName`, `GroupDescription`, `GroupNotes`, `ItemSystemName`, `ItemGenericString` i innych tekstowych.

---

### 3.5 🟠 WYSOKI — Pola obliczeniowe (`ItemCalculated*`) mogą być zapisywane ręcznie bez ograniczeń

Pola `ItemCalculatedUnitPriceNet`, `ItemCalculatedVatRate`, `ItemCalculatedUnitPriceGross`, `ItemCalculatedValueNet`, `ItemCalculatedValueGross`, `ItemCalculatedUnitVat`, `ItemCalculatedTotalVat` mają `IsReadonly` w konfiguracji per-użytkownik (`Restricted` access), ale:
- Właściciel kosztorysu (`Owner` access) może wpisać dowolną wartość w pola kalkulowane
- Brak walidacji że `UnitPriceGross` = `UnitPriceNet * (1 + VatRate)` przy ręcznym zapisie
- Brak walidacji że wartości są nieujemne dla właściciela

---

### 3.6 🟠 WYSOKI — `GroupStartDate` / `GroupEndDate` — brak walidacji kolejności dat

Brak reguły `StartDate < EndDate` zarówno w FluentValidation jak i w handlerze.

---

### 3.7 🟡 NORMALNY — `GroupPriority` (9) przechowywany jako `decimal`, ale semantycznie jest `int`

Konfiguracja: `ValueTypeName: "int"` — ale przechowywany w `DecimalValue`. Nie ma walidacji że wartość jest całkowita ani zakresu (np. 1–5).

---

### 3.8 🟡 NORMALNY — `ItemSystemCategory` brak walidacji białej listy kategorii szablonu

Pole `ItemSystemCategory` może przyjąć dowolny string. Szablony definiują `Categories` — ale nie ma walidacji że wartość zapisana należy do tej listy.

---

## 4. ARCHITEKTURA PRZEPŁYWU WALIDACJI (obecny stan)

```
PATCH /items/{itemId}/fields
         │
         ▼
UpsertCostEstimateItemFieldCommandValidator
    ├── RequiredId (TenantId, ProjectId, CostEstimateId, ItemId) ✅
    └── FieldDefinitionId NotEmpty gdy FieldValueId=null ✅
         │
         ▼ (FluentValidationBehavior)
UpsertCostEstimateItemFieldCommandHandler
    ├── AccessLevel check ✅
    ├── Item existence check ✅
    ├── FieldDefinition existence in template ✅
    ├── IsReadonly check (Restricted only) ✅
    ├── VatRate [0,1] check (AddFieldValue only) ⚠️
    └── FieldValueConverter.SetTypedValue()
            ├── CostEstimateFieldTypeHelper.GetFieldTypeConfig() ✅
            ├── Czyści wszystkie kolumny ✅
            └── Zapisuje wartość w odpowiedniej kolumnie
                ⚠️ BEZ walidacji: null safety, zakresy, format

❌ CostEstimateItemValidator — NIGDY NIE WYWOŁYWANY
   (waliduje zakresy ale kod jest martwy)
```

---

## 5. ZESTAWIENIE PROBLEMÓW — PRIORYTETY

### 🔴 Krytyczne (3)

| # | Problem | Plik |
|---|---------|------|
| K1 | `CostEstimateGroupValidator` i `CostEstimateItemValidator` są martwym kodem — nigdy nie wywoływane w handlerach | `CostEstimateValidators.cs`, `ServiceCollectionExtensions.cs` |
| K2 | Brak walidacji że podany `value` pasuje typem do `FieldType` — można wysłać `stringValue` dla pola numeric, handler cicho zapisze null | `FieldValueConverter.cs`, `UpsertCostEstimateItemFieldCommandHandler.cs` |
| K3 | Sprzeczny zakres `VatRate`: handler Add=[0,1], validator domenowy=[0,100], Update path brak sprawdzenia | `UpsertCostEstimateItemFieldCommandHandler.cs`, `CostEstimateValidators.cs` |

### 🟠 Wysokie (3)

| # | Problem | Plik |
|---|---------|------|
| H1 | `StringValue` max 2000 znaków w DB bez walidacji w API — przy przekroczeniu runtime exception zamiast 400 | `CostEstimateItemConfiguration.cs`, brak validatora |
| H2 | Pola `ItemCalculated*` mogą być zapisywane przez właściciela bez żadnych reguł domenowych (brak reguły nieujemności, brak spójności formuł) | `UpsertCostEstimateItemFieldCommandHandler.cs` |
| H3 | `GroupStartDate`/`GroupEndDate` — brak walidacji `StartDate ≤ EndDate` | brak validatora |

### 🟡 Normalne (3)

| # | Problem | Plik |
|---|---------|------|
| N1 | `GroupPriority` brak walidacji zakresu i całkowitości | brak validatora |
| N2 | `ItemSystemCategory` nie jest walidowana względem listy kategorii szablonu | `UpsertCostEstimateItemFieldCommandHandler.cs` |
| N3 | `ValidateGroupFieldValues` zawiera komentarz `// TODO:` — nigdy nie zaimplementowana | `CostEstimateValidators.cs` |

---

## 6. REKOMENDACJE NAPRAWCZE

### Priorytet 1 — Podłączyć `CostEstimateItemValidator` do handlerów

W `UpsertCostEstimateItemFieldCommandHandler` wstrzyknąć `CostEstimateItemValidator` i wywołać `ValidateFieldValueByType` przed zapisem. Naprawić przy okazji zakres VatRate [0,1] → [0,100] LUB odwrotnie (wybrać jedną konwencję).

### Priorytet 2 — Dodać walidację type-mismatch w `FieldValueConverter`

```csharp
// Przykład — rzucić ValidationApiException gdy typ nie pasuje
if (config.IsNumeric && !decimalValue.HasValue && stringValue != null)
    throw new ValidationApiException("Pole numeryczne wymaga wartości decimalValue");
```

### Priorytet 3 — Dodać MaxLength(2000) w FluentValidation dla pól tekstowych

W `UpsertCostEstimateItemFieldCommandValidator` i `UpsertCostEstimateGroupFieldCommandValidator` dodać:
```csharp
RuleFor(x => x.StringValue)
    .MaximumLength(2000).WithMessage("StringValue cannot exceed 2000 characters")
    .When(x => x.StringValue != null);
```

### Priorytet 4 — Zaimplementować `ValidateGroupFieldValues` w `CostEstimateGroupValidator`

Usunąć komentarz `// TODO:` i zaimplementować walidację analogicznie jak `ValidateItemFieldValues`.

### Priorytet 5 — Dodać walidację StartDate ≤ EndDate

W handlerze `UpsertCostEstimateGroupFieldCommandHandler` po zapisaniu obu dat sprawdzić spójność.
