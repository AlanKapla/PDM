# Refaktor kosztorysów — pełna modernizacja API + UI

## Opis
Kompleksowy refaktor modułu kosztorysów upraszczający architekturę: pola podstawowe stają się bezpośrednimi właściwościami encji, a pola dodatkowe są trzymane w płaskiej strukturze jako schema na kosztorysie.

## Architektura docelowa

### 1. Pola podstawowe — bezpośrednio na encjach

**CostEstimateGroup:**
- `Name` (string) — już istnieje jako właściwość
- `TotalNet`, `TotalGross`, `TotalVat` — już istnieją, obliczane
- Brak FieldValues (Name jest bezpośrednią właściwością)

**CostEstimateItem:**
- `Name` (string) — już istnieje
- `Quantity` (decimal?) — NOWE, bezpośrednio
- `Unit` (string?) — NOWE, bezpośrednio
- `UnitPriceNet` (decimal?) — NOWE, bezpośrednio
- `VatRate` (decimal?) — NOWE, bezpośrednio (0.23 = 23%)
- `UnitPriceGross` (decimal?) — NOWE, obliczane
- `NetValue` (decimal?) — już istnieje
- `GrossValue` (decimal?) — już istnieje
- `VatValue` (decimal?) — już istnieje
- `IsSelected` (bool, default true) — NOWE
- `IsStageWork` (bool, default false) — NOWE
- `Files` — kolekcja `CostEstimateItemFile` (przeniesiona z FieldValue)
- Brak FieldValues (wszystkie podstawowe pola są właściwościami)

**ItemRelationType (bez zmian):**
- None = 0 (pozycja główna)
- Option = 1 (opcja)
- Component = 2 (komponent)

### 2. Pola dodatkowe — płaska schema na kosztorysie

Nowa encja: **CostEstimateAdditionalField**
- `Id` (Guid)
- `CostEstimateId` (Guid) — kosztorys do którego należy
- `Name` (string) — nazwa pola np. "Kod CPV", "Uwagi"
- `FieldType` (AdditionalFieldType: String=0, Decimal=1, Boolean=2, DateTime=3)
- `Order` (int) — kolejność wyświetlania
- `CreatedAt`

Nowa encja: **CostEstimateAdditionalFieldValue** (wspólna dla grup i pozycji)
- `Id` (Guid)
- `AdditionalFieldId` (Guid) — FK do definicji
- `GroupId` (Guid?, nullable) — jeśli wartość należy do grupy
- `ItemId` (Guid?, nullable) — jeśli wartość należy do pozycji
- `StringValue` (string?)
- `DecimalValue` (decimal?)
- `BoolValue` (bool?)
- `DateTimeValue` (DateTime?)

### 3. Pliki na pozycjach

Nowa encja: **CostEstimateItemFile** (zastępuje CostEstimateFieldFile)
- `Id` (Guid)
- `ItemId` (Guid) — FK do CostEstimateItem
- `CostEstimateId` (Guid) — denormalizacja
- `OriginalFileName`, `BlobName`, `ContentType`, `FileSize`, `Order`
- `CreatedAt`, `CreatedByUserId`

### 4. Schema dla UI

Przy odczycie kosztorysu API zwraca:
- Wszystkie definicje pól dodatkowych (z `CostEstimateAdditionalField`)
- Grupy z właściwościami (Name, TotalNet/Gross/Vat) + wartości pól dodatkowych
- Pozycje z właściwościami (Name, Quantity, Unit, UnitPriceNet, ...) + wartości pól dodatkowych + pliki
- UI zna pola podstawowe (są stałe dla encji)
- UI renderuje pola dodatkowe z schema wg zadanej kolejności

### 5. Usunięte / zbędne
- `FieldScope` enum — usunąć
- `CostEstimateFieldDefinition` encja — usunąć
- `CostEstimateFieldSchema` encja — usunąć
- `CostEstimateItemFieldValue` encja — usunąć
- `CostEstimateGroupFieldValue` encja — usunąć
- `CostEstimateFieldFile` encja — zastąpiona przez CostEstimateItemFile
- `IsUserDefined`, `CanDelete`, `CanRename` — zbędne
- Większość `FieldType` enuma — zostawić tylko typy dla pól dodatkowych (String/Decimal/Boolean/DateTime)
- Wszystkie endpointy schema field management — zastąpione prostszymi

### 6. Nowe endpointy

| Metoda | Ścieżka | Opis |
|--------|---------|------|
| GET | `/{id}/schema` | Pobierz pola dodatkowe kosztorysu |
| POST | `/{id}/schema` | Dodaj pole dodatkowe |
| PUT | `/{id}/schema/{fieldId}` | Edytuj pole dodatkowe |
| DELETE | `/{id}/schema/{fieldId}` | Usuń pole dodatkowe |
| POST | `/{id}/schema/reorder` | Zmień kolejność pól |
| PATCH | `/{id}/items/{itemId}/select` | Zmień IsSelected (z auto-deselect dla opcji) |
| PATCH | `/{id}/groups/{groupId}/additional-fields` | Zapisz pole dodatkowe grupy |
| PATCH | `/{id}/items/{itemId}/additional-fields` | Zapisz pole dodatkowe pozycji |

### 7. Zachowane endpointy (dostosowane)

| Metoda | Ścieżka | Zmiana |
|--------|---------|--------|
| PATCH | `/{id}/items/{itemId}/fields` | Usunąć — zastąpione przez bezpośrednie property |
| PATCH | `/{id}/groups/{groupId}/fields` | Usunąć — zastąpione przez bezpośrednie property |
| PATCH | `/{id}/items/{itemId}` | NOWY — update właściwości pozycji (quantity, unit, price, vat, name) |
| PATCH | `/{id}/groups/{groupId}` | NOWY — update właściwości grupy (name) |
| POST | `/{id}/items/{itemId}/files` | Uproszczenie — bez fieldDefinitionId |

### 8. Silnik obliczeń
- `CostEstimateCalculationService` — przystosowany do pracy z bezpośrednimi property
- Nadal obowiązuje: user wpisuje dowolne pole, pozostałe są obliczane
- Propagacja opcji: jeśli `IsSelected=true` na opcji → jej wartości kopiowane do rodzica
- Komponenty: sumowane tylko `IsSelected=true`
- Pozycje: sumowane do grupy tylko `IsSelected=true`

### 9. UI

**Widoki Tree/Card:**
- Renderowane pola podstawowe z property encji
- + Pola dodatkowe z schema wg zadanej kolejności
- Dla etapów: Quantity, Unit, UnitPriceNet, VatRate, UnitPriceGross, VatValue = puste/—

**Nowe funkcje:**
- Sortowanie po kolumnach
- Filtrowanie + search input po nazwie i tekstowych polach dodatkowych
- Checkbox IsSelected dla pozycji (sumowanie w etapie)
- Radio button dla opcji (exclusive)
- Checkbox IsSelected dla komponentów (sumowanie w pozycji)
- Checkbox IsStageWork dla pozycji głównych

**Autosave:**
- useFieldAutosave z debounce 700ms
- Osobne endpointy dla base fields (PATCH item/group) i additional fields

**Soft delete:**
- Rekurencyjny dla grup (IsDeleted na podgrupach i pozycjach)
- Pliki soft-delete z blob cleanup

### 10. Migracja DB

Wymagana migracja danych:
1. `CostEstimateItemFieldValue` → dla pól podstawowych: przepisać do entity properties
2. `CostEstimateItemFieldValue` → dla pól typu ItemSystemFiles: przenieść pliki do CostEstimateItemFile
3. `CostEstimateItemFieldValue` → dla pól ItemGeneric (user-defined): przenieść do CostEstimateAdditionalFieldValue
4. `CostEstimateGroupFieldValue` → dla GroupName: już na encji, usunąć duplikaty
5. `CostEstimateGroupFieldValue` → dla user-defined group fields: przenieść do CostEstimateAdditionalFieldValue
6. `CostEstimateFieldDefinition` → dla IsUserDefined=true: przenieść do CostEstimateAdditionalField
