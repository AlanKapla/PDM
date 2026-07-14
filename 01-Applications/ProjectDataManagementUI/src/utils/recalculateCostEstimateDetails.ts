/**
 * Przeliczanie sum i wartości kalkulowanych kosztorysu (live preview).
 *
 * Odpowiada za:
 *  1. Przeliczenie wartości kalkulowanych w każdej pozycji (quantity × unitPrice itp.)
 *  2. Obsługę komponentów (sumowanie z komponentów do pozycji nadrzędnej)
 *  3. Obsługę opcji z zaznaczeniem (kopiowanie wartości z zaznaczonej opcji)
 *  4. Agregację sum na poziomie grup i całego kosztorysu
 *
 * Używa BEZPOŚREDNICH właściwości na pozycji: quantity, unitPriceNet, vatRate,
 * netValue, grossValue, vatValue, isSelected — zgodnie z nową architekturą.
 */

import type {
  CostEstimateDetailsWeb,
  CostEstimateGroupWeb,
  CostEstimateItemWeb,
  CostEstimateAdditionalFieldValueWeb,
} from '../types/costEstimate.types.new';
import { cloneAdditionalFieldValues } from './additionalFieldHelpers';
import { deriveItemFinancialState } from './costEstimateItemFinancial';

// ---------------------------------------------------------------------------
// Obliczenia dla pojedynczej pozycji (direct properties)
// ---------------------------------------------------------------------------

/**
 * Przelicza wartości kalkulowane pozycji na podstawie direct properties.
 * Obsługuje zarówno obliczone (z unitPriceNet × quantity) jak i ręcznie wpisane wartości.
 */
function calculateDerivedValues(item: CostEstimateItemWeb): CostEstimateItemWeb {
  const derived = deriveItemFinancialState(item);
  return {
    ...item,
    netValue: derived.netValue,
    vatValue: derived.vatValue,
    grossValue: derived.grossValue,
    unitPriceGross: derived.unitPriceGross,
  };
}

// ---------------------------------------------------------------------------
// Obliczenia dla pozycji z komponentami i opcjami
// ---------------------------------------------------------------------------

/**
 * Przelicza jedną pozycję z obsługą komponentów i opcji.
 *
 * Komponenty (relationType=2):
 *   - Przelicz każdy komponent rekurencyjnie
 *   - Sumuj netValue/grossValue/vatValue komponentów z isSelected=true do pozycji nadrzędnej
 *
 * Opcje (relationType=1):
 *   - Znajdź zaznaczoną opcję (isSelected=true)
 *   - Skopiuj jej wartości kalkulowane do pozycji nadrzędnej
 *
 * Standardowe:
 *   - Oblicz z direct properties
 */
function calculateItemValues(item: CostEstimateItemWeb): CostEstimateItemWeb {
  // --- Komponenty: przelicz każdy, potem zsumuj do pozycji nadrzędnej ---
  if (item.components && item.components.length > 0) {
    const recalcComponents = item.components.map((comp) =>
      calculateItemValues({ ...comp, components: undefined })
    );

    // Sumuj komponenty z isSelected=true (lub wszystkie jeśli żaden nie ma isSelected=false)
    const summableComponents = recalcComponents.filter((c) => c.isSelected !== false);

    const sumNet = summableComponents.reduce((s, c) => s + (c.netValue ?? 0), 0);
    const sumGross = summableComponents.reduce((s, c) => s + (c.grossValue ?? 0), 0);
    const sumVat = summableComponents.reduce((s, c) => s + (c.vatValue ?? 0), 0);

    return {
      ...item,
      components: recalcComponents,
      netValue: sumNet,
      grossValue: sumGross,
      vatValue: sumVat,
    };
  }

  // --- Opcje z zaznaczeniem: kopiuj wartości z wybranej opcji ---
  if (item.options && item.options.length > 0) {
    const recalcOptions = item.options.map(calculateItemValues);

    // Znajdź zaznaczoną opcję
    const selectedOption = recalcOptions.find((opt) => opt.isSelected === true);

    if (selectedOption) {
      return {
        ...item,
        options: recalcOptions,
        netValue: selectedOption.netValue,
        grossValue: selectedOption.grossValue,
        vatValue: selectedOption.vatValue,
        unitPriceGross: selectedOption.unitPriceGross,
        additionalFieldValues: cloneAdditionalFieldValues(selectedOption.additionalFieldValues),
      };
    }

    // Brak zaznaczonej opcji — użyj własnych wartości pozycji (jak dla zwykłej pozycji)
    const derived = calculateDerivedValues(item);
    return {
      ...derived,
      options: recalcOptions,
    };
  }

  // --- Standardowe obliczenia ---
  return calculateDerivedValues(item);
}

// ---------------------------------------------------------------------------
// Agregacja sum dla grupy (bottom-up)
// ---------------------------------------------------------------------------

/**
 * Przelicza grupę (rekurencyjnie, bottom-up).
 * Sumuje netValue/grossValue/vatValue pozycji z isSelected=true.
 */
function recalculateGroup(group: CostEstimateGroupWeb): CostEstimateGroupWeb {
  const updatedChildGroups = group.childGroups.map(recalculateGroup);
  const updatedItems = (group.items ?? []).map(calculateItemValues);

  // Sumuj tylko pozycje zaznaczone (isSelected=true, domyślnie true gdy brak flagi)
  const selectedItems = updatedItems.filter((itm) => itm.isSelected !== false);

  const itemsNet = selectedItems.reduce((s, itm) => s + (itm.netValue ?? 0), 0);
  const itemsGross = selectedItems.reduce((s, itm) => s + (itm.grossValue ?? 0), 0);
  const itemsVat = selectedItems.reduce((s, itm) => s + (itm.vatValue ?? 0), 0);

  const childNet = updatedChildGroups.reduce((s, ch) => s + (ch.totalNet ?? 0), 0);
  const childGross = updatedChildGroups.reduce((s, ch) => s + (ch.totalGross ?? 0), 0);
  const childVat = updatedChildGroups.reduce((s, ch) => s + (ch.totalVat ?? 0), 0);

  return {
    ...group,
    items: updatedItems,
    childGroups: updatedChildGroups,
    totalNet: itemsNet + childNet,
    totalGross: itemsGross + childGross,
    totalVat: itemsVat + childVat,
    lastCalculatedAt: new Date().toISOString(),
  };
}

// ---------------------------------------------------------------------------
// Główna funkcja eksportowana
// ---------------------------------------------------------------------------

/**
 * Przelicza cały kosztorys: wartości pozycji, sumy grup i sumy całkowite.
 * Zwraca nowy obiekt details z uaktualnionymi wartościami.
 *
 * Używa wyłącznie direct properties na CostEstimateItemWeb i CostEstimateGroupWeb
 * (bez fieldValues, bez schema).
 */
export function recalculateCostEstimateDetails(
  data: CostEstimateDetailsWeb,
): CostEstimateDetailsWeb {
  const recalculatedRootGroups = data.rootGroups.map(recalculateGroup);

  const totalNet = recalculatedRootGroups.reduce((s, g) => s + (g.totalNet ?? 0), 0);
  const totalGross = recalculatedRootGroups.reduce((s, g) => s + (g.totalGross ?? 0), 0);
  const totalVat = recalculatedRootGroups.reduce((s, g) => s + (g.totalVat ?? 0), 0);

  return {
    ...data,
    rootGroups: recalculatedRootGroups,
    totalNet,
    totalGross,
    totalVat,
    lastCalculatedAt: new Date().toISOString(),
  };
}
