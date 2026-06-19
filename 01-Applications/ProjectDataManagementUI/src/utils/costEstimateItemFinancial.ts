import type { CostEstimateItemWeb } from '../types/costEstimate.types.new';
import { roundToDecimals } from './numericInputUtils';

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

/** Ilość do obliczeń — domyślnie 1 gdy podano cenę, a ilość nie jest ustawiona. */
export function resolveCalculationQuantity(item: FinancialInput): number | undefined {
  if (item.quantity != null) {
    return item.quantity;
  }
  if (item.unitPriceNet != null || item.unitPriceGross != null) {
    return 1;
  }
  return undefined;
}

function calculateGrossFromUnitPriceGross(
  unitPriceGross: number | undefined,
  quantity: number | undefined,
): number | undefined {
  if (unitPriceGross != null && quantity != null) {
    return unitPriceGross * quantity;
  }
  return undefined;
}

function deriveLeafFinancialState(
  item: FinancialInput,
  lockedByChildren: boolean,
): ItemFinancialDerivedState {
  const qty = resolveCalculationQuantity(item);
  const unitNet = item.unitPriceNet ?? undefined;
  const vat = item.vatRate ?? undefined;

  const netValueComputed = !lockedByChildren && unitNet != null && qty != null;
  const netValue: number | undefined = netValueComputed
    ? roundToDecimals(unitNet * qty)
    : item.netValue !== undefined && item.netValue !== null
      ? roundToDecimals(item.netValue)
      : undefined;

  const vatValueComputed = !lockedByChildren && netValue != null && vat != null;
  const vatValue: number | undefined = vatValueComputed
    ? roundToDecimals(netValue * vat)
    : item.vatValue !== undefined && item.vatValue !== null
      ? roundToDecimals(item.vatValue)
      : undefined;

  let unitPriceGross: number | undefined;
  const unitPriceGrossComputed = !lockedByChildren && unitNet != null && vat != null;
  if (!lockedByChildren && unitNet != null && vat != null) {
    unitPriceGross = roundToDecimals(unitNet * (1 + vat));
  } else if (!lockedByChildren && item.unitPriceGross != null) {
    unitPriceGross = roundToDecimals(item.unitPriceGross);
  } else if (
    !lockedByChildren &&
    item.grossValue != null &&
    qty != null &&
    qty !== 0
  ) {
    unitPriceGross = roundToDecimals(item.grossValue / qty);
  } else {
    unitPriceGross = undefined;
  }

  const grossFromUnitPrice = calculateGrossFromUnitPriceGross(unitPriceGross, qty);
  const grossValueComputed =
    !lockedByChildren &&
    (grossFromUnitPrice != null ||
      (netValue != null && vatValue != null) ||
      (netValue != null && vat != null));
  let grossValue: number | undefined;
  if (!lockedByChildren && grossFromUnitPrice != null) {
    grossValue = roundToDecimals(grossFromUnitPrice);
  } else if (!lockedByChildren && netValue != null && vatValue != null) {
    grossValue = roundToDecimals(netValue + vatValue);
  } else if (!lockedByChildren && netValue != null && vat != null) {
    grossValue = roundToDecimals(netValue * (1 + vat));
  } else if (item.grossValue !== undefined && item.grossValue !== null) {
    grossValue = roundToDecimals(item.grossValue);
  } else {
    grossValue = undefined;
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

function aggregateFinancialsFromChildren(
  item: CostEstimateItemWeb,
): Pick<ItemFinancialDerivedState, 'netValue' | 'vatValue' | 'grossValue' | 'unitPriceGross'> | null {
  if ((item.components?.length ?? 0) > 0) {
    const summableComponents = item.components!.filter((child) => child.isSelected !== false);
    let netSum = 0;
    let vatSum = 0;
    let grossSum = 0;

    for (const child of summableComponents) {
      const childDerived = deriveLeafFinancialState(child, false);
      netSum += childDerived.netValue ?? 0;
      vatSum += childDerived.vatValue ?? 0;
      grossSum += childDerived.grossValue ?? 0;
    }

    return {
      netValue: roundToDecimals(netSum),
      vatValue: roundToDecimals(vatSum),
      grossValue: roundToDecimals(grossSum),
      unitPriceGross: undefined,
    };
  }

  const selectedOption = item.options?.find((option) => option.isSelected);
  if (selectedOption) {
    const optionDerived = deriveLeafFinancialState(selectedOption, false);
    return {
      netValue: optionDerived.netValue,
      vatValue: optionDerived.vatValue,
      grossValue: optionDerived.grossValue,
      unitPriceGross: optionDerived.unitPriceGross,
    };
  }

  return null;
}

export function deriveItemFinancialState(
  item: CostEstimateItemWeb,
  lockedByChildren: boolean = isItemFinancialLockedByChildren(item),
): ItemFinancialDerivedState {
  if (lockedByChildren) {
    const aggregated = aggregateFinancialsFromChildren(item);
    if (aggregated) {
      return {
        ...aggregated,
        netValueComputed: true,
        vatValueComputed: true,
        grossValueComputed: true,
        unitPriceGrossComputed: aggregated.unitPriceGross != null,
      };
    }
  }

  return deriveLeafFinancialState(item, lockedByChildren);
}

export function isItemFinancialLockedByChildren(item: CostEstimateItemWeb): boolean {
  const hasComponents = (item.components?.length ?? 0) > 0;
  const hasSelectedOption = item.options?.some((option) => option.isSelected) ?? false;
  return hasComponents || hasSelectedOption;
}

/** Wartość netto / VAT / brutto do wyświetlenia w komórce tabeli. */
export function getDerivedFinancialColumnValue(
  derived: ItemFinancialDerivedState,
  kind: 'net' | 'vat' | 'gross',
): number | undefined {
  if (kind === 'net') {
    return derived.netValue;
  }
  if (kind === 'vat') {
    return derived.vatValue;
  }
  return derived.grossValue;
}
