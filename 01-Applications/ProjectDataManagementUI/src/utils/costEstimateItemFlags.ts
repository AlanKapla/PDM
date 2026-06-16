/**
 * Flagi blokowania pól pozycji kosztorysu — wspólna logika dla Tree View i Card View.
 *
 * Określa, które pola są kalkulowane automatycznie (readonly) vs. edytowalne ręcznie.
 * Zgodne z regułami w costEstimateItemFinancial.ts.
 */

import type { CostEstimateItemWeb, ComputedFlags } from '../types/costEstimate.types.new';
import {
  deriveItemFinancialState,
  isItemFinancialLockedByChildren,
} from './costEstimateItemFinancial';

export type { ComputedFlags };

/**
 * Oblicza flagi pól kalkulowanych dla pozycji.
 */
export function computeItemFieldFlags(item: CostEstimateItemWeb): ComputedFlags {
  const hasComponents = (item.components?.length ?? 0) > 0;
  const hasOptions = (item.options?.length ?? 0) > 0;
  const lockedByChildren = isItemFinancialLockedByChildren(item);
  const derived = deriveItemFinancialState(item, lockedByChildren);

  return {
    netValueComputed: derived.netValueComputed,
    vatValueComputed: derived.vatValueComputed,
    grossValueComputed: derived.grossValueComputed,
    unitPriceGrossComputed: derived.unitPriceGrossComputed,
    financialFieldsLockedByComponents: hasComponents,
    financialFieldsLockedByOptions: hasOptions,
  };
}

export interface CostEstimateItemFieldState {
  isComponent: boolean;
  isOption: boolean;
  hasComponents: boolean;
  hasOptions: boolean;
  hasSelectedOption: boolean;
  flags: ComputedFlags;
}

/**
 * Zwraca pełny stan pól pozycji — używany przez Tree View i Card View.
 */
export function getCostEstimateItemFieldState(item: CostEstimateItemWeb): CostEstimateItemFieldState {
  const isComponent = item.relationType === 2;
  const isOption = item.relationType === 1;
  const hasComponents = (item.components?.length ?? 0) > 0;
  const hasOptions = (item.options?.length ?? 0) > 0;
  const hasSelectedOption = item.options?.some((o) => o.isSelected) ?? false;
  const flags = computeItemFieldFlags(item);

  return {
    isComponent,
    isOption,
    hasComponents,
    hasOptions,
    hasSelectedOption,
    flags,
  };
}

/** Pola źródłowe (ilość, jednostka, cena netto, VAT) — zablokowane gdy pozycja ma komponenty lub wybraną opcję. */
export function areItemSourceFieldsLocked(state: CostEstimateItemFieldState): boolean {
  return state.flags.financialFieldsLockedByComponents || state.hasSelectedOption;
}

/** Pola dodatkowe — zablokowane gdy wybrano opcję (wartości pochodzą z opcji). Pozycje z komponentami mają własne wartości. */
export function areItemAdditionalFieldsLocked(state: CostEstimateItemFieldState): boolean {
  return state.hasSelectedOption;
}

/** Nazwa pozycji — zablokowana gdy wybrano opcję (wartości pochodzą z opcji). */
export function isItemNameLocked(state: CostEstimateItemFieldState): boolean {
  return state.hasSelectedOption;
}
