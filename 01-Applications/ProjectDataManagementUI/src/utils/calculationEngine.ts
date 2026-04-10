import { CalculatedFieldType, SystemFieldType } from '../types/costEstimate.types';
import type {
  CostEstimateDataModel,
  CostEstimateGroup,
  CostEstimateWorkScope,
  CostEstimateCollectionItem,
  CalculatedFieldDefinition,
  GenericFieldCollectionDefinition,
} from '../types/costEstimate.types';

/**
 * Calculation Engine for Cost Estimates
 * Automatically calculates derived field values based on formulas
 */

/**
 * Klucz dla wartości Quantity w mapie wartości kalkulacji.
 * Używamy wartości ujemnej, żeby nie kolidować z wartościami CalculatedFieldType.
 * Quantity to pole systemowe (FieldType.ItemSystemQuantity = 101), ale potrzebujemy go w kalkulacjach.
 */
const QUANTITY_KEY = -1;

/**
 * Kolejność obliczania pól: NET → VAT → GROSS (zawsze).
 * Pola źródłowe (UnitPriceNet, Quantity, VatRate) nie są tu obecne – nigdy nie obliczamy ich automatycznie.
 */
const CALCULATION_ORDER: CalculatedFieldType[] = [
  CalculatedFieldType.ValueNet,        // Net Price × Quantity
  CalculatedFieldType.TotalVat,        // Net Value × VAT Rate
  CalculatedFieldType.ValueGross,      // Net Value + VAT Value
  CalculatedFieldType.UnitPriceGross,  // Gross Value / Quantity (lub inne metody)
  CalculatedFieldType.UnitVat,         // Net Price × VAT Rate
];

export interface CalculationContext {
  calculatedFields: CalculatedFieldDefinition[];
  genericFields?: any[];
  /** Pola systemowe pozycji – wymagane do odczytu wartości Quantity (FieldType 101) */
  systemFields?: any[];
}

/**
 * Calculate all calculated fields in a work scope
 */
export function calculateWorkScope(
  workScope: CostEstimateWorkScope,
  context: CalculationContext
): CostEstimateWorkScope {
  const calculated = { ...workScope };
  
  // Zachowaj lockedFields podczas całego procesu kalkulacji
  const lockedFieldNames = new Set(workScope.lockedFields || []);
  
  // Scalamy wszystkie wartości z calculatedFieldValues i genericFieldValues
  const allValues = {
    ...calculated.genericFieldValues,
    ...calculated.calculatedFieldValues,
  };

  // Tworzymy mapę: type → field.name dla szybkiego dostępu
  const typeToName = new Map<number, string>();
  context.calculatedFields.forEach(f => typeToName.set(f.type, f.name));

  // Tworzymy mapę wartości indeksowaną po typie (enum), nie po field.name
  const valuesByType: Record<number, number> = {};
  context.calculatedFields.forEach(field => {
    if (field.name in allValues) {
      valuesByType[field.type] = allValues[field.name];
    }
  });

  // Dodaj wartość Quantity z pól systemowych (FieldType.ItemSystemQuantity = 101)
  if (context.systemFields) {
    const quantityField = context.systemFields.find((f: any) => f.type === 101 || f.fieldType === 101);
    const quantityName: string | undefined = quantityField?.name ?? quantityField?.fieldName;
    if (quantityName && quantityName in allValues) {
      valuesByType[QUANTITY_KEY] = parseFloat(allValues[quantityName]) || 0;
    }
  }

  // Get field definitions that should be auto-calculated – w kolejności NET → VAT → GROSS
  const autoCalcFields = context.calculatedFields
    .filter(f => f.autoCalculated && !lockedFieldNames.has(f.name))
    .sort((a, b) => {
      const orderA = CALCULATION_ORDER.indexOf(a.type);
      const orderB = CALCULATION_ORDER.indexOf(b.type);
      return (orderA === -1 ? 999 : orderA) - (orderB === -1 ? 999 : orderB);
    });

  // Calculate each auto-calculated field in order
  for (const field of autoCalcFields) {
    const calculatedValue = calculateFieldValue(field.type, valuesByType);
    
    if (calculatedValue !== null && calculatedValue !== undefined) {
      valuesByType[field.type] = calculatedValue;
      allValues[field.name] = calculatedValue;
    }
  }

  // Zapisujemy tylko obliczone pola do calculatedFieldValues
  // Zachowaj oryginalne wartości dla pól zablokowanych (lockedFields)
  calculated.calculatedFieldValues = { ...workScope.calculatedFieldValues };
  
  for (const field of context.calculatedFields) {
    // Nie nadpisuj wartości pól zablokowanych
    if (field.name in allValues && !lockedFieldNames.has(field.name)) {
      calculated.calculatedFieldValues[field.name] = allValues[field.name];
    }
  }

  // Calculate collection fields
  if (calculated.collectionFieldValues && context.genericFields) {
    const collectionFields = context.genericFields.filter((f: any) => f.type === 10); // Collection type
    
    for (const collectionField of collectionFields) {
      const items = calculated.collectionFieldValues[collectionField.name];
      if (items && collectionField.nestedFields) {
        calculated.collectionFieldValues[collectionField.name] = items.map(item => {
          const recalculated = calculateCollectionItem(item, collectionField.nestedFields);
          // MUST: Zachowaj isSelected i inne meta pola z oryginału
          return { ...recalculated, isSelected: item.isSelected };
        });
      }
    }
  }

  return calculated;
}

/**
 * Calculate calculated fields in a collection item
 */
export function calculateCollectionItem(
  item: CostEstimateCollectionItem,
  nestedFieldsDef: GenericFieldCollectionDefinition
): CostEstimateCollectionItem {
  if (!nestedFieldsDef.calculatedFields || nestedFieldsDef.calculatedFields.length === 0) {
    return item;
  }

  const calculated = { ...item };
  const allValues = { ...(calculated.calculatedFieldValues || {}) };

  // Budujemy mapę wartości indeksowaną po typie (enum) – tak jak w calculateWorkScope
  const valuesByType: Record<number, number> = {};
  nestedFieldsDef.calculatedFields.forEach(field => {
    if (field.name in allValues) {
      valuesByType[field.type] = parseFloat(allValues[field.name]) || 0;
    }
  });

  const autoCalcFields = nestedFieldsDef.calculatedFields
    .filter(f => f.autoCalculated)
    .sort((a, b) => {
      const orderA = CALCULATION_ORDER.indexOf(a.type);
      const orderB = CALCULATION_ORDER.indexOf(b.type);
      return (orderA === -1 ? 999 : orderA) - (orderB === -1 ? 999 : orderB);
    });

  for (const field of autoCalcFields) {
    const calculatedValue = calculateFieldValue(field.type, valuesByType);
    if (calculatedValue !== null && calculatedValue !== undefined) {
      valuesByType[field.type] = calculatedValue;
      allValues[field.name] = calculatedValue;
    }
  }

  // Zapisz obliczone wartości z powrotem do calculated field values
  const newCalculatedFieldValues = { ...(calculated.calculatedFieldValues || {}) };
  for (const field of nestedFieldsDef.calculatedFields) {
    if (field.name in allValues) {
      newCalculatedFieldValues[field.name] = allValues[field.name];
    }
  }

  calculated.calculatedFieldValues = newCalculatedFieldValues;
  return calculated;
}

/**
 * Sprawdza czy pole może być automatycznie obliczone na podstawie dostępnych wartości wejściowych.
 * Pole jest blokowane do edycji tylko gdy może być obliczone automatycznie.
 * @param fieldType - Typ pola do sprawdzenia
 * @param values - Dostępne wartości, klucze = CalculatedFieldType (enum)
 */
export function canAutoCalculate(
  fieldType: CalculatedFieldType,
  values: Record<number, any>
): boolean {
  const hasValue = (fieldKey: number): boolean => {
    const val = values[fieldKey];
    return val !== null && val !== undefined && val !== '' && !isNaN(Number(val));
  };

  switch (fieldType) {
    case CalculatedFieldType.ValueNet:
      // Net Price × Quantity
      return hasValue(CalculatedFieldType.UnitPriceNet) && hasValue(QUANTITY_KEY);

    case CalculatedFieldType.TotalVat:
      // Net Value × VAT Rate
      return hasValue(CalculatedFieldType.ValueNet) && hasValue(CalculatedFieldType.VatRate);

    case CalculatedFieldType.ValueGross:
      // Net Value + VAT Value  LUB  Net Value × (1 + VAT Rate)
      return hasValue(CalculatedFieldType.ValueNet) &&
             (hasValue(CalculatedFieldType.TotalVat) || hasValue(CalculatedFieldType.VatRate));

    case CalculatedFieldType.UnitPriceGross:
      // Net Price × (1 + VAT Rate)  LUB  Net Price + VAT Value/Quantity  LUB  Gross Value / Quantity
      return (hasValue(CalculatedFieldType.UnitPriceNet) && hasValue(CalculatedFieldType.VatRate)) ||
             (hasValue(CalculatedFieldType.UnitPriceNet) && hasValue(CalculatedFieldType.TotalVat) && hasValue(QUANTITY_KEY)) ||
             (hasValue(CalculatedFieldType.ValueGross) && hasValue(QUANTITY_KEY));

    case CalculatedFieldType.UnitVat:
      // Net Price × VAT Rate
      return hasValue(CalculatedFieldType.UnitPriceNet) && hasValue(CalculatedFieldType.VatRate);

    default:
      // UnitPriceNet, Quantity, VatRate – pola źródłowe, nigdy nie obliczane
      return false;
  }
}

/**
 * Oblicza wartość pojedynczego pola na podstawie jego typu i dostępnych wartości.
 * Kierunek obliczeń zawsze: NET → VAT → GROSS.
 * @param values - mapa gdzie klucze to field.type (wartości enum CalculatedFieldType)
 */
export function calculateFieldValue(
  fieldType: CalculatedFieldType,
  values: Record<number, any>
): number | null {
  const getNum = (type: number): number => {
    const val = values[type];
    return typeof val === 'number' ? val : parseFloat(val) || 0;
  };

  const hasVal = (type: number): boolean => {
    const val = values[type];
    return val !== null && val !== undefined && val !== '' && !isNaN(Number(val));
  };

  switch (fieldType) {
    case CalculatedFieldType.ValueNet: {
      // Net Price × Quantity
      const unitPriceNet = getNum(CalculatedFieldType.UnitPriceNet);
      const quantity = getNum(QUANTITY_KEY);
      return unitPriceNet * quantity;
    }

    case CalculatedFieldType.TotalVat: {
      // Net Value × (VAT Rate / 100)
      const valueNet = getNum(CalculatedFieldType.ValueNet);
      const vatRate = getNum(CalculatedFieldType.VatRate);
      return valueNet * (vatRate / 100);
    }

    case CalculatedFieldType.ValueGross: {
      const valueNet = getNum(CalculatedFieldType.ValueNet);
      // Priorytet 1: Net Value + VAT Value (konkretna kwota podatku)
      if (valueNet > 0 && hasVal(CalculatedFieldType.TotalVat)) {
        return valueNet + getNum(CalculatedFieldType.TotalVat);
      }
      // Priorytet 2: Net Value × (1 + VAT Rate / 100)
      if (valueNet > 0 && hasVal(CalculatedFieldType.VatRate)) {
        return valueNet * (1 + getNum(CalculatedFieldType.VatRate) / 100);
      }
      // Brak danych — nie obliczamy, pole pozostaje edytowalne
      return null;
    }

    case CalculatedFieldType.UnitPriceGross: {
      const unitPriceNet = getNum(CalculatedFieldType.UnitPriceNet);
      const quantity = getNum(QUANTITY_KEY);
      // Priorytet 1: Net Price + (VAT Value / Quantity)
      if (unitPriceNet > 0 && hasVal(CalculatedFieldType.TotalVat) && quantity > 0) {
        return unitPriceNet + (getNum(CalculatedFieldType.TotalVat) / quantity);
      }
      // Priorytet 2: Gross Value / Quantity
      const valueGross = getNum(CalculatedFieldType.ValueGross);
      if (valueGross > 0 && quantity > 0) {
        return valueGross / quantity;
      }
      return null;
    }

    case CalculatedFieldType.UnitVat: {
      // Net Price × (VAT Rate / 100)
      const unitPriceNet = getNum(CalculatedFieldType.UnitPriceNet);
      const vatRate = getNum(CalculatedFieldType.VatRate);
      if (unitPriceNet > 0 && hasVal(CalculatedFieldType.VatRate)) {
        return unitPriceNet * (vatRate / 100);
      }
      return null;
    }

    default:
      // UnitPriceNet, Quantity, VatRate – pola źródłowe, nie obliczane
      return null;
  }
}

/**
 * Calculate group totals (sum of work scopes)
 */
export function calculateGroupTotals(
  group: CostEstimateGroup,
  summableFields: string[]
): Record<string, number> {
  const totals: Record<string, number> = {};

  // Initialize totals
  for (const fieldName of summableFields) {
    totals[fieldName] = 0;
  }

  // Sum work scopes
  for (const workScope of group.workScopes) {
    for (const fieldName of summableFields) {
      const value = workScope.calculatedFieldValues[fieldName];
      if (typeof value === 'number') {
        totals[fieldName] += value;
      }
    }
  }

  // Sum sub-groups recursively
  if (group.subGroups) {
    for (const subGroup of group.subGroups) {
      if (subGroup.groupTotals) {
        for (const fieldName of summableFields) {
          if (typeof subGroup.groupTotals[fieldName] === 'number') {
            totals[fieldName] += subGroup.groupTotals[fieldName];
          }
        }
      }
    }
  }

  return totals;
}

/**
 * Calculate overall totals for entire cost estimate
 */
export function calculateOverallTotals(
  groups: CostEstimateGroup[],
  summableFields: string[]
): Record<string, number> {
  const totals: Record<string, number> = {};

  // Initialize totals
  for (const fieldName of summableFields) {
    totals[fieldName] = 0;
  }

  // Sum all top-level groups
  for (const group of groups) {
    if (group.groupTotals) {
      for (const fieldName of summableFields) {
        if (typeof group.groupTotals[fieldName] === 'number') {
          totals[fieldName] += group.groupTotals[fieldName];
        }
      }
    }
  }

  return totals;
}

/**
 * Recalculate entire cost estimate data model
 */
export function recalculateEstimate(
  dataModel: CostEstimateDataModel,
  calculatedFields: CalculatedFieldDefinition[],
  genericFields: any[],
  summaryConfig?: {
    showGroupSummary: boolean;
    showTotalSummary: boolean;
    groupSummaryFields?: string[] | any[];
    totalSummaryFields?: string[] | any[];
  }
): CostEstimateDataModel {
  const context: CalculationContext = {
    calculatedFields,
    genericFields,
  };

  // Recursive function to process groups
  const processGroup = (group: CostEstimateGroup): CostEstimateGroup => {
    const processed = { ...group };

    // Calculate all work scopes
    processed.workScopes = group.workScopes.map(ws => {
      // CRITICAL: Skopiuj wartości z zaznaczonego collection item PRZED przeliczaniem
      let preparedWs = { ...ws };
      
      // Znajdź zaznaczony item w każdej kolekcji
      if (ws.collectionFieldValues) {
        Object.entries(ws.collectionFieldValues).forEach(([fieldName, items]) => {
          const selectedItem = items?.find(item => item.isSelected);
          
          if (selectedItem) {
            // Znajdź definicję pola kolekcji
            const collectionField = genericFields.find(f => f.name === fieldName);
            const nestedCalcFields = collectionField?.nestedFields?.calculatedFields || [];
            
            // Skopiuj wartości z itemu do workScope
            const updatedValues = { ...preparedWs.calculatedFieldValues };
            const lockedFields = preparedWs.lockedFields ? [...preparedWs.lockedFields] : [];
            
            nestedCalcFields.forEach((nestedField: CalculatedFieldDefinition) => {
              const nestedValue = selectedItem.calculatedFieldValues?.[nestedField.name];
              const mainField = calculatedFields.find(f => f.type === nestedField.type);
              
              if (mainField && nestedValue !== undefined) {
                updatedValues[mainField.name] = nestedValue;
                if (!lockedFields.includes(mainField.name)) {
                  lockedFields.push(mainField.name);
                }
              }
            });
            
            preparedWs = {
              ...preparedWs,
              calculatedFieldValues: updatedValues,
              lockedFields: lockedFields.length > 0 ? lockedFields : undefined
            };
          }
        });
      }
      
      // Teraz przelicz z już skopiowanymi wartościami i lockedFields
      return calculateWorkScope(preparedWs, context);
    });

    // Process sub-groups recursively
    if (group.subGroups) {
      processed.subGroups = group.subGroups.map(sg => processGroup(sg));
    }

    // Calculate group totals - TYLKO jeśli są wybrane pola do sumowania
    const groupFields = summaryConfig?.groupSummaryFields ?? [];
    if (summaryConfig?.showGroupSummary && groupFields.length > 0) {
      // Konwertuj SummaryFieldWeb[] na string[] jeśli potrzeba
      const fieldNames = groupFields.map((f: any) => typeof f === 'string' ? f : f.fieldName);
      processed.groupTotals = calculateGroupTotals(
        processed,
        fieldNames
      );
    }

    return processed;
  };

  // Process all groups
  const processedGroups = dataModel.groups.map(g => processGroup(g));

  // Calculate overall totals - TYLKO jeśli są wybrane pola do sumowania
  let overallTotals: Record<string, number> | undefined;
  const totalFields = summaryConfig?.totalSummaryFields ?? [];
  if (summaryConfig?.showTotalSummary && totalFields.length > 0) {
    // Konwertuj SummaryFieldWeb[] na string[] jeśli potrzeba
    const fieldNames = totalFields.map((f: any) => typeof f === 'string' ? f : f.fieldName);
    overallTotals = calculateOverallTotals(
      processedGroups,
      fieldNames
    );
  }

  return {
    ...dataModel,
    groups: processedGroups,
    totals: overallTotals,
    metadata: {
      ...dataModel.metadata,
      lastModified: new Date().toISOString(),
      schemaVersion: dataModel.metadata?.schemaVersion || 1,
    },
  };
}

/**
 * Helper to format calculated values for display
 */
export function formatCalculatedValue(
  value: number | null | undefined,
  displayFormat?: string,
  unit?: string
): string {
  if (value === null || value === undefined || isNaN(value)) {
    return '-';
  }

  let formatted: string;

  if (displayFormat) {
    // Apply custom format (e.g., "0.00", "0,000.00")
    const decimalPlaces = (displayFormat.match(/\./)?.[0]?.length || 2) - 1;
    formatted = value.toFixed(Math.max(0, decimalPlaces));
  } else {
    formatted = value.toFixed(2);
  }

  // Add unit if provided
  if (unit) {
    formatted += ` ${unit}`;
  }

  return formatted;
}
