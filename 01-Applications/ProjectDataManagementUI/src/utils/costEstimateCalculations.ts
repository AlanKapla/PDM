/**
 * Stub modułu costEstimateCalculations.
 * 
 * Oryginalna logika obliczeń jest teraz w recalculateCostEstimateDetails.ts
 * używając direct properties (quantity, unitPriceNet, vatRate, netValue, grossValue, vatValue).
 *
 * Ten moduł zachowany dla kompatybilności z komponentami CostEstimateTableView, rows/*.
 * @deprecated Używaj recalculateCostEstimateDetails.ts
 */

import type { CostEstimateItemWeb } from '../types/costEstimate.types.new';

/**
 * Wszystkie wartości kalkulowane dla pozycji.
 */
export interface AllItemValues {
  quantity: number;
  unitPriceNet: number;
  vatRate: number;
  unitPriceGross: number;
  netValue: number;
  vatValue: number;
  grossValue: number;
  isSelected: boolean;
}

/**
 * Wartości kalkulowane dla pozycji (alias).
 */
export type ItemCalcValues = AllItemValues;

/**
 * Pobiera wszystkie wartości kalkulowane dla pozycji.
 * Używa direct properties z nowej architektury.
 * @param _templateStructure ignorowany (zachowany dla kompatybilności ze starymi komponentami)
 */
export function getAllValues(item: CostEstimateItemWeb, _templateStructure?: unknown): AllItemValues {
  return {
    quantity: item.quantity ?? 0,
    unitPriceNet: item.unitPriceNet ?? 0,
    vatRate: item.vatRate ?? 0,
    unitPriceGross: item.unitPriceGross ?? 0,
    netValue: item.netValue ?? 0,
    vatValue: item.vatValue ?? 0,
    grossValue: item.grossValue ?? 0,
    isSelected: item.isSelected !== false,
  };
}

/**
 * Pobiera wartości kalkulowane dla zaznaczonej opcji (lub zerowe jeśli brak zaznaczonej).
 * Signature overloaded dla kompatybilności z SortableOptionRow (stary kod).
 */
export function getAllOptionValues(
  itemOrFieldValues: CostEstimateItemWeb | unknown[],
  _templateStructureOrUndefined?: unknown,
  _parentItemOrUndefined?: unknown
): AllItemValues {
  // Nowa architektura: item przekazany bezpośrednio
  if (itemOrFieldValues && !Array.isArray(itemOrFieldValues)) {
    const item = itemOrFieldValues as CostEstimateItemWeb;
    const selectedOption = (item.options ?? []).find((opt) => opt.isSelected === true);
    if (selectedOption) {
      return getAllValues(selectedOption);
    }
  }
  return {
    quantity: 0,
    unitPriceNet: 0,
    vatRate: 0,
    unitPriceGross: 0,
    netValue: 0,
    vatValue: 0,
    grossValue: 0,
    isSelected: false,
  };
}

/**
 * Przelicza wartości kalkulowane dla pozycji.
 * @deprecated Używaj recalculateCostEstimateDetails
 * @param _templateStructure ignorowany (zachowany dla kompatybilności)
 * @param _targetFieldType ignorowany (zachowany dla kompatybilności)
 */
export function recalculateItem(item: CostEstimateItemWeb, _templateStructure?: unknown, _targetFieldType?: unknown): CostEstimateItemWeb {
  const qty = item.quantity ?? 0;
  const unitNet = item.unitPriceNet ?? 0;
  const vat = item.vatRate ?? 0;

  const netValue = qty * unitNet;
  const vatValue = netValue * vat;
  const grossValue = netValue + vatValue;
  const unitPriceGross = unitNet * (1 + vat);

  return { ...item, netValue, vatValue, grossValue, unitPriceGross };
}

/**
 * Przelicza wartości kalkulowane dla opcji.
 * @deprecated Używaj recalculateCostEstimateDetails
 * @param _templateStructure ignorowany (zachowany dla kompatybilności)
 * @param _parentItem ignorowany (zachowany dla kompatybilności)
 */
export function recalculateOption(item: CostEstimateItemWeb, _templateStructure?: unknown, _parentItem?: unknown): CostEstimateItemWeb {
  return recalculateItem(item);
}

/**
 * Zaokrągla do 2 miejsc po przecinku.
 */
export function round2(value: number): number {
  return Math.round(value * 100) / 100;
}

/**
 * Sprawdza czy fieldType jest polem źródłowym (quantity, unitPriceNet, vatRate).
 * @param _fieldScope ignorowany (zachowany dla kompatybilności)
 */
export function isSourceFieldType(fieldType: number, _fieldScope?: unknown): boolean {
  // ItemSystemQuantity=101, ItemCalculatedUnitPriceNet=200, ItemCalculatedVatRate=201
  return fieldType === 101 || fieldType === 200 || fieldType === 201;
}

/**
 * Sprawdza czy fieldType jest polem kalkulowanym.
 * @param _fieldScope ignorowany (zachowany dla kompatybilności)
 */
export function isCalculatedFieldType(fieldType: number, _fieldScope?: unknown): boolean {
  // ItemCalculated: 200-299
  return fieldType >= 200 && fieldType <= 299;
}

/**
 * Czyta wartość pola z fieldValues jako string.
 */
export function readFieldValue(
  fieldValues: import('../types/costEstimate.types.new').CostEstimateFieldValueWeb[] | undefined,
  fieldId: string
): string | undefined {
  const fv = (fieldValues ?? []).find((v) => v.fieldDefinitionId === fieldId);
  if (!fv) return undefined;
  if (fv.stringValue !== undefined && fv.stringValue !== null) return fv.stringValue;
  if (fv.decimalValue !== undefined && fv.decimalValue !== null) return String(fv.decimalValue);
  if (fv.boolValue !== undefined && fv.boolValue !== null) return String(fv.boolValue);
  if (fv.dateTimeValue !== undefined && fv.dateTimeValue !== null) return fv.dateTimeValue;
  return undefined;
}

/**
 * Pobiera wartości źródłowe (quantity, unitPriceNet, vatRate) z pozycji.
 */
export function getSourceValues(item: CostEstimateItemWeb): { quantity: number; unitPriceNet: number; vatRate: number } {
  return {
    quantity: item.quantity ?? 0,
    unitPriceNet: item.unitPriceNet ?? 0,
    vatRate: item.vatRate ?? 0,
  };
}

/**
 * Sprawdza czy można wyliczyć wartość kalkulowaną z dostępnych danych.
 * @param itemOrFieldType pozycja lub typ pola (kompatybilność z legacy code)
 * @param _allValuesOrUndefined ignorowany (kompatybilność z legacy code)
 */
export function canComputeFromAvailable(
  itemOrFieldType: CostEstimateItemWeb | number | unknown,
  _allValuesOrUndefined?: unknown
): boolean {
  if (typeof itemOrFieldType === 'object' && itemOrFieldType !== null && 'quantity' in itemOrFieldType) {
    const item = itemOrFieldType as CostEstimateItemWeb;
    return item.quantity !== undefined && item.unitPriceNet !== undefined;
  }
  // Legacy: fieldType as first argument — assume computable
  return true;
}

/**
 * Wylicza wartość pola kalkulowanego z dostępnych danych.
 * @deprecated Używaj recalculateCostEstimateDetails
 */
export function computeFieldFromAvailable(item: CostEstimateItemWeb, fieldType: number): number | undefined {
  const qty = item.quantity ?? 0;
  const unitNet = item.unitPriceNet ?? 0;
  const vat = item.vatRate ?? 0;
  const netValue = qty * unitNet;

  switch (fieldType) {
    case 200: return unitNet; // UnitPriceNet
    case 201: return vat;     // VatRate
    case 202: return unitNet * (1 + vat); // UnitPriceGross
    case 203: return netValue; // ValueNet
    case 204: return netValue + netValue * vat; // ValueGross
    case 205: return netValue * vat; // TotalVat
    case 206: return unitNet * vat; // UnitVat
    default: return undefined;
  }
}
