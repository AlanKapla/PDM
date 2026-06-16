# UI Fix 03: Nowe hooki

## Kontekst
Feature: costestimate-full-refactor — patrz `.opencode/features/costestimate-full-refactor.md`

Dostosowanie hooków do nowej struktury: autosave dla base fields + additional fields, IsSelected management.

## Do zrobienia

### 1. Modyfikacja `src/hooks/useFieldAutosave.ts`

Dostosuj do nowej struktury — teraz są dwa typów zapisów:
1. **Base field**: zapis przez `updateItemBaseFields` lub `updateGroupBaseFields`
2. **Additional field**: zapis przez `upsertItemAdditionalField` lub `upsertGroupAdditionalField`

```typescript
interface AutosaveParams {
  entityType: 'group' | 'item';
  entityId: string;
  fieldType: 'base' | 'additional'; // NOWE
  additionalFieldId?: string;       // Dla pól dodatkowych
  fieldValueId?: string | null;      // Dla pól dodatkowych (existing value)
  name: string;                     // Pole do zmiany: 'name', 'quantity', 'unit', itp.
  value: string | number | boolean | null;
  valueType: 'string' | 'numeric' | 'boolean' | 'date';
}
```

Logika autosave:
- Jeśli `fieldType === 'base'` → woła `updateItemBaseFields` lub `updateGroupBaseFields`
- Jeśli `fieldType === 'additional'` → woła `upsertItemAdditionalField` lub `upsertGroupAdditionalField`
- Debounce 700ms (bez zmian)
- Optimistic update: UI pokazuje zmianę natychmiast, API potwierdza

### 2. Nowy hook: `useItemSelection`

```typescript
// src/hooks/useItemSelection.ts

interface UseItemSelectionParams {
  tenantId: string;
  projectId: string;
  costEstimateId: string;
  onSuccess?: () => void;
  onError?: (error: Error) => void;
}

interface UseItemSelectionReturn {
  setSelected: (itemId: string, isSelected: boolean) => Promise<void>;
  isPending: boolean;
}

export function useItemSelection(params: UseItemSelectionParams): UseItemSelectionReturn {
  // Używa mutation z TanStack React Query
  // Optimistic update: natychmiast zmienia stan w cache
  // Na wypadek błędu: rollback do poprzedniej wartości
  
  // Po sukcesie: invalidate query dla details → trigger refetch
}
```

### 3. Modyfikacja `src/hooks/queries/useCostEstimate.ts`

Dostosuj do nowego web modelu — `details` zawiera teraz `additionalFields` zamiast `schema`.

```typescript
// Zmień typ zwracany z useCostEstimateDetails
// Stary: details.schema.fieldDefinitions
// Nowy: details.additionalFields

// Dodaj funkcje dla additional fields
export function useAdditionalFields(tenantId: string, projectId: string, costEstimateId: string) {
  return useQuery({
    queryKey: ['cost-estimate', costEstimateId, 'additional-fields'],
    queryFn: () => getAdditionalFields(tenantId, projectId, costEstimateId),
  });
}

// Dodaj mutation dla additional fields
export function useAddAdditionalField() {
  return useMutation({
    mutationFn: (params: { tenantId: string; projectId: string; costEstimateId: string; data: { name: string; fieldType: AdditionalFieldType; order?: number } }) =>
      addAdditionalField(params.tenantId, params.projectId, params.costEstimateId, params.data),
  });
}
```

### 4. Nowy hook: `useAdditionalFieldValues`

```typescript
// src/hooks/useAdditionalFieldValues.ts

interface UseAdditionalFieldValuesParams {
  tenantId: string;
  projectId: string;
  costEstimateId: string;
  entityType: 'group' | 'item';
  entityId: string;
}

export function useAdditionalFieldValues(params: UseAdditionalFieldValuesParams) {
  // Zarządza wartościami pól dodatkowych dla konkretnego entity
  // Zapisuje przez upsertItemAdditionalField lub upsertGroupAdditionalField
  // Optimistic update + debounce
}
```

### 5. Helper dla AdditionalFieldValue

```typescript
// src/utils/additionalFieldHelpers.ts

export function getAdditionalFieldValue(
  fieldValues: CostEstimateAdditionalFieldValueWeb[],
  additionalFieldId: string
): CostEstimateAdditionalFieldValueWeb | undefined {
  return fieldValues.find(fv => fv.additionalFieldId === additionalFieldId);
}

export function getAdditionalFieldValueAsString(
  fieldValues: CostEstimateAdditionalFieldValueWeb[],
  additionalFieldId: string
): string | undefined {
  const fv = getAdditionalFieldValue(fieldValues, additionalFieldId);
  if (!fv) return undefined;
  return fv.stringValue ?? fv.decimalValue?.toString() ?? fv.boolValue?.toString() ?? fv.dateTimeValue;
}

export function getAdditionalFieldDefinition(
  fields: CostEstimateAdditionalFieldWeb[],
  fieldId: string
): CostEstimateAdditionalFieldWeb | undefined {
  return fields.find(f => f.id === fieldId);
}
```

### Uwaga
Nie usuwaj starego `useFieldAutosave` — dostosuj go. Stare callbacki (onFieldChange, onFieldAutosave) mogą być nadal używane w komponentach, ale dostosuj ich typy do nowej struktury.

### Build

```powershell
npm run build
```
Jeśli build failed, przerwij i zgłoś błędy.
