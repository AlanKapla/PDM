# UI Fix 05: Przebudowa wierszy (ItemRow, OptionRow, ComponentRow)

## Kontekst
Feature: costestimate-full-refactor — patrz `.opencode/features/costestimate-full-refactor.md`

Dostosowanie komponentów wierszy do nowej struktury: IsSelected checkbox/radio, IsStageWork, additional fields, direct properties.

## Do zrobienia

### 1. Modyfikacja `SortableItemRow.tsx`

**Nowe propsy**:
```typescript
interface SortableItemRowProps {
  item: CostEstimateItemWeb;
  groupId: string;
  level: number;
  isEditMode: boolean;
  baseColumns: ColumnDef[];
  additionalColumns: ColumnDef[];
  additionalFieldDefs: CostEstimateAdditionalFieldWeb[];
  onFieldChange: (field: string, value: any) => void;
  onFieldAutosave?: (params: AutosaveParams) => void;
  onAddComponent: (itemId: string) => void;
  onAddOption: (itemId: string) => void;
  onSelectOption: (itemId: string, optionId: string) => void;
  onUploadFiles: (itemId: string) => void;
  onDeleteItem: (itemId: string) => void;
}
```

**Renderowanie**:
1. **Checkbox IsSelected** (po lewej stronie, dla RelationType=None):
   - Domyślnie checked (true)
   - Kliknięcie → onSelectOption (dla opcji) lub setItemIsSelected (dla pozycji)
   - Dla pozycji głównej: zmienia czy pozycja jest sumowana do grupy

2. **Pola podstawowe**:
   - Name: input text (edytowalny)
   - Quantity: input number
   - Unit: combobox z jednostkami (szt, m, m², m³, kg, mb, godz, kpl) + opcja wpisania własnej
   - UnitPriceNet: input number (decimal)
   - VatRate: select dropdown (0%, 5%, 8%, 23% lub custom)
   - UnitPriceGross: readonly (obliczane)
   - NetValue: input number (lub readonly jeśli obliczane z UnitPriceNet × Quantity)
   - GrossValue: readonly
   - VatValue: readonly

3. **Pola dodatkowe** (z `additionalFieldDefs`):
   - Renderuj odpowiedni input w zależności od `fieldType`
   - Pobierz wartość z `item.additionalFieldValues`
   - Zapisz przez `upsertItemAdditionalField`
   - Wartość: string → text input, decimal → number input, bool → checkbox, datetime → date input

4. **Checkbox IsStageWork** (tylko dla RelationType=None):
   - Mała ikonka/checkbox w wierszu
   - Tooltip: "Dodaj do harmonogramu"

5. **Przycisk plików**:
   - Ikona paperclip/files
   - Wyświetla liczbę załączonych plików
   - Kliknięcie → modal z listą plików + upload

6. **Action buttons**:
   - Dodaj komponent, Dodaj opcję, Usuń

### 2. Modyfikacja `SortableOptionRow.tsx`

**Zmiana**: checkbox na radio button (exclusive selection)

**Renderowanie**:
1. **Radio button** (zamiast checkboxa):
   - Tylko jedna opcja może być zaznaczona
   - Kliknięcie → `onSelectOption(itemId, optionId)` → API woła `setItemIsSelected`
   - UI natychmiast odznacza pozostałe opcje (optimistic update)

2. **Pola podstawowe** (jak item):
   - Name, Quantity, Unit, UnitPriceNet, VatRate, UnitPriceGross
   - NetValue, GrossValue, VatValue

3. **Pola dodatkowe** — to samo co w item row

4. **Pliki** — to samo co w item row

### 3. Modyfikacja `SortableComponentRow.tsx`

**Renderowanie**:
1. **Checkbox IsSelected** (domyślnie true):
   - Jeśli odznaczony, komponent nie jest sumowany do pozycji nadrzędnej
   - Kliknięcie → `setItemIsSelected`

2. **Pola podstawowe**:
   - Name (jedyny edytowalny)
   - Quantity: readonly/null
   - Unit: readonly/null
   - UnitPriceNet: readonly/null
   - NetValue: input number (wpisywany przez usera)
   - GrossValue: readonly (obliczane)
   - VatValue: readonly

3. **Pola dodatkowe** — to samo

4. **Pliki** — to samo

### 4. Modyfikacja `SortableGroupRow.tsx`

**Renderowanie**:
1. **Nazwa** — edytowalny text input
2. **NetValue** — readonly (suma z pozycji)
3. **GrossValue** — readonly
4. **Pola dodatkowe** — wartości z `group.additionalFieldValues`
5. **Puste pola** (Quantity, Unit, itp.) — "—"

### 5. Modyfikacja `FileFieldRenderer.tsx`

Dostosuj do nowej struktury:
- Zamiast `fieldValueId` → `itemId`
- Zamiast `CostEstimateFieldFileWeb` → `CostEstimateItemFileWeb`
- Użyj nowych endpointów `uploadItemFiles` / `deleteItemFile`

### Build

```powershell
npm run build
```
Jeśli build failed, przerwij i zgłoś błędy.
