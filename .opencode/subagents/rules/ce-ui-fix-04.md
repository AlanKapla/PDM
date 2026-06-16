# ce-ui-fix-04 — Resize kolumn + tooltip na nagłówkach + min-width

## Cel
Umożliwić zmianę szerokości kolumn przez przeciągnięcie krawędzi nagłówka.
Szerokość nigdy nie może być mniejsza niż długość tekstu nazwy kolumny.
Tooltip na nazwie nagłówka z opisem kolumny.

## Przeczytaj skill przed implementacją
`.github/skills/ui-components/SKILL.md`

---

## 1. Opisy kolumn (tooltip)

Dodaj pole `description` do `ColumnDef`:
```typescript
export interface ColumnDef {
  id: string;
  label: string;
  description?: string;  // ← NOWE
  fieldType: 'string' | 'numeric' | 'boolean' | 'datetime';
  appliesTo: Array<'group' | 'item'>;
  width?: string;
  isAdditional?: boolean;
  isSortable?: boolean;
  textAlign?: 'left' | 'right' | 'center';
}
```

Uzupełnij `BASE_COLUMNS` w `CostEstimateTreeView.tsx` o `description`:
```typescript
{ id: 'quantity',       description: 'Ilość jednostek' },
{ id: 'unit',           description: 'Jednostka miary (szt, m², godz...)' },
{ id: 'unitPriceNet',   description: 'Cena jednostkowa netto' },
{ id: 'vatRate',        description: 'Stawka VAT (%)' },
{ id: 'unitPriceGross', description: 'Cena jednostkowa brutto = netto × (1 + VAT)' },
{ id: 'netValue',       description: 'Wartość netto = ilość × cena netto' },
{ id: 'grossValue',     description: 'Wartość brutto = wartość netto + VAT' },
{ id: 'vatValue',       description: 'Wartość VAT = wartość netto × stawka VAT' },
{ id: 'isSelected',     description: 'Sumuj — czy wliczać do sum etapu i kosztorysu' },
{ id: 'isStageWork',    description: 'Zakres pracy harmonogramu — powiąż pozycję z harmonogramem' },
{ id: 'files',          description: 'Załączone pliki' },
```

---

## 2. Stan szerokości kolumn

W `CostEstimateTreeView.tsx` dodaj stan:
```typescript
const [colWidths, setColWidths] = useState<Record<string, number>>(() => {
  // Inicjalizuj z domyślnych szerokości lub z localStorage
  const initial: Record<string, number> = {};
  for (const col of BASE_COLUMNS) {
    initial[col.id] = parseInt(col.width ?? '100px');
  }
  return initial;
});
```

Funkcja aktualizacji:
```typescript
const handleResizeColumn = useCallback((colId: string, newWidth: number) => {
  setColWidths((prev) => ({ ...prev, [colId]: Math.max(newWidth, MIN_WIDTHS[colId] ?? 60) }));
}, []);
```

`MIN_WIDTHS` to mapa minimalnych szerokości wg długości tekstu nagłówka:
```typescript
const MIN_WIDTHS: Record<string, number> = {
  quantity: 50,
  unit: 50,
  unitPriceNet: 110,
  vatRate: 50,
  unitPriceGross: 120,
  netValue: 100,
  grossValue: 110,
  vatValue: 90,
  isSelected: 55,
  isStageWork: 65,
  files: 45,
};
```

Przekaż `colWidths` i `onResizeColumn` do `TreeViewHeader` i `TreeViewRow`.

---

## 3. ResizeHandle w nagłówku kolumny

W `TreeViewHeader.tsx`, w `ColumnHeaderCell` dodaj uchwyt do resize:

```tsx
interface ColumnHeaderCellProps {
  col: ColumnDef;
  sortConfig: SortConfig | null;
  onSort: (field: string) => void;
  width: number;           // ← pixele zamiast string
  onResize: (colId: string, width: number) => void;
}

const ColumnHeaderCell: React.FC<ColumnHeaderCellProps> = ({
  col, sortConfig, onSort, width, onResize
}) => {
  const isDragging = useRef(false);
  const startX = useRef(0);
  const startWidth = useRef(0);

  const handleMouseDown = (e: React.MouseEvent) => {
    e.preventDefault();
    isDragging.current = true;
    startX.current = e.clientX;
    startWidth.current = width;

    const onMouseMove = (e: MouseEvent) => {
      if (!isDragging.current) return;
      const delta = e.clientX - startX.current;
      const newWidth = Math.max(startWidth.current + delta, MIN_WIDTHS[col.id] ?? 60);
      onResize(col.id, newWidth);
    };

    const onMouseUp = () => {
      isDragging.current = false;
      document.removeEventListener('mousemove', onMouseMove);
      document.removeEventListener('mouseup', onMouseUp);
    };

    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onMouseUp);
  };

  return (
    <Box position="relative" flex="0 0 auto" w={`${width}px`} userSelect="none">
      {/* Tooltip z opisem kolumny */}
      <Tooltip
        label={col.description ?? col.label}
        placement="top"
        hasArrow
        openDelay={500}
        fontSize="12px"
      >
        {/* Istniejący content nagłówka (label + sort icon) */}
        <Flex
          as={col.isSortable ? 'button' : 'div'}
          align="center"
          gap="4px"
          px={2}
          h="42px"
          cursor={col.isSortable ? 'pointer' : 'default'}
          onClick={col.isSortable ? () => onSort(col.id) : undefined}
          _hover={col.isSortable ? { color: 'primary.600' } : undefined}
          color={sortConfig?.field === col.id ? 'primary.600' : 'neutral.500'}
          justify={col.textAlign === 'right' ? 'flex-end' : col.textAlign === 'center' ? 'center' : 'flex-start'}
          w="full"
          role={col.isSortable ? 'columnheader' : undefined}
          overflow="hidden"
        >
          {col.textAlign === 'right' && <SortIcon field={col.id} sortConfig={sortConfig} />}
          <Text
            fontSize="11.5px"
            fontWeight="700"
            textTransform="uppercase"
            letterSpacing="0.045em"
            noOfLines={2}           // ← zmień z noOfLines={1} na 2, żeby wieloliniowe etykiety były widoczne
            userSelect="none"
            lineHeight="1.2"
            whiteSpace="pre-wrap"   // ← pozwala na \n w etykiecie (np. "Zakres\nharmon.")
          >
            {col.label}
          </Text>
          {col.textAlign !== 'right' && <SortIcon field={col.id} sortConfig={sortConfig} />}
        </Flex>
      </Tooltip>

      {/* Uchwyt resize */}
      <Box
        position="absolute"
        right={0}
        top={0}
        bottom={0}
        w="4px"
        cursor="col-resize"
        onMouseDown={handleMouseDown}
        _hover={{ bg: 'primary.300' }}
        zIndex={1}
        aria-hidden="true"
      />
    </Box>
  );
};
```

---

## 4. Przekazanie szerokości do TreeViewRow

W `CostEstimateTreeView`, buduj `baseColumns` z aktualną szerokością:
```typescript
const baseColumnsWithWidths = useMemo(
  () => BASE_COLUMNS.map((col) => ({
    ...col,
    width: `${colWidths[col.id] ?? parseInt(col.width ?? '100px')}px`,
  })),
  [colWidths]
);
```

Przekaż `baseColumnsWithWidths` zamiast `BASE_COLUMNS` do `TreeViewRow` i `TreeViewHeader`.

---

## Uwagi

- Kolumna Nazwa NIE ma resize handlera (flex: 1, adaptatywna)
- Min-width dla każdej kolumny musi wynikać z szerokości tekstu etykiety — `MIN_WIDTHS` zapewnia to ręcznie
- Nie zapisujemy szerokości kolumn do sessionStorage (tylko widoczność kolumn — to zrobiło ce-ui-fix-02)
- `noOfLines={1}` w nagłówkach zamień na `noOfLines={2}` żeby tekst dwulinijkowy (np. "Zakres\nharmon.") był widoczny

---

## Weryfikacja

1. Najeżdżając na prawą krawędź nagłówka kursor zmienia się na col-resize
2. Przeciągnięcie krawędzi zmienia szerokość kolumny
3. Kolumna nie może być węższa niż MIN_WIDTH
4. Tooltip pojawia się po najechaniu na nazwę nagłówka z opisem kolumny
5. Tekst etykiet wieloliniowych (Zakres harmonogramu) wyświetla się poprawnie
