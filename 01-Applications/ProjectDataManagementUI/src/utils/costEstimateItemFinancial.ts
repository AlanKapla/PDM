/**
 * Wspólna logika wyliczania i blokowania pól finansowych pozycji kosztorysu.
 * Używana przez recalculateCostEstimateDetails i costEstimateItemFlags.
 */

import type { CostEstimateItemWeb } from '../types/costEstimate.types.new';

export interface ItemFinancialDerivedState {
  netValue: number | undefined;
  vatValue: number | undefined;
  grossValue: number | undefined;
  unitPriceGross: number | undefined;
  netValueComputed: boolean;
  vatValueComputed: boolean;
  grossValueComputed: boolean;
  unitPriceGrossComputed: boolean;
}

type FinancialInput = Pick<
  CostEstimateItemWeb,
  'quantity' | 'unitPriceNet' | 'vatRate' | 'netValue' | 'vatValue' | 'grossValue' | 'unitPriceGross'
>;

export function deriveItemFinancialState(
  item: FinancialInput,
  lockedByChildren = false,
): ItemFinancialDerivedState {
  const qty = item.quantity ?? undefined;
  const unitNet = item.unitPriceNet ?? undefined;
  const vat = item.vatRate ?? undefined;

  const netValueComputed = !lockedByChildren && unitNet != null && qty != null;
  const netValue: number | undefined = netValueComputed
    ? unitNet * qty
    : item.netValue ?? undefined;

  const vatValueComputed = !lockedByChildren && netValue != null && vat != null;
  const vatValue: number | undefined = vatValueComputed
    ? netValue * vat
    : item.vatValue ?? undefined;

  const grossValueComputed =
    !lockedByChildren &&
    ((netValue != null && vatValue != null) || (netValue != null && vat != null));
  let grossValue: number | undefined;
  if (!lockedByChildren && netValue != null && vatValue != null) {
    grossValue = netValue + vatValue;
  } else if (!lockedByChildren && netValue != null && vat != null) {
    grossValue = netValue * (1 + vat);
  } else {
    grossValue = item.grossValue ?? undefined;
  }

  const unitPriceGrossComputed =
    !lockedByChildren &&
    ((unitNet != null && vat != null) ||
      (grossValue != null && qty != null && qty !== 0));
  let unitPriceGross: number | undefined;
  if (!lockedByChildren && unitNet != null && vat != null) {
    unitPriceGross = unitNet * (1 + vat);
  } else if (!lockedByChildren && grossValue != null && qty != null && qty !== 0) {
    unitPriceGross = grossValue / qty;
  } else {
    unitPriceGross = item.unitPriceGross ?? undefined;
  }

  return {
    netValue,
    vatValue,
    grossValue,
    unitPriceGross,
    netValueComputed: lockedByChildren || netValueComputed,
    vatValueComputed: lockedByChildren || vatValueComputed,
    grossValueComputed: lockedByChildren || grossValueComputed,
    unitPriceGrossComputed: lockedByChildren || unitPriceGrossComputed,
  };
}

export function isItemFinancialLockedByChildren(item: CostEstimateItemWeb): boolean {
  const hasComponents = (item.components?.length ?? 0) > 0;
  const hasSelectedOption = item.options?.some((option) => option.isSelected) ?? false;
  return hasComponents || hasSelectedOption;
}
