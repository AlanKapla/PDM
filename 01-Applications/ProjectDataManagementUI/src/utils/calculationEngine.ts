import { CalculatedFieldType } from '../types/costEstimate.types';
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

export interface CalculationContext {
  calculatedFields: CalculatedFieldDefinition[];
  genericFields?: any[];
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

  // Get field definitions that should be auto-calculated
  const autoCalcFields = context.calculatedFields
    .filter(f => f.autoCalculated && !lockedFieldNames.has(f.name))
    .sort((a, b) => a.type - b.type);

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
  const values = { ...(calculated.calculatedFieldValues || {}) };

  const autoCalcFields = nestedFieldsDef.calculatedFields.filter(f => f.autoCalculated);

  for (const field of autoCalcFields) {
    const calculatedValue = calculateFieldValue(field.type, values);
    if (calculatedValue !== null && calculatedValue !== undefined) {
      values[field.name] = calculatedValue;
    }
  }

  calculated.calculatedFieldValues = values;
  return calculated;
}

/**
 * Check if a field can be auto-calculated based on available source values
 * @param fieldType - Type of the field to check
 * @param values - Available values mapped by field type
 * @returns true if all required source values are available for calculation
 */
export function canAutoCalculate(
  fieldType: CalculatedFieldType,
  values: Record<number, any>
): boolean {
  const hasValue = (type: CalculatedFieldType): boolean => {
    const val = values[type];
    return val !== null && val !== undefined && val !== '' && !isNaN(Number(val));
  };

  switch (fieldType) {
    case CalculatedFieldType.UnitPriceGross:
      // Wymaga UnitPriceNet i VatRate
      return hasValue(CalculatedFieldType.UnitPriceNet) && hasValue(CalculatedFieldType.VatRate);

    case CalculatedFieldType.ValueNet:
      // Wymaga UnitPriceNet i Quantity
      return hasValue(CalculatedFieldType.UnitPriceNet) && hasValue(CalculatedFieldType.Quantity);

    case CalculatedFieldType.ValueGross:
      // Wymaga UnitPriceGross i Quantity
      return hasValue(CalculatedFieldType.UnitPriceGross) && hasValue(CalculatedFieldType.Quantity);

    case CalculatedFieldType.UnitVat:
      // UnitVat = UnitPriceNet * (VatRate / 100) LUB UnitPriceGross - UnitPriceNet
      return (hasValue(CalculatedFieldType.UnitPriceNet) && hasValue(CalculatedFieldType.VatRate)) ||
             (hasValue(CalculatedFieldType.UnitPriceGross) && hasValue(CalculatedFieldType.UnitPriceNet));

    case CalculatedFieldType.TotalVat:
      // TotalVat = ValueNet * (VatRate / 100) LUB UnitVat * Quantity LUB ValueGross - ValueNet
      return (hasValue(CalculatedFieldType.ValueNet) && hasValue(CalculatedFieldType.VatRate)) ||
             (hasValue(CalculatedFieldType.UnitVat) && hasValue(CalculatedFieldType.Quantity)) ||
             (hasValue(CalculatedFieldType.ValueGross) && hasValue(CalculatedFieldType.ValueNet));

    default:
      // UnitPriceNet, VatRate, Quantity są polami wejściowymi
      return false;
  }
}

/**
 * Calculate a single field value based on its type and existing values
 * values - mapa gdzie klucze to field.type (wartości enum CalculatedFieldType)
 */
export function calculateFieldValue(
  fieldType: CalculatedFieldType,
  values: Record<number, any>
): number | null {
  // Helper to get numeric value by field type (enum value)
  const getNum = (type: CalculatedFieldType): number => {
    const val = values[type];
    return typeof val === 'number' ? val : parseFloat(val) || 0;
  };

  switch (fieldType) {
    case CalculatedFieldType.UnitPriceGross: {
      // UnitPriceGross = UnitPriceNet * (1 + VatRate/100)
      const unitPriceNet = getNum(CalculatedFieldType.UnitPriceNet);
      const vatRate = getNum(CalculatedFieldType.VatRate);
      return unitPriceNet * (1 + vatRate / 100);
    }

    case CalculatedFieldType.ValueNet: {
      // ValueNet = UnitPriceNet * Quantity
      const unitPriceNet = getNum(CalculatedFieldType.UnitPriceNet);
      const quantity = getNum(CalculatedFieldType.Quantity);
      return unitPriceNet * quantity;
    }

    case CalculatedFieldType.ValueGross: {
      // ValueGross = UnitPriceGross * Quantity
      const unitPriceGross = getNum(CalculatedFieldType.UnitPriceGross);
      const quantity = getNum(CalculatedFieldType.Quantity);
      return unitPriceGross * quantity;
    }

    case CalculatedFieldType.UnitVat: {
      // UnitVat = UnitPriceNet * (VatRate / 100)
      // lub UnitVat = UnitPriceGross - UnitPriceNet
      const unitPriceNet = getNum(CalculatedFieldType.UnitPriceNet);
      const vatRate = getNum(CalculatedFieldType.VatRate);
      const unitPriceGross = getNum(CalculatedFieldType.UnitPriceGross);
      
      // Preferuj obliczenie z UnitPriceNet i VatRate
      if (unitPriceNet > 0 && vatRate > 0) {
        return unitPriceNet * (vatRate / 100);
      }
      // Alternatywnie: UnitPriceGross - UnitPriceNet
      if (unitPriceGross > 0 && unitPriceNet > 0) {
        return unitPriceGross - unitPriceNet;
      }
      return 0;
    }

    case CalculatedFieldType.TotalVat: {
      // TotalVat = ValueNet * (VatRate / 100)
      // lub TotalVat = UnitVat * Quantity
      // lub TotalVat = ValueGross - ValueNet
      const valueNet = getNum(CalculatedFieldType.ValueNet);
      const vatRate = getNum(CalculatedFieldType.VatRate);
      const unitVat = getNum(CalculatedFieldType.UnitVat);
      const quantity = getNum(CalculatedFieldType.Quantity);
      const valueGross = getNum(CalculatedFieldType.ValueGross);
      
      // Preferuj obliczenie z ValueNet i VatRate
      if (valueNet > 0 && vatRate > 0) {
        return valueNet * (vatRate / 100);
      }
      // Alternatywnie: UnitVat * Quantity
      if (unitVat > 0 && quantity > 0) {
        return unitVat * quantity;
      }
      // Alternatywnie: ValueGross - ValueNet
      if (valueGross > 0 && valueNet > 0) {
        return valueGross - valueNet;
      }
      return 0;
    }

    default:
      // UnitPriceNet, VatRate, Quantity are input fields, not calculated
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
    groupSummaryFields: string[];
    totalSummaryFields: string[];
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
            
            nestedCalcFields.forEach(nestedField => {
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
    if (summaryConfig?.showGroupSummary && summaryConfig.groupSummaryFields.length > 0) {
      processed.groupTotals = calculateGroupTotals(
        processed,
        summaryConfig.groupSummaryFields
      );
    }

    return processed;
  };

  // Process all groups
  const processedGroups = dataModel.groups.map(g => processGroup(g));

  // Calculate overall totals - TYLKO jeśli są wybrane pola do sumowania
  let overallTotals: Record<string, number> | undefined;
  if (summaryConfig?.showTotalSummary && summaryConfig.totalSummaryFields.length > 0) {
    overallTotals = calculateOverallTotals(
      processedGroups,
      summaryConfig.totalSummaryFields
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
