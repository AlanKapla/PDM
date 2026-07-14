# ce-ui-fix-01 — Silnik kalkulacji client-side + edytowalne pola finansowe

## Cel
Zaimplementować pełny silnik obliczeń po stronie UI w komponencie TreeView — każde pole finansowe ma być edytowalne gdy nie jest obliczane. Logika wg dokumentacji (plik `.opencode/documentation_ce`).

## Przeczytaj skill przed implementacją
`.github/skills/ui-components/SKILL.md`

---

## Zasady kalkulacji (z dokumentacji)

```
NetValue:
  jeśli UnitPriceNet && Quantity → NetValue = UnitPriceNet × Quantity, pole ZABLOKOWANE
  w przeciwnym razie → pole EDYTOWALNE

VatValue:
  jeśli NetValue && VatRate → VatValue = NetValue × VatRate, pole ZABLOKOWANE
  w przeciwnym razie → pole EDYTOWALNE

GrossValue:
  jeśli NetValue && VatValue → GrossValue = NetValue + VatValue, pole ZABLOKOWANE
  jeśli NetValue && VatRate → GrossValue = NetValue × (1 + VatRate), pole ZABLOKOWANE
  w przeciwnym razie → pole EDYTOWALNE

UnitPriceGross (wyliczany):
  jeśli UnitPriceNet && VatRate → UnitPriceGross = UnitPriceNet × (1 + VatRate), pole ZABLOKOWANE
  jeśli GrossValue && Quantity != 0 → UnitPriceGross = GrossValue / Quantity, pole ZABLOKOWANE
  w przeciwnym razie → pole EDYTOWALNE
```

### Gdy pozycja ma komponenty
Pola finansowe pozycji są zablokowane — wyliczane jako suma wartości komponentów. Edytowalne tylko: Nazwa i IsSelected (Sumuj).

### Gdy pozycja ma opcje
Pola finansowe pozycji są nadpisywane wartością wybranej opcji. Edytowalne tylko opcje, nie pozycja.

---

## Plik do modyfikacji

`src/utils/recalculateCostEstimateDetails.ts`

### Funkcja `calculateDerivedValues` — ZASTĄP obecną implementację

Obecna implementacja `calculateDerivedValues` oblicza tylko jeśli wszystkie trzy pola są obecne (unitPriceNet × quantity). Brakuje obsługi:
- Edytowalności pól wg zasad
- Wpisanego z palca NetValue → obliczenie VatValue i GrossValue
- Wpisanego z palca GrossValue bez NetValue
- Wpisanego z palca VatValue bez VatRate

Dodaj do zwracanego obiektu flagi `_computed`:

```typescript
export interface ComputedFlags {
  netValueComputed: boolean;       // true = zablokowany
  vatValueComputed: boolean;       // true = zablokowany
  grossValueComputed: boolean;     // true = zablokowany
  unitPriceGrossComputed: boolean; // true = zablokowany
  financialFieldsLockedByComponents: boolean; // pozycja ma komponenty
  financialFieldsLockedByOptions: boolean;    // pozycja ma opcje
}
```

Rozszerz `CostEstimateItemWeb` w typach (lub użyj intersection) o `_computed?: ComputedFlags`.

Nowa implementacja `calculateDerivedValues`:

```typescript
function calculateDerivedValues(item: CostEstimateItemWeb): CostEstimateItemWeb & { _computed: ComputedFlags } {
  const qty = item.quantity ?? undefined;
  const unitNet = item.unitPriceNet ?? undefined;
  const vat = item.vatRate ?? undefined;
  let netValue = item.netValue ?? undefined;
  let vatValue = item.vatValue ?? undefined;
  let grossValue = item.grossValue ?? undefined;
  let unitPriceGross = item.unitPriceGross ?? undefined;

  let netValueComputed = false;
  let vatValueComputed = false;
  let grossValueComputed = false;
  let unitPriceGrossComputed = false;

  // NetValue
  if (unitNet !== undefined && qty !== undefined) {
    netValue = unitNet * qty;
    netValueComputed = true;
  }

  // VatValue
  if (netValue !== undefined && vat !== undefined) {
    vatValue = netValue * vat;
    vatValueComputed = true;
  }

  // GrossValue
  if (netValue !== undefined && vatValue !== undefined) {
    grossValue = netValue + vatValue;
    grossValueComputed = true;
  } else if (netValue !== undefined && vat !== undefined) {
    grossValue = netValue * (1 + vat);
    grossValueComputed = true;
  }

  // UnitPriceGross
  if (unitNet !== undefined && vat !== undefined) {
    unitPriceGross = unitNet * (1 + vat);
    unitPriceGrossComputed = true;
  } else if (grossValue !== undefined && qty !== undefined && qty !== 0) {
    unitPriceGross = grossValue / qty;
    unitPriceGrossComputed = true;
  }

  return {
    ...item,
    netValue,
    vatValue,
    grossValue,
    unitPriceGross,
    _computed: {
      netValueComputed,
      vatValueComputed,
      grossValueComputed,
      unitPriceGrossComputed,
      financialFieldsLockedByComponents: false,
      financialFieldsLockedByOptions: false,
    },
  };
}
```

Zaktualizuj `calculateItemValues` aby propagował `_computed.financialFieldsLockedByComponents = true` gdy pozycja ma komponenty, i `financialFieldsLockedByOptions = true` gdy ma opcje.

---

## Plik do modyfikacji: `src/components/CostEstimate/TreeView/TreeViewRow.tsx`

### Cel zmian w `renderBaseFieldCells` w `ItemRow`

Pola `netValue`, `grossValue`, `vatValue`, `unitPriceGross` — ZMIEŃ z read-only `<Text>` na `PrototypeNumberInput` z warunkiem `isDisabled`.

Aby obliczyć flagi, wynik `calculateDerivedValues` musi być dostępny w `ItemRow`. Przekaż `_computed` jako prop lub oblicz go lokalnie na podstawie `item`:

```typescript
// Helper — oblicz flagi bezpośrednio na podstawie wartości item
function computeFlags(item: CostEstimateItemWeb): ComputedFlags {
  const qty = item.quantity ?? undefined;
  const unitNet = item.unitPriceNet ?? undefined;
  const vat = item.vatRate ?? undefined;
  const net = item.netValue ?? undefined;
  const vv = item.vatValue ?? undefined;

  const netValueComputed = unitNet !== undefined && qty !== undefined;
  const vatValueComputed = net !== undefined && vat !== undefined;
  const grossValueComputed =
    (net !== undefined && vv !== undefined) ||
    (net !== undefined && vat !== undefined);
  const unitPriceGrossComputed =
    (unitNet !== undefined && vat !== undefined) ||
    (item.grossValue !== undefined && qty !== undefined && qty !== 0);

  return {
    netValueComputed,
    vatValueComputed,
    grossValueComputed,
    unitPriceGrossComputed,
    financialFieldsLockedByComponents: (item.components?.length ?? 0) > 0,
    financialFieldsLockedByOptions: (item.options?.length ?? 0) > 0,
  };
}
```

### Renderowanie pól finansowych

Wewnątrz `renderBaseFieldCells`:

**netValue:**
```tsx
const flags = computeFlags(item);
const isNetLocked = flags.netValueComputed || flags.financialFieldsLockedByComponents || flags.financialFieldsLockedByOptions;

return (
  <Flex key="netValue" flex="0 0 auto" w={w} justify="flex-end" pr={1}>
    <PrototypeNumberInput
      value={item.netValue !== undefined && item.netValue !== null ? String(item.netValue) : ''}
      onChange={(e) => {
        const v = e.target.value;
        onFieldChange(groupId, item.id, 'netValue', v);
        triggerBaseAutosave('netValue', 'numeric', v);
      }}
      isDisabled={!isEditMode || isNetLocked}
      placeholder="0.00"
      w="full"
      fontWeight="600"
    />
  </Flex>
);
```

Analogicznie dla `vatValue`, `grossValue`, `unitPriceGross` z odpowiednimi flagami.

### Autosave po zmianie pola wyzwalającego

Gdy user wpisze `netValue` ręcznie → po zapisaniu, jeśli teraz `netValue && vatRate` → VatValue i GrossValue zostaną automatycznie obliczone przez silnik kalkulacji (w `recalculateCostEstimateDetails.ts`). Silnik działa w CostEstimateEditPage przy każdej zmianie pola.

Sprawdź w `CostEstimateEditPage.tsx` czy `handleFieldChange` → wywołuje `recalculateCostEstimateDetails` na całym drzewie → aktualizuje `details`. Jeśli tak — przepływ jest poprawny. Jeśli nie — znajdź gdzie jest wywoływane przeliczanie i upewnij się że jest wywołane po każdej zmianie pola finansowego.

---

## Weryfikacja

Scenariusze do sprawdzenia ręcznie:
1. Wpisz Quantity=2, UnitPriceNet=100 → NetValue = 200 (zablokowane), inne pola dalej edytowalne
2. Wpisz NetValue=200 z palca (bez qty/price) → edytowalne
3. Wpisz NetValue=200 + VatRate=0.23 → VatValue=46 (zablokowane), GrossValue=246 (zablokowane)
4. Pozycja z komponentami → wszystkie pola finansowe zablokowane
5. Opcja zaznaczona → wartości pozycji aktualizują się
