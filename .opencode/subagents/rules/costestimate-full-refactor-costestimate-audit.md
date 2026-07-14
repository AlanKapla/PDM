# Audyt kosztorysów dla feature: costestimate-full-refactor

**Data**: 2026-06-12
**Agent**: CostEstimate Audit Agent
**Zakres**: Spójność API↔UI, FieldScope removal, opcje, komponenty, IsSelected, pliki, user-defined fields, soft delete, autosave, kalkulacje

---

## BLOK 1 — Analiza spójności API ↔ UI

### Web modele vs UI types

| Obszar | API (C#) | UI (TypeScript) | Zgodne? | Uwagi |
|--------|----------|-----------------|---------|-------|
| **CostEstimateFieldValueWeb** | `CostEstimateDataWeb.cs` lines 21-33 | `costEstimate.types.new.ts` lines 170-182 | ✅ | Pełna zgodność pól |
| **CostEstimateFieldValueWeb.FieldScope** | `int FieldScope` | `fieldScope: number` | ✅ | Przekazywane jako int |
| **CostEstimateFieldValueWeb.FieldName** | `Guid? FieldName` | `fieldName?: string` | ✅ | Opcjonalne po obu stronach |
| **CostEstimateItemWeb** | `CostEstimateDataWeb.cs` lines 41-55 | `costEstimate.types.new.ts` lines 193-207 | ⚠️ | Różnica: `relationType` w UI opcjonalny (`?`), w API wymagany (`int`) |
| **CostEstimateItemWeb.RelationType** | `int RelationType` | `relationType?: number` | ⚠️ | UI ma jako opcjonalne, API zawsze zwraca wartość |
| **CostEstimateGroupWeb** | `CostEstimateDataWeb.cs` lines 60-74 | `costEstimate.types.new.ts` lines 212-226 | ✅ | Pełna zgodność |
| **CostEstimateFieldDefinitionWeb** | `CostEstimateFieldDefinitionWeb.cs` lines 6-22 | `costEstimate.types.new.ts` lines 712-728 | ✅ | Full match |
| **CostEstimateFieldDefinitionWeb.ChildFields** | `List<CostEstimateFieldDefinitionWeb>? ChildFields` | `childFields: CostEstimateFieldDefinitionWeb[] \| null` | ✅ | Zgodne |
| **CostEstimateSchemaWeb** | `CostEstimateSchemaWeb.cs` lines 6-12 | `costEstimate.types.new.ts` lines 733-739 | ✅ | Zgodne |
| **CostEstimateDetailsWeb** | `CostEstimateDetailsWeb.cs` lines 18-40 | `costEstimate.types.new.ts` lines 231-259 | ✅ | Zgodne |
| **CostEstimateMutationDto (Request DTOs)** | `CostEstimateMutationDto.cs` lines 18-53 | `costEstimate.types.new.ts` lines 105-126 | ✅ | Zgodne |
| **UpsertFieldValueRequestDto** | Commands + Mutations | `costEstimate.types.new.ts` lines 385-392 | ✅ | Zgodne |
| **CostEstimateFieldFileWeb** | `CostEstimateDataWeb.cs` lines 6-15 | `costEstimate.types.new.ts` lines 155-164 | ✅ | Zgodne |
| **AddGroupRequestDto** | Via command | `costEstimate.types.new.ts` lines 355-358 | ✅ | Zgodne |
| **AddItemRequestDto** | Via command | `costEstimate.types.new.ts` lines 363-368 | ✅ | Zgodne |

### Niespójności

| # | Problem | Lokalizacja API | Lokalizacja UI | Szczegóły |
|---|---------|----------------|----------------|-----------|
| 1 | `CostEstimateItemWeb.RelationType` opcjonalne w UI | API zawsze zwraca int | `costEstimate.types.new.ts:197` `relationType?: number` | UI powinno być `relationType: number` (wymagane), bo API zawsze zwraca wartość. Backend zawsze ustawia RelationType przy tworzeniu pozycji |
| 2 | Brak `isDeleted` w web modelach | Entita ma `IsDeleted`, web model go nie eksponuje | Brak w typach UI | Celowe — soft delete jest obsługiwany po stronie API, UI nie powinno widzieć usuniętych elementów jako osobnych stanów. OK. |
| 3 | Brak `IsSelected` w web modelach (API i UI) | Nie istnieje jako pole na entity ani web modelu | Nie istnieje w typach | **KRYTYCZNE** — feature wymaga IsSelected, ale nie jest zaimplementowane nigdzie |
| 4 | Brak `Name` na `CostEstimateGroupWeb` | API zwraca Name przez FieldValues (GroupName), nie ma osobnego pola | UI oczekuje nazwy przez FieldValues | OK — nazwa grupy to wartość pola GroupName |

### Podsumowanie spójności

**Łączna liczba problemów spójności**: 1 (RelationType opcjonalne)
**Brakujące elementy**: IsSelected (całkowicie brak)

---

## BLOK 2 — Stan obecny API

### Encje zaangażowane

| Encja | Plik | Kluczowe properties |
|-------|------|-------------------|
| `CostEstimate` | `CostEstimate.cs` | `TotalNet`, `TotalGross`, `TotalVat`, `Schema`, `AllGroups`, `AllItems` |
| `CostEstimateItem` | `CostEstimateItem.cs` | `ParentItemId`, `RelationType`, `NetValue`, `GrossValue`, `VatValue`, `FieldValues`, `Options`, `Components` |
| `CostEstimateGroup` | `CostEstimateGroup.cs` | `ParentGroupId`, `Level`, `TotalNet`, `TotalGross`, `TotalVat`, `FieldValues`, `Items`, `ChildGroups` |
| `CostEstimateFieldDefinition` | `CostEstimateFieldDefinition.cs` | `FieldScope`, `FieldType`, `Label`, `IsUserDefined`, `CanDelete`, `Order` |
| `CostEstimateItemFieldValue` | `CostEstimateItemFieldValue.cs` | `ItemId`, `FieldDefinitionId`, `StringValue`, `DecimalValue`, `BoolValue`, `DateTimeValue`, `Files` |
| `CostEstimateGroupFieldValue` | `CostEstimateGroupFieldValue.cs` | `GroupId`, `FieldDefinitionId`, value fields |
| `CostEstimateFieldFile` | `CostEstimateFieldFile.cs` | `FieldValueId`, `CostEstimateId`, `OriginalFileName`, `BlobName` |
| `CostEstimateFieldSchema` | `CostEstimateFieldSchema.cs` | `CostEstimateId`, `FieldDefinitions` |
| `ItemRelationType` | `ItemRelationType.cs` | `None=0`, `Option=1`, `Component=2` |
| `FieldScope` | `CostEstimateEnums.cs` | `Group=0`, `ItemSystem=1`, `ItemCalculated=2`, `ItemGeneric=3` |
| `FieldType` | `CostEstimateEnums.cs` | `GroupName=0` ... `ItemGenericDateTime=304` |
| `CostEstimateFieldValueBase` | `CostEstimateFieldValueBase.cs` | `StringValue`, `DecimalValue`, `BoolValue`, `DateTimeValue` |

### Istniejące endpointy (`CostEstimateController.cs`)

| Ścieżka | Metoda | Opis |
|---------|--------|------|
| `/{scope}` | GET | Lista kosztorysów |
| `/details/{id}` | GET | Szczegóły kosztorysu (pełna hierarchia) |
| `/` | POST | Utwórz kosztorys |
| `/{id}` | PUT | Update kosztorysu (name, description) |
| `/{id}` | DELETE | Soft delete kosztorysu |
| `/{id}/copy` | POST | Kopiuj kosztorys |
| `/{id}/items/{itemId}/files` | POST | Upload plików (Replace All) |
| `/{id}/groups` | POST | Dodaj grupę |
| `/{id}/groups/{groupId}` | DELETE | Usuń grupę (soft) |
| `/{id}/groups/reorder` | PUT | Reorder grup |
| `/{id}/items` | POST | Dodaj pozycję |
| `/{id}/items/{itemId}` | DELETE | Usuń pozycję (soft) |
| `/{id}/groups/{groupId}/items/reorder` | PUT | Reorder pozycji w grupie |
| `/{id}/items/{itemId}/move` | PATCH | Przenieś pozycję między grupami |
| `/{id}/recalculate` | POST | Przelicz sumy |
| `/{id}/groups/{groupId}/fields` | PATCH | Upsert pola grupy (autosave) |
| `/{id}/items/{itemId}/fields` | PATCH | Upsert pola pozycji (autosave) |
| `/{id}/schema/fields` | POST | Dodaj definicję pola |
| `/{id}/schema/fields/{fieldId}` | PUT | Update definicji pola |
| `/{id}/schema/fields/{fieldId}` | DELETE | Usuń definicję pola |
| `/{id}/schema/fields/reorder` | POST | Reorder definicji pól |
| `/{id}/shares` | POST/PUT | Share management |

### Serwisy

| Serwis | Interfejs | Metody |
|--------|-----------|--------|
| `CostEstimateCalculationService` | `ICostEstimateCalculationService` | `RecalculateCostEstimate(CostEstimate)` |
| `CostEstimateAccessService` | `ICostEstimateAccessService` | `GetAccessLevelAsync(...)` |
| `CostEstimateCacheService` | `ICostEstimateCacheService` | `GetCostEstimateAsync`, `GetGroupsDictionaryAsync`, `GetItemsDictionaryAsync`, `GetItemFieldValuesDictionaryAsync`, `GetGroupFieldValuesDictionaryAsync`, invalidation methods |
| `CostEstimateShareService` | — | Share operations |

### Stan implementacji

**Zaimplementowane:**
- ✅ Pełny CRUD kosztorysów, grup, pozycji
- ✅ System pól (FieldDefinition z FieldScope/FieldType)
- ✅ User-defined fields (ItemGeneric, Group)
- ✅ Pliki na pozycjach (przez FieldValue → ItemSystemFiles)
- ✅ Kalkulacja wartości finansowych (net/gross/vat)
- ✅ Obsługa opcji i komponentów (RelationType)
- ✅ GetCostEstimateDetails z pełną hierarchią
- ✅ Recalculations
- ✅ Autosave endpoints (PATCH)
- ✅ Soft delete (DeletableEntity)
- ✅ Share system

**Brakuje / do zrobienia:**
- ❌ **IsSelected na encji CostEstimateItem** — nie istnieje
- ❌ **Propagacja opcji w kalkulacjach backendu** — `CalculateItemValues` nie kopiuje wartości z zaznaczonej opcji
- ❌ **IsSelected w kalkulacjach backendu** — `RecalculateGroup` sumuje wszystkie pozycje (lines 67-84), nie sprawdza IsSelected
- ❌ **IsSelected dla komponentów w kalkulacjach** — obie warstwy sumują wszystkie komponenty
- ❌ **Deselect przy exclusive option** — `CheckExclusiveSelectionAsync` rzuca błędem zamiast automatycznie odznaczyć poprzednią opcję
- ❌ **User-defined fields dla grup (FieldScope.Group)** — `AddFieldModal.tsx` wysyła `fieldScope: 3` (ItemGeneric), nie pozwala tworzyć pól grupowych

---

## BLOK 3 — Stan obecny UI

### Komponenty zaangażowane

| Komponent | Plik | Opis |
|-----------|------|------|
| `CostEstimateModernView` | `CostEstimateModernView.tsx` | Wrapper z przełącznikiem Tree/Card |
| `CostEstimateTreeView` | `TreeView/CostEstimateTreeView.tsx` | Widok drzewa (tabela hierarchiczna) |
| `CostEstimateCardView` | `CardView/CostEstimateCardView.tsx` | Widok kart (akordeon) |
| `SortableItemRow` | `rows/SortableItemRow.tsx` | Wiersz pozycji (z opcjami/komponentami) |
| `SortableComponentRow` | `rows/SortableComponentRow.tsx` | Wiersz komponentu |
| `SortableOptionRow` | `rows/SortableOptionRow.tsx` | Wiersz opcji |
| `SortableGroupRow` | `rows/SortableGroupRow.tsx` | Wiersz grupy |
| `FileFieldRenderer` | `FileFieldRenderer.tsx` | Renderer pól plików (z modalem) |
| `SchemaManagerModal` | `SchemaManager/SchemaManagerModal.tsx` | Zarządzanie schematem pól |
| `AddFieldModal` | `SchemaManager/AddFieldModal.tsx` | Dodawanie pola użytkownika |
| `FieldDefinitionList` | `SchemaManager/FieldDefinitionList.tsx` | Lista definicji pól |
| `CostEstimateEditPage` | `CostEstimateEditPage.tsx` | Strona edycji kosztorysu |

### Hooki

| Hook | Plik | Opis |
|------|------|------|
| `useFieldAutosave` | `hooks/useFieldAutosave.ts` | Autosave z debounce 700ms |
| `useCostEstimateDetails` | `hooks/queries/useCostEstimate.ts` | React Query dla szczegółów |
| `useCostEstimateDetails` (legacy) | `hooks/useCostEstimate.ts` | Legacy hook z useState |

### API calle

| Funkcja | Plik | Endpoint |
|---------|------|----------|
| `getCostEstimateDetails` | `costEstimateApi.ts` | GET `/details/{id}` |
| `addGroup` | `costEstimateApi.ts` | POST `/{id}/groups` |
| `deleteGroup` | `costEstimateApi.ts` | DELETE `/{id}/groups/{groupId}` |
| `addItem` | `costEstimateApi.ts` | POST `/{id}/items` |
| `deleteItem` | `costEstimateApi.ts` | DELETE `/{id}/items/{itemId}` |
| `upsertGroupField` | `costEstimateApi.ts` | PATCH `/{id}/groups/{groupId}/fields` |
| `upsertItemField` | `costEstimateApi.ts` | PATCH `/{id}/items/{itemId}/fields` |
| `uploadCostEstimateItemFiles` | `costEstimateApi.ts` | POST `/{id}/items/{itemId}/files` |
| `addFieldDefinition` | `costEstimateApi.ts` | POST `/{id}/schema/fields` |
| `recalculate` | `costEstimateApi.ts` | POST `/{id}/recalculate` |

### Uwagi o stanie UI

- UI `recalculateCostEstimateDetails.ts` ma **bardziej zaawansowaną logikę niż backend** — obsługuje IsSelected, propagację opcji
- `AddFieldModal.tsx` używa **osobnych funkcji** z `costEstimateApi.ts` (lines 543-553) które mają **inną ścieżkę URL** (`cost-estimates` zamiast `cost-estimate`) — potencjalnie zepsute
- UI ma dwie wersje hooków (React Query + legacy) — możliwy konflikt

---

## BLOK 4 — Luki i braki

| # | Brak / Luka | Warstwa | Priorytet | Opis |
|---|-------------|---------|-----------|------|
| 1 | **IsSelected nie istnieje na encji** | API | Wysoki | `CostEstimateItem` nie ma pola `IsSelected`. Feature wymaga go dla wszystkich typów (None/Option/Component) |
| 2 | **Brak propagacji opcji w kalkulacjach backendu** | API | Wysoki | `CostEstimateCalculationService.CalculateItemValues` nie kopiuje wartości z zaznaczonej opcji do rodzica. UI ma tę logikę w `recalculateCostEstimateDetails.ts` (lines 301-323) |
| 3 | **Brak IsSelected w backend kalkulacjach** | API | Wysoki | `RecalculateGroup` (line 67-84) sumuje WSZYSTKIE pozycje, nie filtruje po IsSelected. UI filtruje |
| 4 | **Brak IsSelected dla komponentów w kalkulacjach** | API+UI | Wysoki | Żadna warstwa nie filtruje komponentów po IsSelected przy sumowaniu do pozycji nadrzędnej |
| 5 | **CheckExclusiveSelectionAsync rzuca błędem zamiast deselect** | API | Średni | `UpsertCostEstimateItemFieldCommandHandler` (lines 216-260) rzuca `ValidationApiException` gdy próbujesz zaznaczyć opcję inna już jest zaznaczona. Zamiast tego powinien automatycznie odznaczyć poprzednią |
| 6 | **AddFieldModal wysyła zły URL** | UI | Średni | `costEstimateApi.ts` lines 543-553: `addFieldDefinition` używa ścieżki `cost-estimates` (z 's') zamiast `cost-estimate`. Reszta API klienta używa `cost-estimate`. To spowoduje 404 |
| 7 | **User-defined fields tylko dla pozycji (ItemGeneric), nie dla grup** | API+UI | Średni | `AddFieldDefinitionCommandValidator` (line 38) pozwala tylko `FieldScope.ItemGeneric` lub `FieldScope.Group`. UI `AddFieldModal.tsx` wysyła zawsze `fieldScope: 3` (ItemGeneric). Feature wymaga wspólnych pól dla etapów i pozycji |
| 8 | **Brak sortowania/filtrowania/wyszukiwania** | UI | Średni | Feature wymaga sortowania po kolumnach i wyszukiwania po nazwie. Obecne widoki tego nie implementują |
| 9 | **Brak `selectedOptionId` cache na encji** | API | Niski | Dla szybkiego odczytu która opcja jest aktywna, przydałoby się pole `SelectedOptionId` na `CostEstimateItem` lub cache |
| 10 | **Brak `isSelected` w web modelu item** | API+UI | Wysoki | Web model `CostEstimateItemWeb` nie ma pola IsSelected. UI też go nie ma w typach |

---

## BLOK 5 — Zmiany w encjach/DB

| Encja | Zmiana | Typ (nowa / nowe pole / relacja) | Wymaga migracji |
|-------|--------|----------------------------------|-----------------|
| `CostEstimateItem` | Dodaj `IsSelected` (`bool`, default `true`) | Nowe pole | **Tak** |
| `CostEstimateItem` | Opcjonalnie: dodaj `SelectedOptionId` (`Guid?`) dla szybkiego dostępu | Nowe pole | Opcjonalnie |
| `CostEstimateFieldFile` | Opcjonalnie: przenieś relację z `FieldValueId` → `ItemId` | Zmiana relacji | **Tak** (znacząca) |
| `FieldScope` | Potencjalnie usuń lub uprość — ale to zmiana paradygmatu | Nowa strategia | **Tak** (bardzo duża) |

### Uwaga o FieldScope
Usunięcie `FieldScope` to zmiana fundamentalna — wymaga przeprojektowania jak kategoryzujemy pola. Obecnie `FieldScope` jest używane w:
- Encji `CostEstimateFieldDefinition` (line 27)
- Kalkulacjach (filtrowanie po `ItemCalculated`, `ItemSystem`)
- Walidatorze `AddFieldDefinitionCommandValidator`
- Web modelach (przekazywane do UI)
- UI (filtrowanie, renderowanie)

Zamiast usuwać `FieldScope`, proponuję:
1. Dodać możliwość przypisania user-defined field DO WSZYSTKICH scope'ów
2. Pola typu `ItemGeneric` mogą być przypisane do grup (etapów) i pozycji
3. Pole `appliesToGroups` i `appliesToItems` jako flagi na `FieldDefinition`

---

## BLOK 6 — Zmiany w CQRS i kontrolerach

| Command/Query/Endpoint | Typ (nowy/modyfikacja) | Warstwa | Opis |
|------------------------|------------------------|---------|------|
| `AddCostEstimateItemCommandHandler` | Modyfikacja | CQRS | Dodaj ustawienie `IsSelected = true` dla nowych pozycji (RelationType=None/Component) |
| `UpsertCostEstimateItemFieldCommandHandler` | Modyfikacja | CQRS | `CheckExclusiveSelectionAsync` — zamiast rzucać błędem, odznacz poprzednią opcję (auto-deselect) |
| `CostEstimateCalculationService` | Modyfikacja | Serwis | Dodaj: (1) propagację wartości z zaznaczonej opcji, (2) filtrowanie po IsSelected przy sumowaniu do grupy, (3) filtrowanie komponentów po IsSelected |
| `RecalculateCostEstimateCommandHandler` | Modyfikacja | CQRS | Bez zmian w handlerze (zmiany w serwisie) |
| `GetCostEstimateDetailsQueryHandler` | Modyfikacja | CQRS | Dodaj `IsSelected` do `CostEstimateItemWeb` |
| `CostEstimateItemWeb` (record) | Modyfikacja | Web Model | Dodaj `bool IsSelected` |
| Nowy: `SetItemIsSelectedCommand` | Nowy | CQRS | Osobny endpoint do zmiany IsSelected dla itemów/opcji/komponentów. Ważne dla exclusive selection opcji |
| Nowy: `SetItemIsSelectedCommandHandler` | Nowy | CQRS | Obsługa auto-deselect dla opcji, trigger recalculation |
| Kontroler: `PATCH /{id}/items/{itemId}/select` | Nowy | Controller | Endpoint do zmiany IsSelected |
| Kontroler: `PATCH /{id}/groups/{groupId}/fields` | Modyfikacja | Controller | Możliwość obsługi pól Group-scope dla user-defined fields |
| `CostEstimateFieldFile` | Modyfikacja (opcjonalnie) | Encja | Jeśli przenosimy pliki na Item: `FieldValueId` → `ItemId`. To zmienia strukturę DB i endpoint upload |

---

## BLOK 7 — Zmiany w komponentach UI

| Komponent/Hook/Typ | Typ (nowy/modyfikacja) | Opis |
|--------------------|------------------------|------|
| `costEstimate.types.new.ts` `CostEstimateItemWeb` | Modyfikacja | Dodaj `isSelected: boolean` |
| `costEstimate.types.new.ts` `CostEstimateItemDto` | Modyfikacja | Dodaj `isSelected?: boolean` |
| `costEstimate.types.new.ts` `AddItemRequestDto` | Modyfikacja | Dodaj `isSelected?: boolean` |
| `recalculateCostEstimateDetails.ts` | Modyfikacja | Dodaj filtrowanie komponentów po IsSelected przy sumowaniu (linie 277-299) |
| `SortableItemRow.tsx` | Modyfikacja | Dodaj checkbox IsSelected dla pozycji głównej (None) do sumowania w etapie |
| `SortableComponentRow.tsx` | Modyfikacja | Dodaj checkbox IsSelected dla komponentu do sumowania w pozycji |
| `SortableOptionRow.tsx` | Modyfikacja | Zmień checkbox IsSelected na radio button (option exclusive) |
| `AddFieldModal.tsx` | Modyfikacja | Dodaj możliwość wyboru scope (ItemGeneric/Group) dla nowego pola. Popraw URL API |
| `costEstimateApi.ts` standalone exports | Modyfikacja | Popraw URL z `cost-estimates` → `cost-estimate` (linie 543-553) |
| `useFieldAutosave.ts` | Modyfikacja | Dodaj wsparcie dla obsługi błędu exclusive selection (jeśli API zwróci 409) |
| Nowy: `useItemSelection.ts` | Nowy hook | Hook do zarządzania IsSelected z optimistic update |
| Nowy: Search input | Nowy komponent | Wyszukiwanie po nazwie i polach tekstowych (feature requirement) |
| Nowy: Sort controls | Nowy komponent | Sortowanie po kolumnach (feature requirement) |

---

## BLOK 8 — Problemy i ryzyka

| # | Problem | Warstwa | Ryzyko | Rekomendacja |
|---|---------|---------|--------|-------------|
| 1 | **IsSelected całkowicie brak** | API+UI | **WYSOKIE** | IsSelected to kluczowy feature. Wymaga: (a) migracji DB, (b) zmian w encji, (c) web modelach, (d) kalkulacjach, (e) UI. Bez tego feature jest niemożliwy |
| 2 | **Niespójność kalkulacji API ↔ UI** | API+UI | **WYSOKIE** | UI ma bardziej zaawansowaną logikę (IsSelected, propagacja opcji) niż backend. Po wdrożeniu recalculation, wartości w backendzie mogą się różnić od UI preview |
| 3 | **FieldScope removal to ogromna zmiana** | API+UI | **WYSOKIE** | ~50+ miejsc w API i ~30+ w UI używa FieldScope. Zamiast usuwać, lepiej dodać flagi `appliesToGroups` / `appliesToItems` na FieldDefinition |
| 4 | **Dwa zestawy funkcji API w costEstimateApi.ts** | UI | **ŚREDNIE** | Schema management ma DWA zestawy funkcji: jedne w `costEstimateApi` obiekcie, drugie jako standalone exporty. Różnią się URL-ami (`cost-estimate` vs `cost-estimates`). Standalone exporty są zepsute |
| 5 | **CheckExclusiveSelectionAsync z ValidationApiException** | API | **ŚREDNIE** | Rzucanie błędu gdy user próbuje zaznaczyć drugą opcję jest złym UX. Zamiast tego handler powinien automatycznie odznaczyć poprzednią + zaktualizować cache |
| 6 | **Brak synchronizacji kalkulacji po autosave** | API | **ŚREDNIE** | Autosave endpointy (PATCH) nie triggerują recalculation. UI musi jawnie wołać `POST /recalculate`. Przy autosave z debounce 700ms, jeśli user szybko edytuje wiele pól, każde wywołanie recalculation jest kosztowne |
| 7 | **AddFieldModal używa fieldScope: 3 (ItemGeneric) na sztywno** | UI | **NISKIE** | `AddFieldModal.tsx:67` — zawsze wysyła `fieldScope: 3`. Feature wymaga wspólnych pól dla etapów i pozycji. Musi być opcja wyboru |
| 8 | **Soft delete grup — item field values hard-deleted** | API | **NISKIE** | `DeleteCostEstimateGroupCommandHandler` line 134: `ExecuteDeleteAsync` usuwa na stałe item field values. Jeśli przywracamy grupę, field values są stracone. Dla spójności z soft delete, field values też powinny być soft-deleted (ale nie mają `IsDeleted`) |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Nowe encje | 0 |
| Zmiany w encjach | 2 (`CostEstimateItem.IsSelected`, opcjonalnie `SelectedOptionId`) |
| Nowe Commands | 1 (`SetItemIsSelectedCommand`) |
| Nowe Queries | 0 |
| Nowe endpointy | 1 (`PATCH /{id}/items/{itemId}/select`) |
| Nowe serwisy | 0 |
| Nowe komponenty UI | 2 (search input, sort controls) |
| Zmiany w komponentach UI | 5 (`SortableItemRow`, `SortableComponentRow`, `SortableOptionRow`, `AddFieldModal`, `FileFieldRenderer`) |
| Nowe hooki UI | 1 (`useItemSelection`) |
| Zmiany w hookach UI | 1 (`useFieldAutosave`) |
| Nowe typy UI | 0 |
| Zmiany w typach UI | 2 (`CostEstimateItemWeb`, `CostEstimateItemDto`) |
| Wymaga migracji DB | **Tak** (dodanie `IsSelected` na `CostEstimateItem`) |
| Spójność API↔UI | 1 drobny problem + 1 brakujący element (IsSelected) |
| Pytania domenowe | 5 |

---

## Pytania domenowe wymagające decyzji

1. **IsSelected — jak traktować różne RelationType?**
   - Dla `None` (pozycja główna): checkbox do sumowania w etapie, default `true`
   - Dla `Option` (opcja): radio button do wyboru wariantu, tylko jedna zaznaczona
   - Dla `Component` (komponent): checkbox do sumowania w pozycji, default `true`
   - Czy te zachowania mają być wymuszane przez API (validation) czy tylko przez UI?

2. **FieldScope — usunąć czy dodać flagi?**
   - Obecny system z FieldScope.Group/ItemSystem/ItemCalculated/ItemGeneric jest głęboko zintegrowany
   - Propozycja: zamiast usuwać FieldScope, dodać flagi `appliesToGroups` i `appliesToItems`
   - Czy to akceptowalne, czy konieczne jest całkowite usunięcie FieldScope?

3. **Auto-deselect przy exclusive options?**
   - Gdy user zaznacza opcję B, a opcja A jest już zaznaczona:
     - **Opcja 1**: API zwraca błąd (obecne zachowanie) — user musi ręcznie odznaczyć A przed zaznaczeniem B
     - **Opcja 2**: API automatycznie odznacza A i zapisuje zaznaczenie B — lepszy UX
   - Którą opcję wybrać?

4. **Czy user-defined fields mają być wspólne (ta sama definicja dla grup i pozycji)?**
   - Feature spec mówi "wspólne dla etapów i pozycji (te same definicje pól)"
   - Czy to oznacza, że jedna definicja pola (FieldDefinition) może być użyta zarówno dla grupy jak i pozycji?
   - Czy user-defined field powinien mieć opcję "Dostępne dla: Etapy | Pozycje | Oba"?

5. **Pliki — przenoszenie z FieldValue na Item?**
   - Obecnie `CostEstimateFieldFile` jest powiązane z `CostEstimateItemFieldValue` przez `FieldValueId`
   - Feature wymaga plików "na pozycjach" — czy to znaczy bezpośrednio na `CostEstimateItem`?
   - Jeśli tak, to czy usuwamy pole typu `ItemSystemFiles` (fieldType=105) i robimy osobne pole "Pliki" na encji?
   - Czy upload endpoint ma być zmieniony z `POST /items/{itemId}/files` (obecnie wymaga fieldDefinitionId) na prostszy `POST /items/{itemId}/files`?

## Kluczowe znaleziska dla każdego obszaru

### 1. Spójność API↔UI
- Modele są generalnie spójne — jeden drobny problem: `relationType?: number` w UI powinno być wymagane
- **Główny brak**: `IsSelected` nie istnieje nigdzie. Web model `CostEstimateItemWeb` go nie ma.

### 2. FieldScope removal impact
- **Ogromny impact**: ~50 miejsc w API + ~30 w UI
- Główne użycia: (a) filtrowanie w kalkulacjach, (b) walidacja nowych pól, (c) mapowanie na web modele, (d) UI helpery
- **Rekomendacja**: Zamiast usuwać, dodać flagi `appliesToGroups` / `appliesToItems`

### 3. Obsługa opcji (Option propagation)
- **API**: `CheckExclusiveSelectionAsync` istnieje w `UpsertCostEstimateItemFieldCommandHandler` (lines 216-260) — sprawdza czy nie ma już zaznaczonej opcji, rzuca błędem jeśli tak. Nie ma auto-deselect
- **API**: `CostEstimateCalculationService.CalculateItemValues` (lines 112-186) — **NIE MA** propagacji opcji. Nie kopiuje wartości z opcji do rodzica
- **UI**: `recalculateCostEstimateDetails.ts` (lines 301-323) — **MA** propagację opcji. Kopiuje wszystkie calculated fields z zaznaczonej opcji do rodzica
- **Wniosek**: Backend i UI są niespójne. UI robi to dobrze, backend nie robi tego wcale

### 4. Obsługa komponentów (Component summation)
- **API**: `CalculateItemValues` (lines 119-153) — sumuje wszystkie `components` (bez IsSelected), rekurencyjnie wywołuje `CalculateItemValues` dla każdego
- **UI**: `calculateItemValues` (lines 277-299) — sumuje wszystkie `components` (bez IsSelected), ale nie wywołuje rekurencyjnie `calculateDerivedValues` dla komponentów (tylko dla itemów)
- **Wniosek**: API i UI są zgodne w sumowaniu wszystkich komponentów, ale obie warstwy nie obsługują IsSelected dla komponentów

### 5. IsSelected na Item
- **Nie istnieje** w żadnej warstwie
- `CostEstimateItem` entity nie ma pola IsSelected
- `CostEstimateItemWeb` web model nie ma pola IsSelected
- UI types nie mają IsSelected
- Kalkulacje nie używają IsSelected (backend) lub używają tylko dla pozycji głównej (UI)
- **Wniosek**: To największy brak — kluczowy feature wymaga dodania IsSelected od encji po UI

### 6. Pliki na pozycjach
- **Obecny model**: `CostEstimateFieldFile` → `CostEstimateItemFieldValue` → `CostEstimateItem`
- Pliki są powiązane z polem typu `ItemSystemFiles` (fieldType=105)
- Upload: `POST /{id}/items/{itemId}/files` z `fieldDefinitionId` i listą plików (Replace All)
- **Problem**: Pliki nie są bezpośrednio na itemie — są na konkretnym polu. Jeśli chcemy "pliki na pozycji", to obecny model wymaga utworzenia FieldValue dla pola ItemSystemFiles
- **Możliwe podejścia**: (a) zostawić obecny model (files na field value), (b) dodać `ICollection<CostEstimateFieldFile> Files` bezpośrednio na `CostEstimateItem`, (c) zrobić osobne pole IsSelectedFiles (jak ItemSystemFiles na stałe)

### 7. User-defined fields (ItemGeneric)
- `AddFieldDefinitionCommandValidator` pozwala tylko `FieldScope.ItemGeneric` (3) lub `FieldScope.Group` (0) — line 38
- `AddFieldModal.tsx` wysyła `fieldScope: 3` (ItemGeneric) — line 67
- **Stan**: user-defined fields działają tylko dla pozycji (ItemGeneric). Nie można dodać user-defined field dla grupy z UI
- **Validator sprawdza**: FieldType musi pasować do FieldScope (ItemGeneric: 300-399, Group: 0-99)
- **Wniosek**: Feature wymaga wspólnych pól — obecnie nie ma mechanizmu by jedna definicja była używana i przez grupy i przez pozycje

### 8. Soft delete grup rekurencyjny
- `DeleteCostEstimateGroupCommandHandler` (lines 196-232) — `CollectDescendantGroupIds` zbiera wszystkie grupy potomne (BFS)
- Następnie: (1) soft-delete plików, (2) hard-delete item field values, (3) hard-delete group field values, (4) soft-delete items, (5) soft-delete groups
- **Problem**: Item field values i group field values są HARD-deleted (`ExecuteDeleteAsync`), nie soft-deleted. To może być problem przy ewentualnym przywracaniu
- **Problem**: Pliki są soft-deleted, ale blob w Azure jest już usunięty — nie ma możliwości przywrócenia

### 9. Autosave
- `useFieldAutosave.ts` — dobrze zaimplementowany hook z debounce 700ms
- Wzorzec: `scheduleFieldSave` → debounce → PATCH request → `onSaveSuccess` callback dla optimistic update
- `flushPendingChanges` — zapisuje wszystkie oczekujące zmiany (używane przy nawigacji)
- **Brak**: Autosave nie triggeruje recalculation. UI musi jawnie wołać `POST /recalculate`. To może być problem z wydajnością
- **Brak**: Nie ma obsługi błędu 409 (conflict) dla exclusive selection

### 10. Kalkulacje — porównanie backend vs UI
| Aspekt | Backend (`CostEstimateCalculationService.cs`) | UI (`recalculateCostEstimateDetails.ts`) |
|--------|----------------------------------------------|------------------------------------------|
| **Synchroniczność** | Synchroniczna (void) | Synchroniczna (pure function) |
| **IsSelected dla pozycji** | **NIE** — sumuje wszystkie (line 71-84) | **TAK** — filtruje przez `isItemSelected` |
| **IsSelected dla komponentów** | **NIE** — sumuje wszystkie | **NIE** — sumuje wszystkie |
| **Propagacja opcji** | **NIE** | **TAK** (lines 301-323) |
| **Kalkulacja pochodnych** | unitPrice × quantity, VAT | unitPrice × quantity, VAT |
| **Czyszczenie nieaktualnych wartości** | **NIE** — nadpisuje DecimalValue | **TAK** — usuwa wpis z fieldValues (`clearItemFieldValue`) |
| **Zapis do DB** | Mutuje encje, EF save | Tylko preview, nie zapisuje |

**Główne różnice**:
1. UI obsługuje IsSelected, backend nie
2. UI obsługuje propagację opcji, backend nie
3. UI czyści nieaktualne wartości kalkulowane, backend nadpisuje je

---

## Pliki do zmiany (konkretne linie)

### API (C#)

| Plik | Linie | Zmiana |
|------|-------|--------|
| `Entities/Models/CostEstimates/CostEstimateItem.cs` | 19-118 | Dodaj `public bool IsSelected { get; set; } = true;` |
| `Entities/Configurations/CostEstimateItemConfiguration.cs` | 10-88 | Dodaj konfigurację `IsSelected` |
| `Business/Interfaces/WebModels/CostEstimates/CostEstimateDataWeb.cs` | 41-55 | Dodaj `bool IsSelected` do `CostEstimateItemWeb` |
| `Business/Interfaces/WebModels/CostEstimates/CostEstimateMutationDto.cs` | 32-40 | Dodaj `bool? IsSelected` do `CostEstimateItemDto` |
| `Business/Implementation/Services/CostEstimateCalculationService.cs` | 64-85 | Dodaj filtrowanie po IsSelected przy sumowaniu do grupy |
| `Business/Implementation/Services/CostEstimateCalculationService.cs` | 119-153 | Dodaj (1) propagację opcji, (2) filtrowanie komponentów po IsSelected |
| `CQRS/CostEstimates/UpsertCostEstimateItemField/UpsertCostEstimateItemFieldCommandHandler.cs` | 216-260 | Zmień `CheckExclusiveSelectionAsync` — auto-deselect zamiast błędu |
| `CQRS/CostEstimates/AddCostEstimateItem/AddCostEstimateItemCommandHandler.cs` | 92-102 | Ustaw `IsSelected = true` dla nowych pozycji głównych i komponentów |
| `CQRS/CostEstimates/GetCostEstimateDetails/GetCostEstimateDetailsQueryHandler.cs` | 327-381 | Dodaj `IsSelected: item.IsSelected` do `CostEstimateItemWeb` |
| `CQRS/CostEstimates/AddFieldDefinition/AddFieldDefinitionCommandValidator.cs` | 37-39 | Rozszerz dozwolone scope (jeśli zmieniamy strategię) |
| `CQRS/CostEstimates/DeleteCostEstimateGroup/DeleteCostEstimateGroupCommandHandler.cs` | 133-140 | Rozważ soft-delete dla field values zamiast hard-delete |

### UI (TypeScript/React)

| Plik | Linie | Zmiana |
|------|-------|--------|
| `types/costEstimate.types.new.ts` | 193-207 | Dodaj `isSelected: boolean` do `CostEstimateItemWeb` |
| `types/costEstimate.types.new.ts` | 105-113 | Dodaj `isSelected?: boolean` do `CostEstimateItemDto` |
| `types/costEstimate.types.new.ts` | 363-368 | Dodaj `isSelected?: boolean` do `AddItemRequestDto` |
| `types/costEstimate.types.new.ts` | 197 | Zmień `relationType?: number` → `relationType: number` |
| `api/costEstimateApi.ts` | 543-553 | Popraw URL: `cost-estimates` → `cost-estimate` |
| `components/CostEstimate/rows/SortableItemRow.tsx` | + | Dodaj checkbox IsSelected dla pozycji głównej |
| `components/CostEstimate/rows/SortableComponentRow.tsx` | + | Dodaj checkbox IsSelected dla komponentów |
| `components/CostEstimate/rows/SortableOptionRow.tsx` | + | Zmień na radio button (exclusive) |
| `components/CostEstimate/SchemaManager/AddFieldModal.tsx` | 31-36, 64-68 | Dodaj opcję wyboru scope, popraw URL |
| `utils/recalculateCostEstimateDetails.ts` | 277-299 | Dodaj filtrowanie komponentów po IsSelected |
