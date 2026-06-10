# UI-03: SortableGroupRow — uproszczenie, usunięcie kolumny Pozycja

## Cel
Dostosowanie `SortableGroupRow` do nowej architektury:
1. Otrzymuje tylko `columns` (group columns) zamiast wszystkich expandedColumns
2. Usunięcie sticky kolumny "Pozycja" (z badge ETAP)
3. Uproszczenie iteracji po kolumnach — nie trzeba już szukać groupHeaderFields

## Plik do zmiany

### `src/components/CostEstimate/rows/SortableGroupRow.tsx`

#### Zmiana 1: Props

**OBECNIE:**
```typescript
export interface SortableGroupRowProps {
  // ... reszta
  expandedColumns: ExpandedColumn[];
  // ... reszta
}
```

**PO ZMIANIE:**
```typescript
export interface SortableGroupRowProps {
  // ... reszta (bez zmian)
  columns: ExpandedColumn[];  // TYLKO group columns
  // ... reszta
}
```

Usuń `getColumnWidth`, `getGroupFieldValue`, `updateGroupFieldValue`, `renderFieldInput`, `formatDisplayValue` — jeśli są nadal potrzebne do renderowania pól, zostaw.

Usuń lub oznacz jako opcjonalny `templateStructure` — nie jest już potrzebny do wykrywania group fields, ale może być potrzebny do innych rzeczy (np. `canBranchGroups`, `maxGroupLevel`). Sprawdź użycie w komponencie.

#### Zmiana 2: Usunięcie sticky kolumny "Pozycja"

Usuń cały `<Td>` z badge ETAP (obecnie linie 217-256). Ten `<Td>` był sticky (position="sticky", left={editable ? '120px' : 0}) i zawierał przycisk collapse/expand + badge "ETAP {groupNumber}".

**ZACHOWAJ** przycisk collapse/expand — przenieś go do sekcji Akcje (obok drag handle i innych przycisków), lub dodaj na początku wiersza jako osobny element.

Jeśli badge ETAP miał funkcję identyfikacji grupy — zastąp go tekstem w pierwszej kolumnie danych (renderowanej z groupColumns, gdzie pierwsze pole to GroupName).

#### Zmiana 3: Uproszczenie renderowania kolumn

**OBECNIE** (linie 259-372):
```typescript
{expandedColumns.map((col: any) => {
  // 1. Sprawdź childField — pomiń
  // 2. Sprawdź groupHeaderField — wyświetl
  // 3. Sprawdź systemField/calcField/genericField — wyświetl sumę
  // 4. W przeciwnym razie — puste
})}
```

**PO ZMIANIE:**
```typescript
{columns.map((col: any) => {
  const colWidth = getColumnWidth(col.fieldId, col.width, col.label);
  
  // Wszystkie kolumny są grupowe — nie trzeba sprawdzać
  const groupHeaderField = col.fieldDef;  // lub znajdź w templateStructure.groupHeaderFields
  if (groupHeaderField) {
    const value = getGroupFieldValue(group, groupHeaderField.id);
    return (
      <Td key={col.fieldId} p={2} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`}>
        {canEditFields ? (
          renderFieldInput(groupHeaderField, value, (newValue) =>
            updateGroupFieldValue(group.id, groupHeaderField.id, newValue)
          )
        ) : (
          <Text fontSize="sm" fontWeight="medium">
            {formatDisplayValue(value, groupHeaderField)}
          </Text>
        )}
      </Td>
    );
  }
  
  // Dla pól które nie są groupHeaderFields (mogą być item fields w group row) — nie powinny wystąpić
  return (
    <Td key={col.fieldId} p={2} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`}>
      <Text fontSize="xs" color="neutral.400" fontStyle="italic" textAlign="center">—</Text>
    </Td>
  );
})}
```

#### Zmiana 4: Zachowanie sum grupowych

Logika sumowania (`shouldSum`, `summaryValues`, `sumInGroup`) pozostaje — ale teraz działa tylko na group columns. Upewnij się że pola sumowane (calculated fields z `sumInGroup`) są nadal dostępne w group columns — jeśli nie, to muszą pozostać w `templateStructure.calculatedFields`.

**Uwaga:** Pola kalkulowane z `sumInGroup` mogą mieć `fieldScope !== Group` — jeśli tak, muszą pozostać w expandedColumns/głównej liście, ale być renderowane tylko w group rows jako podsumowania.

#### Zmiana 5: BoxShadow i stylizacja

Usuń box-shadow związany z pozycją sticky (która została usunięta). Zachowaj oznaczenie poziomu (primary.100 dla level 0, primary.50 dla reszty).

## Zależności
- Wymaga UI-02 (CostEstimateTableView przekazuje `columns` zamiast `expandedColumns`)
