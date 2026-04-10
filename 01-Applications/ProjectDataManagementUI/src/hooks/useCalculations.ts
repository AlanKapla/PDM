import { useCallback, useMemo } from 'react';
import type {
  CostEstimateDataModel,
  CostEstimateWorkScope,
  CostEstimateCollectionItem,
  CalculatedFieldDefinition,
  GenericFieldDefinition,
  CostEstimateSummaryConfiguration,
} from '../types/costEstimate.types';
import {
  calculateWorkScope,
  calculateCollectionItem,
  recalculateEstimate,
} from '../utils/calculationEngine';
import type { CalculationContext } from '../utils/calculationEngine';

export interface UseCalculationsProps {
  calculatedFields: CalculatedFieldDefinition[];
  genericFields: GenericFieldDefinition[];
  /** Pola systemowe pozycji – wymagane do odczytu wartości Quantity */
  systemFields?: any[];
  summaryConfig?: CostEstimateSummaryConfiguration;
}

export interface UseCalculationsReturn {
  /**
   * Recalculate a single work scope after field value changes
   */
  recalculateWorkScope: (workScope: CostEstimateWorkScope) => CostEstimateWorkScope;

  /**
   * Recalculate a collection item after field value changes
   */
  recalculateCollectionItem: (
    item: CostEstimateCollectionItem,
    collectionFieldName: string
  ) => CostEstimateCollectionItem;

  /**
   * Recalculate entire cost estimate data model
   */
  recalculateAll: (dataModel: CostEstimateDataModel) => CostEstimateDataModel;

  /**
   * Get calculation context for manual calculations
   */
  calculationContext: CalculationContext;

  /**
   * Check if a field should be auto-calculated
   */
  isAutoCalculated: (fieldName: string) => boolean;

  /**
   * Check if a field is summable
   */
  isSummable: (fieldName: string) => boolean;
}

/**
 * Hook for managing calculations in cost estimates
 * Provides functions to recalculate derived values automatically
 */
export function useCalculations({
  calculatedFields,
  genericFields,
  systemFields,
  summaryConfig,
}: UseCalculationsProps): UseCalculationsReturn {
  // Memoize calculation context
  const calculationContext = useMemo<CalculationContext>(
    () => ({
      calculatedFields,
      genericFields,
      systemFields,
    }),
    [calculatedFields, genericFields, systemFields]
  );

  // Recalculate a single work scope
  const recalculateWorkScope = useCallback(
    (workScope: CostEstimateWorkScope): CostEstimateWorkScope => {
      return calculateWorkScope(workScope, calculationContext);
    },
    [calculationContext]
  );

  // Recalculate a collection item
  const recalculateCollectionItem = useCallback(
    (item: CostEstimateCollectionItem, collectionFieldName: string): CostEstimateCollectionItem => {
      // Find the collection field definition
      const collectionField = genericFields.find(f => f.name === collectionFieldName);
      
      if (!collectionField?.nestedFields) {
        return item;
      }

      return calculateCollectionItem(item, collectionField.nestedFields);
    },
    [genericFields]
  );

  // Recalculate entire data model
  const recalculateAll = useCallback(
    (dataModel: CostEstimateDataModel): CostEstimateDataModel => {
      return recalculateEstimate(
        dataModel,
        calculatedFields,
        genericFields,
        summaryConfig
      );
    },
    [calculatedFields, genericFields, summaryConfig]
  );

  // Check if field is auto-calculated
  const isAutoCalculated = useCallback(
    (fieldName: string): boolean => {
      const field = calculatedFields.find(f => f.name === fieldName);
      return field?.autoCalculated === true;
    },
    [calculatedFields]
  );

  // Check if field is summable
  const isSummable = useCallback(
    (fieldName: string): boolean => {
      const field = calculatedFields.find(f => f.name === fieldName);
      return field?.summable === true;
    },
    [calculatedFields]
  );

  return {
    recalculateWorkScope,
    recalculateCollectionItem,
    recalculateAll,
    calculationContext,
    isAutoCalculated,
    isSummable,
  };
}
