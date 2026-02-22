/**
 * System obliczeń kosztorysu — czyste funkcje bez zależności od UI.
 *
 * Pola źródłowe (zawsze edytowalne, wpisywane ręcznie):
 *   101 - Quantity (ilość)
 *   200 - UnitPriceNet (cena jednostkowa netto)
 *   201 - VatRate (stawka VAT %)
 *
 * Pola obliczane (readonly gdy WSZYSTKIE wymagane pola źródłowe są wypełnione):
 *   202 - UnitPriceGross = netto × (1 + VAT/100)     wymaga: netto + VAT
 *   203 - ValueNet = netto × ilość                     wymaga: netto + ilość
 *   204 - ValueGross = brutto_jedn × ilość             wymaga: netto + VAT + ilość
 *   205 - UnitVat = netto × (VAT/100)                  wymaga: netto + VAT
 *   206 - TotalVat = VAT_jedn × ilość                  wymaga: netto + VAT + ilość
 *
 * ZASADY:
 * - Przeliczenie odpala się TYLKO przy edycji pola źródłowego (101, 200, 201)
 * - Edycja pola obliczanego NIE odpala przeliczenia
 * - Gdy brakuje danych źródłowych → pole obliczane jest edytowalne
 * - Gdy pojawiają się dane źródłowe → stare ręczne wartości są NADPISYWANE obliczonymi
 * - Gdy znikają dane źródłowe → obliczone wartości są USUWANE (pole staje się puste i edytowalne)
 */

import type {
  CostEstimateItemWeb,
  CostEstimateFieldValueWeb,
} from '../types/costEstimate.types.new';
import { FieldScope } from '../types/costEstimate.types';

// ---------------------------------------------------------------------------
// Stałe
// ---------------------------------------------------------------------------

export const SOURCE_FIELD_TYPES = new Set([101, 200, 201]);
export const CALCULATED_FIELD_TYPES = new Set([202, 203, 204, 205, 206]);

// Kolejność obliczeń: najpierw bazowe, potem pochodne
const CALC_ORDER = [202, 205, 203, 206, 204] as const;

// ---------------------------------------------------------------------------
// Typy
// ---------------------------------------------------------------------------

/** Wartości ŹRÓDŁOWE — do decyzji czy triggerować przeliczenie */
export interface ItemCalcValues {
  quantity?: number;
  unitPriceNet?: number;
  vatRate?: number;
}

/** WSZYSTKIE wartości pozycji — do sprawdzenia readonly i ścieżek alternatywnych */
export interface AllItemValues extends ItemCalcValues {
  unitPriceGross?: number;  // 202
  valueNet?: number;        // 203
  valueGross?: number;      // 204
  unitVat?: number;         // 205
  totalVat?: number;        // 206
}

export type ValueKey = keyof AllItemValues;

interface ComputePath {
  requires: ValueKey[];
  compute: (v: AllItemValues) => number;
}

// ---------------------------------------------------------------------------
// Pomocnicze
// ---------------------------------------------------------------------------

export const round2 = (v: number): number => Math.round(v * 100) / 100;

/**
 * Czyta wartość liczbową z fieldValues pozycji po fieldType.
 */
export const readFieldValue = (
  item: CostEstimateItemWeb,
  fieldType: number,
  fields: any[]
): number | undefined => {
  const def = fields.find((f: any) => (f.fieldType ?? f.fieldTypeConfig?.fieldType) === fieldType);
  if (!def) return undefined;
  const fv = item.fieldValues?.find((v) => v.fieldDefinitionId === def.id);
  if (!fv) return undefined;
  if (fv.decimalValue !== null && fv.decimalValue !== undefined) {
    return !isNaN(fv.decimalValue) ? fv.decimalValue : undefined;
  }
  if (fv.stringValue) {
    const p = parseFloat(fv.stringValue);
    return !isNaN(p) ? p : undefined;
  }
  return undefined;
};

/**
 * Pobiera TYLKO wartości pól źródłowych (101, 200, 201).
 */
export const getSourceValues = (
  item: CostEstimateItemWeb,
  templateStructure: any
): ItemCalcValues => {
  const sys = templateStructure.systemFields || [];
  const calc = templateStructure.calculatedFields || [];
  return {
    quantity: readFieldValue(item, 101, sys),
    unitPriceNet: readFieldValue(item, 200, calc),
    vatRate: readFieldValue(item, 201, calc),
  };
};

/**
 * Pobiera WSZYSTKIE wartości pozycji (źródłowe + obliczane/ręczne).
 */
export const getAllValues = (
  item: CostEstimateItemWeb,
  templateStructure: any
): AllItemValues => {
  const sys = templateStructure.systemFields || [];
  const calc = templateStructure.calculatedFields || [];
  return {
    quantity: readFieldValue(item, 101, sys),
    unitPriceNet: readFieldValue(item, 200, calc),
    vatRate: readFieldValue(item, 201, calc),
    unitPriceGross: readFieldValue(item, 202, calc),
    valueNet: readFieldValue(item, 203, calc),
    valueGross: readFieldValue(item, 204, calc),
    unitVat: readFieldValue(item, 205, calc),
    totalVat: readFieldValue(item, 206, calc),
  };
};

// ---------------------------------------------------------------------------
// Ścieżki obliczania
// ---------------------------------------------------------------------------

/**
 * Każde pole obliczane ma kilka ścieżek — pierwsza pasująca jest używana.
 * Pole jest READONLY gdy jakakolwiek ścieżka ma wszystkie wymagane wartości.
 */
const COMPUTE_PATHS: Record<number, ComputePath[]> = {
  // UnitPriceGross = netto × (1 + VAT/100)
  202: [
    { requires: ['unitPriceNet', 'vatRate'], compute: v => round2(v.unitPriceNet! * (1 + v.vatRate! / 100)) },
  ],
  // ValueNet = netto × ilość
  203: [
    { requires: ['unitPriceNet', 'quantity'], compute: v => round2(v.unitPriceNet! * v.quantity!) },
  ],
  // ValueGross — 4 ścieżki (ostatnia: brak VAT → brutto = netto)
  204: [
    { requires: ['unitPriceNet', 'vatRate', 'quantity'], compute: v => round2(v.unitPriceNet! * (1 + v.vatRate! / 100) * v.quantity!) },
    { requires: ['unitPriceGross', 'quantity'], compute: v => round2(v.unitPriceGross! * v.quantity!) },
    { requires: ['valueNet', 'totalVat'], compute: v => round2(v.valueNet! + v.totalVat!) },
    { requires: ['valueNet'], compute: v => round2(v.valueNet!) },
  ],
  // UnitVat = netto × (VAT/100)
  205: [
    { requires: ['unitPriceNet', 'vatRate'], compute: v => round2(v.unitPriceNet! * (v.vatRate! / 100)) },
  ],
  // TotalVat — 3 ścieżki
  206: [
    { requires: ['unitPriceNet', 'vatRate', 'quantity'], compute: v => round2(v.unitPriceNet! * v.quantity! * (v.vatRate! / 100)) },
    { requires: ['unitVat', 'quantity'], compute: v => round2(v.unitVat! * v.quantity!) },
    { requires: ['valueNet', 'vatRate'], compute: v => round2(v.valueNet! * (v.vatRate! / 100)) },
  ],
};

/**
 * Czy pole może być obliczone z dostępnych wartości?
 * Używane do sprawdzenia readonly — sprawdza WSZYSTKIE ścieżki.
 */
export const canComputeFromAvailable = (fieldType: number, vals: AllItemValues): boolean => {
  const paths = COMPUTE_PATHS[fieldType];
  if (!paths) return false;
  return paths.some(path => path.requires.every(key => vals[key] !== undefined));
};

/**
 * Oblicz wartość pola pierwszą dostępną ścieżką.
 */
export const computeFieldFromAvailable = (fieldType: number, vals: AllItemValues): number | undefined => {
  const paths = COMPUTE_PATHS[fieldType];
  if (!paths) return undefined;
  for (const path of paths) {
    if (path.requires.every(key => vals[key] !== undefined)) {
      return path.compute(vals);
    }
  }
  return undefined;
};

// ---------------------------------------------------------------------------
// Przeliczanie pozycji
// ---------------------------------------------------------------------------

/**
 * Przelicza pozycję po zmianie pola.
 * Dla każdego pola obliczanego:
 * - jeśli MOŻNA obliczyć → zapisz wartość
 * - jeśli NIE MOŻNA → NIE usuwaj (mogło być wpisane ręcznie)
 *
 * @param skipFieldType — nie nadpisuj pola, które właśnie zostało ręcznie zmienione
 */
export const recalculateItem = (
  item: CostEstimateItemWeb,
  templateStructure: any,
  skipFieldType?: number
): CostEstimateItemWeb => {
  const calculatedFields = templateStructure.calculatedFields || [];
  let fieldValues = [...(item.fieldValues || [])];

  for (const calcFieldType of CALC_ORDER) {
    if (calcFieldType === skipFieldType) continue;

    const def = calculatedFields.find((f: any) =>
      (f.fieldType ?? f.fieldTypeConfig?.fieldType) === calcFieldType
    );
    if (!def) continue;

    const currentItem: CostEstimateItemWeb = { ...item, fieldValues };
    const vals = getAllValues(currentItem, templateStructure);
    const computed = computeFieldFromAvailable(calcFieldType, vals);

    const idx = fieldValues.findIndex((fv) => fv.fieldDefinitionId === def.id);

    if (computed !== undefined) {
      if (idx !== -1) {
        fieldValues[idx] = {
          ...fieldValues[idx],
          decimalValue: computed,
          stringValue: computed.toString(),
        };
      } else {
        fieldValues.push({
          id: `calc_${Date.now()}_${def.id}`,
          fieldDefinitionId: def.id,
          fieldType: calcFieldType,
          fieldScope: FieldScope.ItemCalculated,
          fieldName: def.fieldName,
          fieldLabel: def.label,
          decimalValue: computed,
          stringValue: computed.toString(),
        });
      }
    }
  }

  return { ...item, fieldValues };
};

// ---------------------------------------------------------------------------
// Obliczenia dla opcji/wariantów
// ---------------------------------------------------------------------------

/**
 * Pobiera childField definitions z pola collection (Options) w templateStructure.
 */
export const getChildFieldDefs = (templateStructure: any): any[] => {
  const optionsField = (templateStructure.systemFields || []).find(
    (f: any) => f.fieldTypeConfig?.isCollection && f.childFields?.length > 0
  );
  return optionsField?.childFields || [];
};

/**
 * Czyta wartość liczbową z fieldValues opcji po fieldType, używając childField definitions.
 */
export const readOptionFieldValue = (
  optionFieldValues: any[],
  fieldType: number,
  childFieldDefs: any[]
): number | undefined => {
  const def = childFieldDefs.find((f: any) => (f.fieldType ?? f.fieldTypeConfig?.fieldType) === fieldType);
  if (!def) return undefined;
  const fv = optionFieldValues.find((v: any) => v.fieldDefinitionId === def.id);
  if (!fv) return undefined;
  if (fv.decimalValue !== null && fv.decimalValue !== undefined) {
    return !isNaN(fv.decimalValue) ? fv.decimalValue : undefined;
  }
  if (fv.stringValue) {
    const p = parseFloat(fv.stringValue);
    return !isNaN(p) ? p : undefined;
  }
  return undefined;
};

/**
 * Pobiera WSZYSTKIE wartości opcji (źródłowe + obliczane) z childField definitions.
 * Ilość (quantity, fieldType 101) jest brana z pozycji nadrzędnej.
 */
export const getAllOptionValues = (
  optionFieldValues: any[],
  templateStructure: any,
  parentItem?: CostEstimateItemWeb
): AllItemValues => {
  const childFieldDefs = getChildFieldDefs(templateStructure);
  const parentQuantity = parentItem
    ? readFieldValue(parentItem, 101, templateStructure.systemFields || [])
    : undefined;
  return {
    quantity: parentQuantity,
    unitPriceNet: readOptionFieldValue(optionFieldValues, 200, childFieldDefs),
    vatRate: readOptionFieldValue(optionFieldValues, 201, childFieldDefs),
    unitPriceGross: readOptionFieldValue(optionFieldValues, 202, childFieldDefs),
    valueNet: readOptionFieldValue(optionFieldValues, 203, childFieldDefs),
    valueGross: readOptionFieldValue(optionFieldValues, 204, childFieldDefs),
    unitVat: readOptionFieldValue(optionFieldValues, 205, childFieldDefs),
    totalVat: readOptionFieldValue(optionFieldValues, 206, childFieldDefs),
  };
};

/**
 * Przelicza opcję/wariant po zmianie pola — analogicznie do recalculateItem,
 * ale działa na childField definitions zamiast templateStructure.calculatedFields.
 */
export const recalculateOption = (
  optionFieldValues: any[],
  templateStructure: any,
  parentItem?: CostEstimateItemWeb,
  skipFieldType?: number
): any[] => {
  const childFieldDefs = getChildFieldDefs(templateStructure);
  let fieldValues = [...optionFieldValues];

  for (const calcFieldType of CALC_ORDER) {
    if (calcFieldType === skipFieldType) continue;

    const def = childFieldDefs.find((f: any) =>
      (f.fieldType ?? f.fieldTypeConfig?.fieldType) === calcFieldType
    );
    if (!def) continue;

    const vals = getAllOptionValues(fieldValues, templateStructure, parentItem);
    const computed = computeFieldFromAvailable(calcFieldType, vals);

    const idx = fieldValues.findIndex((fv: any) => fv.fieldDefinitionId === def.id);

    if (computed !== undefined) {
      if (idx !== -1) {
        fieldValues[idx] = {
          ...fieldValues[idx],
          decimalValue: computed,
          stringValue: computed.toString(),
        };
      } else {
        fieldValues.push({
          id: `calc_opt_${Date.now()}_${def.id}`,
          fieldDefinitionId: def.id,
          fieldType: calcFieldType,
          fieldScope: FieldScope.ItemCalculated,
          fieldName: def.fieldName,
          fieldLabel: def.label,
          decimalValue: computed,
          stringValue: computed.toString(),
        });
      }
    }
  }

  return fieldValues;
};
