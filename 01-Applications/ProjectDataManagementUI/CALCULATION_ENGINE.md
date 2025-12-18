# Calculation Engine - Dokumentacja

## Przegląd

System automatycznych obliczeń dla pól kalkulowanych w kosztorysach. Obsługuje:
- Automatyczne przeliczanie pól zależnych (UnitPriceGross, ValueNet, ValueGross)
- Obliczenia w zakresach prac (work scopes)
- Obliczenia w polach kolekcji
- Sumowanie grup i całego kosztorysu
- Zapis przeliczonych wartości przez API

## Struktura plików

```
src/
├── utils/
│   └── calculationEngine.ts      # Silnik obliczeń
├── hooks/
│   └── useCalculations.ts        # Hook do zarządzania obliczeniami
├── components/
│   └── WorkScopeEditor.tsx       # Przykład komponentu z auto-obliczeniami
└── api/
    └── costEstimateApi.ts        # API z metodą saveCostEstimateData
```

## Wzory obliczeń

### Pola kalkulowane (CalculatedFieldType)

| Pole | Wartość | Wzór |
|------|---------|------|
| UnitPriceNet | 0 | Pole wejściowe (nie obliczane) |
| VatRate | 1 | Pole wejściowe (nie obliczane) |
| UnitPriceGross | 2 | `UnitPriceNet × (1 + VatRate/100)` |
| Quantity | 3 | Pole wejściowe (nie obliczane) |
| ValueNet | 4 | `UnitPriceNet × Quantity` |
| ValueGross | 5 | `UnitPriceGross × Quantity` LUB `ValueNet × (1 + VatRate/100)` |

### Sumowanie

- **Sumy grup**: Sumowanie wartości z zakresów prac i podgrup
- **Sumy całkowite**: Sumowanie wartości ze wszystkich grup najwyższego poziomu

## Użycie

### 1. Podstawowe obliczenia w komponencie

```tsx
import { useCalculations } from '../hooks/useCalculations';
import { CostEstimateWorkScope } from '../types/costEstimate.types';

function MyEditor() {
  const [workScope, setWorkScope] = useState<CostEstimateWorkScope>({
    id: '1',
    order: 0,
    calculatedFieldValues: {
      UnitPriceNet: 100,
      VatRate: 23,
      Quantity: 10,
    },
    genericFieldValues: {},
  });

  const { recalculateWorkScope } = useCalculations({
    calculatedFields: templateCalculatedFields,
    genericFields: templateGenericFields,
    summaryConfig: templateSummaryConfig,
  });

  const handleFieldChange = (fieldName: string, value: number) => {
    const updated = {
      ...workScope,
      calculatedFieldValues: {
        ...workScope.calculatedFieldValues,
        [fieldName]: value,
      },
    };

    // Automatycznie przeliczy UnitPriceGross, ValueNet, ValueGross
    const recalculated = recalculateWorkScope(updated);
    setWorkScope(recalculated);
  };

  return (
    <div>
      {/* Po zmianie UnitPriceNet, VatRate lub Quantity
          automatycznie przeliczą się pola zależne */}
    </div>
  );
}
```

### 2. Przeliczenie całego kosztorysu

```tsx
import { useCalculations } from '../hooks/useCalculations';
import { costEstimateApi } from '../api/costEstimateApi';

function CostEstimateEditor() {
  const [dataModel, setDataModel] = useState<CostEstimateDataModel>({
    groups: [...],
    metadata: {...},
  });

  const { recalculateAll } = useCalculations({
    calculatedFields: template.workScopeFieldsDefinition.calculatedFields,
    genericFields: template.workScopeFieldsDefinition.genericFields,
    summaryConfig: template.summaryConfiguration,
  });

  const handleSave = async () => {
    // Przeliczy wszystkie pola w całym kosztorysie
    const recalculated = recalculateAll(dataModel);
    
    // Zapisz przez API
    await costEstimateApi.saveCostEstimateData(
      tenantId,
      projectId,
      estimateId,
      recalculated
    );

    setDataModel(recalculated);
  };

  return (
    <div>
      {/* Edytor kosztorysu */}
      <button onClick={handleSave}>Zapisz</button>
    </div>
  );
}
```

### 3. Obliczenia w polach kolekcji

```tsx
import { useCalculations } from '../hooks/useCalculations';

function CollectionItemEditor({ item, collectionFieldName }) {
  const { recalculateCollectionItem } = useCalculations({
    calculatedFields: [...],
    genericFields: [...], // Zawiera definicję pola Collection z nestedFields
  });

  const handleItemFieldChange = (fieldName: string, value: number) => {
    const updated = {
      ...item,
      calculatedFieldValues: {
        ...item.calculatedFieldValues,
        [fieldName]: value,
      },
    };

    // Przeliczy pola w elemencie kolekcji
    const recalculated = recalculateCollectionItem(updated, collectionFieldName);
    onItemChange(recalculated);
  };

  return <div>{/* Edytor elementu kolekcji */}</div>;
}
```

### 4. Sprawdzanie czy pole jest auto-obliczane

```tsx
const { isAutoCalculated, isSummable } = useCalculations({
  calculatedFields: [...],
  genericFields: [...],
});

// Zablokuj edycję pól auto-obliczanych
const isReadOnly = isAutoCalculated('UnitPriceGross'); // true

// Sprawdź czy pole uczestniczy w sumowaniu
const shouldSum = isSummable('ValueNet'); // true
```

### 5. Formatowanie wartości

```tsx
import { formatCalculatedValue } from '../utils/calculationEngine';

const valueNet = 12500.50;
const formatted = formatCalculatedValue(valueNet, '0.00', 'PLN');
// Wynik: "12500.50 PLN"

const quantity = 10;
const formattedQty = formatCalculatedValue(quantity, '0', 'szt.');
// Wynik: "10 szt."
```

## Komponenty gotowe do użycia

### WorkScopeEditor

Gotowy komponent do edycji zakresu prac z automatycznymi obliczeniami:

```tsx
import { WorkScopeEditor } from '../components/WorkScopeEditor';

<WorkScopeEditor
  workScope={workScope}
  calculatedFields={template.workScopeFieldsDefinition.calculatedFields}
  genericFields={template.workScopeFieldsDefinition.genericFields}
  onChange={(updated) => {
    // updated zawiera już przeliczone wartości
    setWorkScope(updated);
  }}
  readOnly={false}
/>
```

Komponent automatycznie:
- Wyświetla pola kalkulowane i generyczne
- Oznacza pola auto-obliczane badge "Auto"
- Blokuje edycję pól obliczanych automatycznie
- Pokazuje przeliczone wartości w sekcji podsumowania
- Wywołuje `onChange` po każdej zmianie z przeliczonymi wartościami

## API

### saveCostEstimateData

Specjalny endpoint do zapisywania przeliczonych danych:

```tsx
await costEstimateApi.saveCostEstimateData(
  tenantId,     // ID najemcy
  projectId,    // ID projektu
  estimateId,   // ID kosztorysu
  dataModel     // Przeliczony model danych
);
```

Backend automatycznie:
- Zapisuje strukturę danych
- Aktualizuje `totalNet` i `totalGross` w głównym rekordzie
- Ustawia `lastCalculatedAt` na aktualną datę
- Aktualizuje `metadata.lastModified`

## Przepływ danych

```
1. Użytkownik zmienia wartość (np. UnitPriceNet = 100)
   ↓
2. handleFieldChange aktualizuje stan lokalny
   ↓
3. recalculateWorkScope() oblicza pola zależne:
   - UnitPriceGross = 100 × 1.23 = 123
   - ValueNet = 100 × 10 = 1000
   - ValueGross = 123 × 10 = 1230
   ↓
4. onChange(recalculated) aktualizuje stan w komponencie rodzica
   ↓
5. (Opcjonalnie) recalculateAll() przelicza cały kosztorys:
   - Wszystkie work scopes
   - Wszystkie elementy kolekcji
   - Sumy grup
   - Sumy całkowite
   ↓
6. saveCostEstimateData() zapisuje do backendu
   ↓
7. Backend aktualizuje totalNet/totalGross i zwraca sukces
```

## Testowanie

### Test jednostkowy silnika obliczeń

```tsx
import { calculateFieldValue, CalculatedFieldType } from '../utils/calculationEngine';

describe('Calculation Engine', () => {
  it('oblicza UnitPriceGross poprawnie', () => {
    const values = {
      UnitPriceNet: 100,
      VatRate: 23,
    };
    
    const result = calculateFieldValue(
      CalculatedFieldType.UnitPriceGross,
      values
    );
    
    expect(result).toBe(123);
  });

  it('oblicza ValueNet poprawnie', () => {
    const values = {
      UnitPriceNet: 100,
      Quantity: 10,
    };
    
    const result = calculateFieldValue(
      CalculatedFieldType.ValueNet,
      values
    );
    
    expect(result).toBe(1000);
  });
});
```

## Wydajność

- **Lazy calculation**: Obliczenia wykonywane tylko gdy zmienią się wartości wejściowe
- **Memoization**: Hook `useCalculations` używa `useMemo` i `useCallback`
- **Batch updates**: React automatycznie grupuje aktualizacje stanu
- **Selective recalculation**: Można przeliczyć pojedynczy work scope zamiast całego kosztorysu

## Rozszerzanie

### Dodanie nowego typu pola kalkulowanego

1. Dodaj nowy typ do enum `CalculatedFieldType`:
```tsx
export enum CalculatedFieldType {
  // ... existing types
  NewField = 6,
}
```

2. Dodaj wzór w `calculateFieldValue()`:
```tsx
case CalculatedFieldType.NewField: {
  const value1 = getNum('Field1');
  const value2 = getNum('Field2');
  return value1 + value2; // Twój wzór
}
```

3. (Opcjonalnie) Dodaj specjalne formatowanie w `formatCalculatedValue()`

### Dodanie zaawansowanych reguł walidacji

```tsx
export function validateFieldValue(
  fieldType: CalculatedFieldType,
  value: number,
  rules: ValidationRule[]
): ValidationResult {
  // Implementacja walidacji
}
```

## Uwagi

- Pola `UnitPriceNet`, `VatRate`, `Quantity` są polami wejściowymi (nie obliczane)
- Pola `UnitPriceGross`, `ValueNet`, `ValueGross` są auto-obliczane
- Jeśli pole ma `autoCalculated: true`, jest tylko do odczytu w UI
- Sumowanie działa tylko dla pól z `summable: true`
- Obliczenia w kolekcjach działają niezależnie od głównego work scope
- `recalculateAll()` przetwarza rekurencyjnie całe drzewo grup

## Debugowanie

Włącz logi w development mode:

```tsx
// W calculationEngine.ts
const DEBUG = process.env.NODE_ENV === 'development';

export function calculateFieldValue(...) {
  if (DEBUG) {
    console.log('Calculating', fieldType, 'with values:', values);
  }
  // ... calculation logic
}
```
