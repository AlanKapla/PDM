# UI-02: CostEstimateTableView — podział kolumn, collapsible field sections, usunięcie kolumny Pozycja

## Cel
Główna zmiana w komponencie `CostEstimateTableView`:
1. Podział `expandedColumns` na `groupColumns` i `itemColumns`
2. Dodanie collapsible field sections (przyciski rozwiń/zwiń dla pól etapów i pozycji)
3. Usunięcie kolumny "Pozycja" z nagłówka i wszystkich wierszy
4. Filtrowanie kolumn po `isVisible`
5. Dostosowanie `filterAndSortGroups`, `filterAndSortItems` do nowego podziału

## Plik do zmiany

### `src/components/CostEstimate/CostEstimateTableView.tsx`

#### Zmiana 1: expandedColumns — dodanie fieldScope

W `useMemo` dla `expandedColumns`, dodaj pole `fieldScope` do każdego `ExpandedColumn`:
```typescript
// W expandedColumns useMemo, przy pushowaniu kolumny:
result.push({
  type: 'regular',
  originalColumn: col,
  fieldDef,
  label,
  fieldId: col.fieldId,
  width: undefined,
  isSortable: fieldDef?.isSortable ?? false,
  isFilterable: fieldDef?.isFilterable ?? false,
  isBoolean: fieldCfg?.isBoolean ?? false,
  isNumeric: fieldCfg?.isNumeric ?? false,
  fieldScope: col.fieldScope,  // NOWE — z ColumnConfigurationWeb
});
```

To samo dla childField (z `col.fieldScope` rodzica).

#### Zmiana 2: groupColumns i itemColumns

Po `expandedColumns`, dodaj osobne listy:
```typescript
const groupColumns = useMemo(() => 
  expandedColumns.filter(col => col.fieldScope === FieldScope.Group),
  [expandedColumns]
);

const itemColumns = useMemo(() => 
  expandedColumns.filter(col => col.fieldScope !== FieldScope.Group),
  [expandedColumns]
);
```

#### Zmiana 3: Collapsible field sections

Dodaj stan:
```typescript
const [groupFieldsCollapsed, setGroupFieldsCollapsed] = useState(false);
const [itemFieldsCollapsed, setItemFieldsCollapsed] = useState(false);
```

Dodaj przyciski nad tabelą (obok istniejącego paska narzędziowego):
```typescript
// W return, nad tabelą, po toolbarku:
<HStack spacing={4} px={4} py={2} borderBottomWidth="1px" borderBottomColor="neutral.200">
  <Button
    size="sm"
    variant="ghost"
    leftIcon={groupFieldsCollapsed ? <ChevronRight size={14} /> : <ChevronDown size={14} />}
    onClick={() => setGroupFieldsCollapsed(prev => !prev)}
  >
    Pola etapów {groupFieldsCollapsed ? '(ukryte)' : ''}
  </Button>
  <Button
    size="sm"
    variant="ghost"
    leftIcon={itemFieldsCollapsed ? <ChevronRight size={14} /> : <ChevronDown size={14} />}
    onClick={() => setItemFieldsCollapsed(prev => !prev)}
  >
    Pola pozycji {itemFieldsCollapsed ? '(ukryte)' : ''}
  </Button>
</HStack>
```

#### Zmiana 4: Usunięcie kolumny "Pozycja" z nagłówka

W `renderTableHeader()`:
- Usuń `<Th>` z "Pozycja" (linie 2841-2855 w obecnym kodzie)
- Usuń `<col>` dla pozycji w `<colgroup>` (linia 3112)
- Usuń `POSITION_COL_MIN_WIDTH` z liczenia szerokości tabeli (`minWidth` w linii 3107)

#### Zmiana 5: Nagłówek tabeli — osobne sekcje dla group i item columns

Zmień `renderTableHeader()` aby renderował:
1. Najpierw groupColumns (jeśli nie są zwinięte)
2. Potem itemColumns (jeśli nie są zwinięte)

Możesz to zrobić jako jeden wiersz nagłówka z kolumnami w odpowiedniej kolejności:
- Actions (sticky, left=0)
- groupColumns (jeśli !groupFieldsCollapsed)
- itemColumns (jeśli !itemFieldsCollapsed)

```typescript
const renderTableHeader = () => {
  // Filtruj kolumny w zależności od stanu collapse
  const visibleColumns = [
    ...(!groupFieldsCollapsed ? groupColumns : []),
    ...(!itemFieldsCollapsed ? itemColumns : []),
  ];
  
  // Renderuj identycznie jak obecnie, ale z visibleColumns zamiast expandedColumns
  // i bez sticky kolumny "Pozycja"
};
```

#### Zmiana 6: filterAndSortGroups — użyj groupColumns

Zmień `isGroupCol` aby używała `groupColumns` zamiast `expandedColumns.find()` + `templateStructure.groupHeaderFields`:
```typescript
const filterAndSortGroups = useCallback((groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb[] => {
  let result = [...groups];
  
  // Użyj groupColumns zamiast expandedColumns z ręcznym wykrywaniem
  const groupColIds = new Set(groupColumns.map(col => col.fieldId));
  
  const activeFilters = Object.entries(filters).filter(([fieldId]) => groupColIds.has(fieldId));
  // ... reszta logiki podobna, ale z groupColIds
}, [filters, sortConfig, groupColumns, templateStructure]);
```

#### Zmiana 7: filterAndSortItems — użyj itemColumns

Analogicznie jak powyżej:
```typescript
const filterAndSortItems = useCallback((items: CostEstimateItemWeb[]): CostEstimateItemWeb[] => {
  let result = [...items];
  
  const itemColIds = new Set(itemColumns.map(col => col.fieldId));
  const activeFilters = Object.entries(filters).filter(([fieldId]) => itemColIds.has(fieldId));
  // ... reszta logiki
}, [filters, sortConfig, itemColumns, templateStructure]);
```

#### Zmiana 8: flatRows — przekaż odpowiednie kolumny

W `flatRows`, przy renderowaniu:
- `SortableGroupRow` → przekaż `columns={groupColumns}` (zamiast `expandedColumns`)
- `SortableItemRow` → przekaż `columns={itemColumns}` (zamiast `expandedColumns`)

#### Zmiana 9: Obliczanie szerokości tabeli

Usuń `POSITION_COL_MIN_WIDTH` z sumy:
```typescript
minWidth={`${(canStructuralEdit ? 120 : 0) + totalColumnWidth}px`}
```
Gdzie `totalColumnWidth` to suma szerokości groupColumns + itemColumns.

#### Zmiana 10: Stopka (podsumowanie)

W `tfoot`, użyj `groupColumns` i `itemColumns` (lub expandedColumns) do renderowania sum. Obecnie stopka używa `expandedColumns` — możesz zostawić expandedColumns dla stopki (bo potrzebuje wszystkich kolumn).

## Zależności
- Wymaga UI-01 (zmiany typów)
- Jest zależnością dla UI-03 i UI-04
