# ce-ui-fix-03 — Scroll poziomy + zamrożona kolumna Nazwa

## Cel
Umożliwić poziomy scroll tabeli gdy zawartość jest szersza niż viewport.
Zamrozić kolumnę Nazwa (sticky left) podczas scrollowania.
Zapewnić że nagłówek (header) pozostaje fixed podczas scrollowania pionowego (już sticky, ale wymaga dopasowania).

## Przeczytaj skill przed implementacją
`.github/skills/ui-components/SKILL.md`

---

## Problem obecny

1. Outer Box ma `overflow="hidden"` — blokuje poziomy scroll
2. Kolumna Nazwa (flex: 1, minW="270px") — nie jest sticky, znika przy scrollowaniu poziomym
3. Nagłówek jest sticky top, ale wewnętrzny kontener `Box overflowX="auto"` jest osobnym elementem scroll, co może powodować desynchronizację

---

## Plik: `src/components/CostEstimate/TreeView/CostEstimateTreeView.tsx`

### Outer wrapper — zamień `overflow="hidden"` na scroll

Zmień wrapper z:
```tsx
<Box
  bg="white"
  border="1px solid"
  borderColor="neutral.200"
  borderRadius="14px"
  overflow="hidden"
>
```
Na:
```tsx
<Box
  bg="white"
  border="1px solid"
  borderColor="neutral.200"
  borderRadius="14px"
  overflow="hidden"
  position="relative"
>
```

### Wewnętrzny kontener scroll

Kontener `<Box overflowX="auto">` który opakowuje DndContext i zawartość — zostaje, ale teraz outer Box nie blokuje overflow. Upewnij się że:
- Header (`<TreeViewHeader>`) jest POZA kontenerem `overflowX="auto"`, co umożliwi mu być sticky top niezależnie
- Zawartość (DndContext + wiersze) jest WEWNĄTRZ kontenera `overflowX="auto"`

Obecna struktura jest poprawna, tylko outer Box blokował scroll.

---

## Plik: `src/components/CostEstimate/TreeView/TreeViewRow.tsx` + `TreeViewHeader.tsx`

### Zamrożona kolumna Nazwa — sticky left

Idea: kolumna Nazwa (flex: 1, minW="270px") musi mieć `position: sticky; left: 0; z-index: 2` żeby pozostała widoczna przy scrollowaniu poziomym.

Każdy wiersz ma strukturę `<Flex px={3.5}>`:
- Kolumna Nazwa: `<Flex flex={1} minW="270px">` — dodaj sticky
- Pozostałe kolumny: normalne

#### W TreeViewHeader — nagłówek kolumny Nazwa

Zmień `<Box flex={1} minW="270px">` na:
```tsx
<Box
  flex="0 0 auto"
  w="270px"
  position="sticky"
  left={0}
  zIndex={3}
  bg="neutral.50"
  _after={{
    content: '""',
    position: 'absolute',
    right: 0,
    top: 0,
    bottom: 0,
    width: '8px',
    background: 'linear-gradient(to right, rgba(0,0,0,0.06), transparent)',
    pointerEvents: 'none',
  }}
>
```

#### W TreeViewRow — wiersz grupy

Zmień kolumnę Nazwa grupy:
```tsx
<Flex
  flex="0 0 auto"
  w="270px"
  position="sticky"
  left={0}
  zIndex={2}
  bg={bgGradient ?? 'white'}
  align="center"
  gap={2}
  _after={{
    content: '""',
    position: 'absolute',
    right: 0,
    top: 0,
    bottom: 0,
    width: '8px',
    background: 'linear-gradient(to right, rgba(0,0,0,0.05), transparent)',
    pointerEvents: 'none',
  }}
>
```

#### W ItemRow — wiersz pozycji

Zmień kolumnę Nazwa pozycji:
```tsx
<Flex
  flex="0 0 auto"
  w="270px"
  position="sticky"
  left={0}
  zIndex={2}
  bg={isOption && item.isSelected ? 'primary.25' : 'white'}
  align="center"
  gap={2}
  pl={`${indentSize}px`}
  _after={{
    content: '""',
    position: 'absolute',
    right: 0,
    top: 0,
    bottom: 0,
    width: '8px',
    background: 'linear-gradient(to right, rgba(0,0,0,0.04), transparent)',
    pointerEvents: 'none',
  }}
>
```

### Minimalny scroll

Dodaj do kontenera `<Box overflowX="auto">`:
```tsx
<Box overflowX="auto" overflowY="visible" minW="100%">
```

Cały wiersz powinien mieć `minWidth` równy sumie wszystkich widocznych kolumn:
```tsx
<Flex
  minW={`${totalColumnsWidth}px`}
  align="center"
  ...
>
```

Gdzie `totalColumnsWidth` oblicza się w CostEstimateTreeView i przekazuje do wierszy:
```typescript
const totalColumnsWidth = useMemo(() => {
  const nameCol = 270;
  const actionsCol = 120;
  const baseCols = baseColumns
    .filter((c) => c.id !== 'name')
    .reduce((sum, c) => sum + parseInt(c.width ?? '100px'), 0);
  const addCols = additionalColumns
    .reduce((sum, c) => sum + parseInt(c.width ?? '130px'), 0);
  return nameCol + baseCols + addCols + actionsCol + 28; // 28px padding
}, [baseColumns, additionalColumns]);
```

Przekaż `totalColumnsWidth` do `TreeViewRow` i `TreeViewHeader` jako prop.

---

## Plik: `src/components/CostEstimate/TreeView/TreeViewHeader.tsx`

Nagłówek musi być w tym samym scroll-container co wiersze, żeby sticky kolumna była zsynchronizowana. Sprawdź obecną strukturę — jeśli nagłówek jest poza `overflowX="auto"` kontenerem, przenieś go do środka lub użyj osobnego sticky mechanizmu.

Uproszczony układ w `CostEstimateTreeView`:
```tsx
<Box
  bg="white"
  border="1px solid"
  borderColor="neutral.200"
  borderRadius="14px"
  position="relative"
>
  {/* Search bar — poza scrollem, sticky top */}
  <Box position="sticky" top={0} zIndex={10} bg="white">
    <SearchRow ... />
  </Box>

  {/* Scrollowalny kontener */}
  <Box overflowX="auto" overflowY="visible">
    {/* Header — sticky top, wewnątrz scroll kontenera */}
    <Box position="sticky" top={searchBarHeight} zIndex={5} bg="neutral.50">
      <TreeViewHeader ... />
    </Box>
    
    {/* Zawartość */}
    <DndContext ...>
      ...
    </DndContext>
  </Box>

  {/* Footer */}
  ...
</Box>
```

Jeśli przenosisz SearchInput do osobnego baru poza scrollem, ustaw `top` nagłówka na wysokość search bara (np. `top="48px"` lub dynamicznie).

---

## Weryfikacja

1. Przy wielu kolumnach tabela scrolluje się poziomo
2. Kolumna Nazwa pozostaje widoczna przy scrollowaniu
3. Nagłówek pozostaje widoczny przy scrollowaniu pionowym
4. Cień (box shadow gradient) na prawej krawędzi kolumny Nazwa wskazuje scroll
5. Na mobile/wąskich ekranach tabela scrolluje się swobodnie
