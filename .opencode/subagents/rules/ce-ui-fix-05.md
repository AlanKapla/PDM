# ce-ui-fix-05 — Podsumowanie całkowite + sortowanie grup + akcje per poziom

## Cel
1. Dodać wiersz podsumowania całkowitego kosztorysu (suma wartości netto i brutto)
2. Naprawić sortowanie grup (po totalNet/totalGross)
3. Upewnić się że wszystkie poziomy hierarchii mają poprawne przyciski akcji

## Przeczytaj skill przed implementacją
`.github/skills/ui-components/SKILL.md`

---

## 1. Wiersz podsumowania (pkt 4)

### W `CostEstimateTreeView.tsx`

Oblicz sumy z `details` (dane z API zawierają `totalNet`, `totalGross`, `totalVat` na poziomie kosztorysu):

```typescript
// Użyj details.totalNet / details.totalGross / details.totalVat jeśli dostępne
// lub oblicz jako sumę z rootGroups
const totals = useMemo(() => {
  if (details.totalNet !== undefined && details.totalGross !== undefined) {
    return {
      net: details.totalNet,
      gross: details.totalGross,
      vat: details.totalVat ?? 0,
    };
  }
  // Fallback: sumuj z rootGroups
  return details.rootGroups.reduce(
    (acc, g) => ({
      net: acc.net + (g.totalNet ?? 0),
      gross: acc.gross + (g.totalGross ?? 0),
      vat: acc.vat + (g.totalVat ?? 0),
    }),
    { net: 0, gross: 0, vat: 0 }
  );
}, [details]);
```

Sprawdź typ `CostEstimateDetailsWeb` (plik `src/types/costEstimate.types.new.ts`) — upewnij się że ma pola `totalNet`, `totalGross`, `totalVat`. Jeśli nie — dodaj je do typu.

### Wiersz footer z sumami

Dodaj po `DndContext` (przed Footer z "Dodaj etap"), nowy wiersz:

```tsx
{/* Summary row */}
<Flex
  align="center"
  minH="52px"
  borderTop="2px solid"
  borderColor="neutral.200"
  bg="neutral.50"
  px={3.5}
  py={2}
  role="row"
>
  {/* Spacer for sticky Name column */}
  <Box
    flex="0 0 auto"
    w="270px"
    position="sticky"
    left={0}
    zIndex={2}
    bg="neutral.50"
  >
    <Text fontSize="13px" fontWeight="800" color="neutral.700" px={2}>
      RAZEM
    </Text>
  </Box>

  {/* Empty cells for item-only columns */}
  {baseColumns.filter((c) => ['quantity','unit','unitPriceNet','vatRate','unitPriceGross','isSelected','isStageWork','files'].includes(c.id)).map((col) => (
    <Box key={col.id} flex="0 0 auto" w={col.width ?? '100px'} />
  ))}

  {/* NetValue total */}
  {baseColumns.find((c) => c.id === 'netValue') && (
    <Flex flex="0 0 auto" w={baseColumns.find((c) => c.id === 'netValue')!.width ?? '130px'} justify="flex-end" pr={2}>
      <Text fontFamily="mono" fontSize="15px" fontWeight="800" sx={{ fontVariantNumeric: 'tabular-nums' }} color="neutral.800">
        {totals.net.toFixed(2)}
      </Text>
    </Flex>
  )}

  {/* GrossValue total */}
  {baseColumns.find((c) => c.id === 'grossValue') && (
    <Flex flex="0 0 auto" w={baseColumns.find((c) => c.id === 'grossValue')!.width ?? '130px'} justify="flex-end" pr={2}>
      <Text fontFamily="mono" fontSize="15px" fontWeight="800" sx={{ fontVariantNumeric: 'tabular-nums' }} color="neutral.800">
        {totals.gross.toFixed(2)}
      </Text>
    </Flex>
  )}

  {/* VatValue total */}
  {baseColumns.find((c) => c.id === 'vatValue') && (
    <Flex flex="0 0 auto" w={baseColumns.find((c) => c.id === 'vatValue')!.width ?? '110px'} justify="flex-end" pr={2}>
      <Text fontFamily="mono" fontSize="13px" fontWeight="600" sx={{ fontVariantNumeric: 'tabular-nums' }} color="neutral.500">
        {totals.vat.toFixed(2)}
      </Text>
    </Flex>
  )}

  {/* Empty cells for additional columns */}
  {additionalColumns.map((col) => (
    <Box key={col.id} flex="0 0 auto" w={col.width ?? '130px'} />
  ))}

  {/* Actions spacer */}
  <Box w="120px" />
</Flex>
```

---

## 2. Sortowanie grup (pkt 14 — brakujący przypadek)

### Problem w `CostEstimateTreeView.tsx`

Obecny `filteredGroups` nie ma sortowania. Sortowanie jest tylko wewnątrz wierszy (dla pozycji w grupie). Dodaj sortowanie grup wg `sortConfig`:

```typescript
const filteredAndSortedGroups = useMemo(() => {
  let groups = filteredGroups;

  if (sortConfig) {
    const { field, direction } = sortConfig;
    const sign = direction === 'asc' ? 1 : -1;

    groups = [...groups].sort((a, b) => {
      let aVal: number | string = '';
      let bVal: number | string = '';

      switch (field) {
        case 'name':
          aVal = a.name ?? '';
          bVal = b.name ?? '';
          break;
        case 'netValue':
          aVal = a.totalNet ?? 0;
          bVal = b.totalNet ?? 0;
          break;
        case 'grossValue':
          aVal = a.totalGross ?? 0;
          bVal = b.totalGross ?? 0;
          break;
        case 'vatValue':
          aVal = a.totalVat ?? 0;
          bVal = b.totalVat ?? 0;
          break;
        default: {
          // Pola dodatkowe
          const aFv = (a.additionalFieldValues ?? []).find((v) => v.additionalFieldId === field);
          const bFv = (b.additionalFieldValues ?? []).find((v) => v.additionalFieldId === field);
          aVal = aFv?.stringValue ?? aFv?.decimalValue ?? '';
          bVal = bFv?.stringValue ?? bFv?.decimalValue ?? '';
        }
      }

      if (typeof aVal === 'number' && typeof bVal === 'number') {
        return (aVal - bVal) * sign;
      }
      return String(aVal).localeCompare(String(bVal)) * sign;
    });
  }

  return groups;
}, [filteredGroups, sortConfig]);
```

Użyj `filteredAndSortedGroups` zamiast `filteredGroups` przy renderowaniu.

---

## 3. Poprawność akcji per poziom (pkt 2)

### Sprawdź i popraw w `TreeViewRow.tsx` — `GroupRow`

Wg dokumentacji:
- Etap/Podetap: + Dodaj podetap / + Dodaj pozycję / 🗑 Usuń etap

Sprawdź że w sekcji akcji grup oba przyciski są dostępne. Zgodnie z ce-ui-fix-02 przyciski mają być zawsze widoczne (disabled gdy !isEditMode).

### Sprawdź w `ItemRow`

Wg dokumentacji:
- Pozycja: + Dodaj opcję / + Dodaj komponent / 🗑 Usuń pozycję
- Komponent: + Dodaj opcję / 🗑 Usuń komponent
- Opcja: 🗑 Usuń opcję (brak dodawania)

Warunek dla komponentu: `isComponent === true` → pokaż "Dodaj opcję" + "Usuń". Brak "Dodaj komponent".

Warunek dla opcji: `isOption === true` → pokaż tylko "Usuń". Brak dodawania.

Warunek dla pozycji z komponentami: `hasComponents === true` → brak "Dodaj opcję" (opcje i komponenty nie mogą współistnieć).

Warunek dla pozycji z opcjami: `hasOptions === true` → brak "Dodaj komponent".

---

## 4. Typ CostEstimateDetailsWeb

Sprawdź plik `src/types/costEstimate.types.new.ts`. Jeśli `CostEstimateDetailsWeb` nie ma `totalNet`, `totalGross`, `totalVat` — dodaj:
```typescript
export interface CostEstimateDetailsWeb {
  // ... istniejące pola
  totalNet?: number;
  totalGross?: number;
  totalVat?: number;
}
```

Analogicznie sprawdź `CostEstimateGroupWeb` — powinno mieć `totalVat` (jest `totalNet` i `totalGross`, sprawdź czy jest `totalVat`).

---

## Weryfikacja

1. Wiersz "RAZEM" widoczny na dole tabeli z poprawną sumą netto i brutto
2. Kliknięcie nagłówka kolumny netValue/grossValue sortuje grupy rosnąco/malejąco
3. Kliknięcie nagłówka Nazwa sortuje grupy alfabetycznie
4. Akcje: etap ma "Dodaj podetap" + "Dodaj pozycję" + "Usuń"
5. Akcje: komponent ma "Dodaj opcję" + "Usuń" (brak "Dodaj komponent")
6. Akcje: opcja ma tylko "Usuń"
