# UI Fix 06: CardView + SchemaManager + pliki + recalculate

## Kontekst
Feature: costestimate-full-refactor — patrz `.opencode/features/costestimate-full-refactor.md`

Ostatni krok UI: dostosowanie CardView, SchemaManager, recalculate utility i integracja plików.

## Do zrobienia

### 1. Modyfikacja `CostEstimateCardView.tsx`

Dostosuj do nowej struktury:

```typescript
interface CostEstimateCardViewProps {
  details: CostEstimateDetailsWeb;
  isEditMode: boolean;
  onFieldChange: (field: string, value: any) => void;
  onFieldAutosave?: (params: AutosaveParams) => void;
  onAddGroup: () => void;
  onAddSubGroup: (parentGroupId: string) => void;
  onAddItem: (groupId: string) => void;
  onAddComponent: (groupId: string, itemId: string) => void;
  onAddOption: (groupId: string, itemId: string) => void;
  onDeleteGroup: (groupId: string) => void;
  onDeleteItem: (groupId: string, itemId: string) => void;
  onSelectOption: (groupId: string, itemId: string, optionId: string) => void;
  onUploadFiles: (itemId: string) => void;
}
```

- Renderuj grupy jako karty (StageCard)
- Renderuj pozycje w grupach (PositionCard)
- Renderuj opcje/komponenty wewnątrz pozycji
- Dodaj pola dodatkowe do każdej karty

### 2. Modyfikacja `StageCard.tsx`

- Wyświetl: Name, NetValue, GrossValue, AdditionalFieldValues
- Przyciski: dodaj pozycję, dodaj podetap, usuń

### 3. Modyfikacja `PositionCard.tsx`

- Wyświetl: wszystkie base fields + additional fields
- Checkbox IsSelected
- Checkbox IsStageWork (dla None)
- Radio button dla opcji
- Checkbox dla komponentów
- Przycisk plików

### 4. Przebudowa SchemaManager

#### Modyfikacja `SchemaManagerModal.tsx`

- Usuń starą strukturę (FieldDefinition, FieldScope, FieldTypeConfig)
- Nowa struktura: tylko `CostEstimateAdditionalField[]`
- Użyj nowych endpointów: `getAdditionalFields`, `addAdditionalField`, `updateAdditionalField`, `deleteAdditionalField`, `reorderAdditionalFields`

#### Modyfikacja `AddFieldModal.tsx`

- **Usuń**: `fieldScope`, `fieldTypeConfig`, stare typy pól
- **Nowe pola**:
  - Nazwa pola (text input)
  - Typ pola (select): String, Decimal, Boolean, DateTime
  - Kolejność (number, opcjonalne)
- **Popraw URL**: używaj `addAdditionalField` z nowego API clienta (NIE starego `cost-estimates` z 's')

#### Modyfikacja `FieldDefinitionList.tsx`

- Zmień na listę `CostEstimateAdditionalField[]`
- Drag & drop reorder (użyj `reorderAdditionalFields`)
- Edycja nazwy w miejscu
- Delete z potwierdzeniem

#### Modyfikacja `FieldDefinitionRow.tsx`

- Wyświetl: Nazwa, Typ (String/Decimal/Boolean/DateTime), Kolejność
- Przyciski: Edytuj, Usuń

### 5. Modyfikacja `recalculateCostEstimateDetails.ts`

Dostosuj do nowej struktury — zamiast FieldValues, pracuj na direct properties:

```typescript
export function recalculateCostEstimateDetails(
  details: CostEstimateDetailsWeb
): CostEstimateDetailsWeb {
  // Klonuj dane
  const result = JSON.parse(JSON.stringify(details)) as CostEstimateDetailsWeb;
  
  // Rekurencyjnie przelicz grupy
  for (const group of result.rootGroups) {
    recalculateGroup(group);
  }
  
  // Sumy kosztorysu
  result.totalNet = result.rootGroups.reduce((sum, g) => sum + (g.totalNet ?? 0), 0);
  result.totalGross = result.rootGroups.reduce((sum, g) => sum + (g.totalGross ?? 0), 0);
  result.totalVat = result.rootGroups.reduce((sum, g) => sum + (g.totalVat ?? 0), 0);
  
  return result;
}

function recalculateGroup(group: CostEstimateGroupWeb) {
  // Sumuj tylko pozycje z IsSelected = true
  const selectedItems = (group.items || []).filter(i => i.isSelected);
  
  for (const item of selectedItems) {
    recalculateItem(item);
  }
  
  group.totalNet = selectedItems.reduce((sum, i) => sum + (i.netValue ?? 0), 0);
  group.totalGross = selectedItems.reduce((sum, i) => sum + (i.grossValue ?? 0), 0);
  group.totalVat = selectedItems.reduce((sum, i) => sum + (i.vatValue ?? 0), 0);
  
  // Rekurencyjnie podgrupy
  for (const childGroup of group.childGroups || []) {
    recalculateGroup(childGroup);
    group.totalNet! += childGroup.totalNet ?? 0;
    group.totalGross! += childGroup.totalGross ?? 0;
    group.totalVat! += childGroup.totalVat ?? 0;
  }
}

function recalculateItem(item: CostEstimateItemWeb) {
  // Jeśli ma opcje → wartości z zaznaczonej opcji
  if (item.options && item.options.length > 0) {
    const selectedOption = item.options.find(o => o.isSelected);
    if (selectedOption) {
      item.netValue = selectedOption.netValue;
      item.grossValue = selectedOption.grossValue;
      item.vatValue = selectedOption.vatValue;
    } else {
      item.netValue = undefined;
      item.grossValue = undefined;
      item.vatValue = undefined;
    }
    return;
  }
  
  // Jeśli ma komponenty → suma z IsSelected=true
  if (item.components && item.components.length > 0) {
    const selectedComponents = item.components.filter(c => c.isSelected);
    item.netValue = selectedComponents.reduce((sum, c) => sum + (c.netValue ?? 0), 0);
    item.grossValue = selectedComponents.reduce((sum, c) => sum + (c.grossValue ?? 0), 0);
    item.vatValue = selectedComponents.reduce((sum, c) => sum + (c.vatValue ?? 0), 0);
    // Dla komponentów: item nie ma własnych wartości (quantity, unit itd. = null)
    return;
  }
  
  // Własne wartości — kalkulacja
  if (item.quantity && item.unitPriceNet) {
    item.netValue = item.quantity * item.unitPriceNet;
  }
  if (item.netValue && item.vatRate) {
    item.vatValue = item.netValue * item.vatRate;
  }
  if (item.netValue && item.vatValue) {
    item.grossValue = item.netValue + item.vatValue;
  }
  // UnitPriceGross
  if (item.unitPriceNet && item.vatRate) {
    item.unitPriceGross = item.unitPriceNet * (1 + item.vatRate);
  }
}
```

**WAŻNE**: Ta funkcja musi być SYNCHRONICZNA z `CostEstimateCalculationService.cs` (API Fix 07).

### 6. Integracja plików w UI

W komponentach gdzie wyświetlane są pliki:
- Użyj `CostEstimateItemFileWeb` zamiast starego `CostEstimateFieldFileWeb`
- Użyj nowych endpointów: `uploadItemFiles`, `deleteItemFile`, `replaceItemFiles`
- Modal/listę plików dostosuj do `itemId` zamiast `fieldValueId`

### 7. Modyfikacja `CostEstimateModernView.tsx`

Dostosuj propsy — zmień typ callbacków na nowe:
```typescript
// Usuń stare:
// onFieldChange: (groupId, itemId, fieldId, value) => void
// onFieldAutosave: (params z fieldDefinitionId) => void

// Dodaj nowe:
onFieldChange: (entityType: 'group' | 'item', entityId: string, field: string, value: any) => void
onFieldAutosave?: (params: AutosaveParams) => void
```

Dostosuj do przekazywania `details.additionalFields` zamiast `details.schema` do widoków.

### 8. Modyfikacja strony edycji `CostEstimateEditPage.tsx` (lub odpowiednika)

- Dostosuj pobieranie danych: `details` zawiera `additionalFields` zamiast `schema`
- Podmień callbacki
- Dodaj obsługę `setItemIsSelected`

### Build

```powershell
npm run build
```
Jeśli build failed, przerwij i zgłoś błędy.
