# UI-04: SortableItemRow, SortableOptionRow, SortableComponentRow — uproszczenie, usunięcie kolumny Pozycja

## Cel
Dostosowanie wierszy pozycji do nowej architektury:
1. Otrzymują tylko `columns` (item columns) zamiast wszystkich expandedColumns
2. Usunięcie sticky kolumny "Pozycja" (z badge POZYCJA)
3. Uproszczenie iteracji po kolumnach — nie trzeba pomijać groupHeaderFields

## Pliki do zmiany

### 1. `src/components/CostEstimate/rows/SortableItemRow.tsx`

#### Zmiana 1: Props

**OBECNIE:**
```typescript
export interface SortableItemRowProps {
  // ...
  expandedColumns: ExpandedColumn[];
  // ...
}
```

**PO ZMIANIE:**
```typescript
export interface SortableItemRowProps {
  // ...
  columns: ExpandedColumn[];  // TYLKO item columns
  // ...
}
```

#### Zmiana 2: Usunięcie sticky kolumny "Pozycja"

Usuń cały `<Td>` z POZYCJA (obecnie linie 251-296). Ten `<Td>` był sticky (position="sticky", left={editable ? '120px' : 0}) i zawierał przycisk collapse/expand dla dzieci + "POZYCJA {itemNumber}".

**ZACHOWAJ** przycisk collapse/expand dla opcji/komponentów — przenieś go do sekcji Akcje, lub dodaj na początku wiersza jako osobny element (np. w pierwszej kolumnie danych).

**ZACHOWAJ** `itemNumber` — ale nie wyświetlaj go w sticky kolumnie. Jeśli chcesz pokazać numer pozycji, możesz dodać go do pierwszej kolumny itemColumns (zakładając że ItemSystemName jest zawsze pierwsze).

#### Zmiana 3: Uproszczenie renderowania kolumn

**OBECNIE** (linie 299-401):
```typescript
{expandedColumns.map((col: any) => {
  // 1. Sprawdź groupHeaderField — pomiń (wyświetl "—")
  // 2. Sprawdź childField — renderuj liczbę opcji
  // 3. Znajdź fieldDef w systemFields/calculatedFields/genericFields — renderuj input
  // 4. W przeciwnym razie — "—"
})}
```

**PO ZMIANIE:**
```typescript
{columns.map((col: any) => {
  const colWidth = getColumnWidth(col.fieldId, col.width, col.label);
  
  // Nie trzeba sprawdzać groupHeaderField — nie ma ich w itemColumns
  
  if (col.type === 'childField') {
    // Renderuj wartość opcji (logika jak obecnie)
    return (
      <Td key={col.fieldId} p={2} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`}>
        {/* istniejąca logika childField */}
      </Td>
    );
  }
  
  // Wszystkie regular columns to item fields
  let fieldDef: any = col.fieldDef;
  let fieldSource: FieldSource = 'generic';
  
  if (!fieldDef) {
    // Fallback — znajdź w templateStructure (tylko item fields, nie group)
    fieldDef = templateStructure.systemFields?.find(
      (f: any) => f.fieldName === col.originalColumn.fieldName
    ) ?? templateStructure.calculatedFields?.find(
      (f: any) => f.fieldName === col.originalColumn.fieldName
    ) ?? templateStructure.genericFields?.find(
      (f: any) => f.fieldName === col.originalColumn.fieldName
    );
    // Określ fieldSource...
  } else {
    fieldSource = getFieldSource(fieldDef.id, templateStructure);
  }
  
  if (fieldDef) {
    // Renderuj input lub podgląd (logika jak obecnie, bez sprawdzania groupHeaderField)
    // ...
  }
  
  return (
    <Td key={col.fieldId} p={2} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`}>
      —
    </Td>
  );
})}
```

#### Zmiana 4: Przekazanie props do SortableOptionRow i SortableComponentRow

Zmień `expandedColumns={expandedColumns}` na `columns={columns}` (przekazując item columns).

### 2. `src/components/CostEstimate/rows/SortableOptionRow.tsx`

#### Zmiana 1: Props
```typescript
// OBECNIE:
expandedColumns: ExpandedColumn[];
// PO ZMIANIE:
columns: ExpandedColumn[];
```

#### Zmiana 2: Renderowanie
Zmień iterację `expandedColumns` → `columns`. Usuń logikę pomijania groupHeaderFields (jeśli istnieje).

### 3. `src/components/CostEstimate/rows/SortableComponentRow.tsx`

#### Zmiana 1: Props
```typescript
// OBECNIE:
expandedColumns: ExpandedColumn[];
// PO ZMIANIE:
columns: ExpandedColumn[];
```

#### Zmiana 2: Renderowanie
Zmień iterację `expandedColumns` → `columns`. Usuń logikę pomijania groupHeaderFields (jeśli istnieje).

## Zależności
- Wymaga UI-02 (CostEstimateTableView przekazuje `columns` zamiast `expandedColumns`)
