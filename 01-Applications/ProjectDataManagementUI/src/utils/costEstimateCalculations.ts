/**
 * System obliczeń kosztorysu — czyste funkcje bez zależności od UI.
 *
 * Pola źródłowe (zawsze edytowalne, wpisywane ręcznie):
 *   101 - Quantity (ilość)
 *   200 - UnitPriceNet (cena jednostkowa netto)
 *   201 - VatRate (stawka VAT %, wartość 0-1, np. 0.23 = 23%)
 *   207 - Discount (rabat %, wartość 0-1, np. 0.1 = 10%)
 *
 * Pola obliczane (readonly gdy WSZYSTKIE wymagane pola źródłowe są wypełnione):
 *   202 - UnitPriceGross = netto × (1 + VAT)           wymaga: netto + VAT
 *   203 - ValueNet = netto × ilość × (1 - rabat)       wymaga: netto + ilość (rabat opcjonalny)
 *   204 - ValueGross = brutto_jedn × ilość × (1-rabat) wymaga: netto + VAT + ilość
 *   205 - UnitVat = netto × VAT                        wymaga: netto + VAT
 *   206 - TotalVat = VAT_jedn × ilość × (1 - rabat)    wymaga: netto + VAT + ilość
 *
 * RABAT (Discount):
 * - Rabat jest stosowany TYLKO na wartości łączne (ValueNet, TotalVat, ValueGross)
 * - Pola jednostkowe (UnitPriceNet, UnitPriceGross, UnitVat) NIE są modyfikowane przez rabat
 * - Formuła: discountMultiplier = 1 - discount (np. discount=0.1 → multiplier=0.9)
 *
 * ZASADY:
 * - Przeliczenie odpala się TYLKO przy edycji pola źródłowego (101, 200, 201, 207)
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

// Nowe fieldType (używane w kodzie)
const SOURCE_FIELD_TYPES_NEW = new Set([101, 200, 201, 207]);
const CALCULATED_FIELD_TYPES_NEW = new Set([202, 203, 204, 205, 206]);

// Legacy fieldType (mogą być zwracane przez backend)
// System: 0=Name, 1=Quantity, 2=Unit, 3=Options, 4=Selected
// Calculated: 0=UnitPriceNet, 1=VatRate, 2=UnitPriceGross, 3=ValueNet, 4=ValueGross, 5=UnitVat, 6=TotalVat, 7=Discount
const SOURCE_FIELD_TYPES_LEGACY = new Set([1, 0, 1, 7]); // quantity(1), unitPriceNet(0), vatRate(1), discount(7)
const CALCULATED_FIELD_TYPES_LEGACY = new Set([2, 3, 4, 5, 6]); // 2-6

/**
 * Normalizuje fieldType do nowej wartości (200-299 dla calculated, 100-199 dla system).
 * Obsługuje zarówno nowy jak i legacy fieldType.
 */
export const normalizeFieldType = (fieldType: number, fieldScope?: number): number => {
  // Już znormalizowany (nowy format)
  if (fieldType >= 100) return fieldType;
  
  // Legacy format - konwertuj na nowy w zależności od scope
  // fieldScope: 0=Group, 1=ItemSystem, 2=ItemCalculated, 3=ItemGeneric
  if (fieldScope === 2 || fieldScope === undefined) {
    // ItemCalculated: 0-7 → 200-207
    return fieldType + 200;
  } else if (fieldScope === 1) {
    // ItemSystem: 0-4 → 100-104
    return fieldType + 100;
  } else if (fieldScope === 3) {
    // ItemGeneric: 0-5 → 300-305
    return fieldType + 300;
  }
  return fieldType;
};

/**
 * Sprawdza czy fieldType jest polem źródłowym (edytowalnym przez użytkownika).
 * Obsługuje zarówno nowy jak i legacy fieldType.
 */
export const isSourceFieldType = (fieldType: number, fieldScope?: number): boolean => {
  const normalized = normalizeFieldType(fieldType, fieldScope);
  return SOURCE_FIELD_TYPES_NEW.has(normalized);
};

/**
 * Sprawdza czy fieldType jest polem obliczanym.
 * Obsługuje zarówno nowy jak i legacy fieldType.
 */
export const isCalculatedFieldType = (fieldType: number, fieldScope?: number): boolean => {
  const normalized = normalizeFieldType(fieldType, fieldScope);
  return CALCULATED_FIELD_TYPES_NEW.has(normalized);
};

// Eksport dla kompatybilności wstecznej (używane w komponentach)
export const SOURCE_FIELD_TYPES = SOURCE_FIELD_TYPES_NEW;
export const CALCULATED_FIELD_TYPES = CALCULATED_FIELD_TYPES_NEW;

// Kolejność obliczeń: najpierw bazowe, potem pochodne (rabat wpływa na wartości łączne)
const CALC_ORDER = [202, 205, 203, 206, 204] as const;

// ---------------------------------------------------------------------------
// Typy
// ---------------------------------------------------------------------------

/** Wartości ŹRÓDŁOWE — do decyzji czy triggerować przeliczenie */
export interface ItemCalcValues {
  quantity?: number;
  unitPriceNet?: number;
  vatRate?: number;
  discount?: number;        // 207 - rabat (0-1, np. 0.1 = 10%)
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
 * Konwertuje nowy fieldType (200-299) na legacy (0-7) lub odwrotnie.
 * Używane do porównania definicji pól z backendu z wartościami w kodzie.
 */
const matchFieldType = (defFieldType: number | undefined, searchFieldType: number): boolean => {
  if (defFieldType === undefined) return false;
  if (defFieldType === searchFieldType) return true;
  
  // Konwersja legacy (0-7) na nowy (200-207) dla pól kalkulowanych
  if (searchFieldType >= 200 && searchFieldType <= 299) {
    const legacyType = searchFieldType - 200;
    return defFieldType === legacyType;
  }
  
  // Konwersja legacy (0-4) na nowy (100-104) dla pól systemowych
  if (searchFieldType >= 100 && searchFieldType <= 199) {
    const legacyType = searchFieldType - 100;
    return defFieldType === legacyType;
  }
  
  return false;
};

/**
 * Czyta wartość liczbową z fieldValues pozycji po fieldType.
 * Obsługuje zarówno nowy fieldType (200-299) jak i legacy (0-7).
 */
export const readFieldValue = (
  item: CostEstimateItemWeb,
  fieldType: number,
  fields: any[]
): number | undefined => {
  // Najpierw próbuj znaleźć przez definicję pola
  const def = fields.find((f: any) => {
    const defType = f.fieldType ?? f.fieldTypeConfig?.fieldType;
    return matchFieldType(defType, fieldType);
  });
  
  let fv: any = undefined;
  
  if (def) {
    fv = item.fieldValues?.find((v) => v.fieldDefinitionId === def.id);
  }
  
  // Jeśli nie znaleziono przez definicję, szukaj bezpośrednio po fieldType w fieldValues
  if (!fv) {
    fv = item.fieldValues?.find((v) => {
      const fvType = v.fieldType;
      return matchFieldType(fvType, fieldType);
    });
  }
  
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
    discount: readFieldValue(item, 207, calc),
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
    discount: readFieldValue(item, 207, calc),
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
/**
 * Pobiera mnożnik rabatu (1 - discount). Jeśli discount nie jest zdefiniowany lub = 0, zwraca 1.
 * Discount może być:
 * - wartością 0-1 (np. 0.1 = 10%)
 * - wartością 0-100 (np. 10 = 10%) - automatycznie konwertowane
 */
const getDiscountMultiplier = (v: AllItemValues): number => {
  const discount = v.discount;
  if (discount === undefined || discount === null || discount === 0) return 1;
  
  // Jeśli discount > 1, traktuj jako procent (np. 10 = 10% = 0.1)
  const normalizedDiscount = discount > 1 ? discount / 100 : discount;
  
  // Ogranicz do zakresu 0-1
  const clampedDiscount = Math.max(0, Math.min(1, normalizedDiscount));
  
  return 1 - clampedDiscount;
};

const COMPUTE_PATHS: Record<number, ComputePath[]> = {
  // UnitPriceGross = netto × (1 + VAT) — rabat NIE wpływa na cenę jednostkową
  202: [
    { requires: ['unitPriceNet', 'vatRate'], compute: v => round2(v.unitPriceNet! * (1 + v.vatRate!)) },
  ],
  // ValueNet = netto × ilość × (1 - rabat)
  203: [
    { requires: ['unitPriceNet', 'quantity'], compute: v => round2(v.unitPriceNet! * v.quantity! * getDiscountMultiplier(v)) },
  ],
  // ValueGross = ValueNet + TotalVat (rabat już uwzględniony w ValueNet i TotalVat)
  204: [
    { requires: ['unitPriceNet', 'vatRate', 'quantity'], compute: v => round2(v.unitPriceNet! * (1 + v.vatRate!) * v.quantity! * getDiscountMultiplier(v)) },
    { requires: ['unitPriceGross', 'quantity'], compute: v => round2(v.unitPriceGross! * v.quantity! * getDiscountMultiplier(v)) },
    { requires: ['valueNet', 'totalVat'], compute: v => round2(v.valueNet! + v.totalVat!) },
    { requires: ['valueNet', 'vatRate'], compute: v => round2(v.valueNet! * (1 + v.vatRate!)) },
    { requires: ['valueNet'], compute: v => round2(v.valueNet!) },
  ],
  // UnitVat = netto × VAT — rabat NIE wpływa na VAT jednostkowy
  205: [
    { requires: ['unitPriceNet', 'vatRate'], compute: v => round2(v.unitPriceNet! * v.vatRate!) },
  ],
  // TotalVat = VAT_jedn × ilość × (1 - rabat)
  206: [
    { requires: ['unitPriceNet', 'vatRate', 'quantity'], compute: v => round2(v.unitPriceNet! * v.quantity! * v.vatRate! * getDiscountMultiplier(v)) },
    { requires: ['unitVat', 'quantity'], compute: v => round2(v.unitVat! * v.quantity! * getDiscountMultiplier(v)) },
    { requires: ['valueNet', 'vatRate'], compute: v => round2(v.valueNet! * v.vatRate!) },
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

    const def = calculatedFields.find((f: any) => {
      const defType = f.fieldType ?? f.fieldTypeConfig?.fieldType;
      return matchFieldType(defType, calcFieldType);
    });
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
 * Obsługuje zarówno nowy fieldType (200-299) jak i legacy (0-7).
 */
export const readOptionFieldValue = (
  optionFieldValues: any[],
  fieldType: number,
  childFieldDefs: any[]
): number | undefined => {
  // Najpierw próbuj znaleźć przez definicję pola
  const def = childFieldDefs.find((f: any) => {
    const defType = f.fieldType ?? f.fieldTypeConfig?.fieldType;
    return matchFieldType(defType, fieldType);
  });
  
  let fv: any = undefined;
  
  if (def) {
    fv = optionFieldValues.find((v: any) => v.fieldDefinitionId === def.id);
  }
  
  // Jeśli nie znaleziono przez definicję, szukaj bezpośrednio po fieldType
  if (!fv) {
    fv = optionFieldValues.find((v: any) => {
      const fvType = v.fieldType;
      return matchFieldType(fvType, fieldType);
    });
  }
  
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
    // discount usunięty
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

    const def = childFieldDefs.find((f: any) => {
      const defType = f.fieldType ?? f.fieldTypeConfig?.fieldType;
      return matchFieldType(defType, calcFieldType);
    });
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
