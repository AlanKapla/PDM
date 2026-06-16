/**
 * Hook do zarządzania wartościami pól dodatkowych dla konkretnej encji (grupy lub pozycji).
 *
 * Łączy useFieldAutosave z mechanizmem optimistic update dla pól dodatkowych.
 * Zapewnia debounce 700ms, optimistic update i obsługę błędów.
 */

import { useCallback, useRef } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useFieldAutosave } from './useFieldAutosave';
import { costEstimateKeys } from './queries/useCostEstimate';
import type {
  CostEstimateDetailsWeb,
  CostEstimateAdditionalFieldValueWeb,
  CostEstimateItemWeb,
  CostEstimateGroupWeb,
  AdditionalFieldType,
} from '../types/costEstimate.types.new';
import type { FieldValueType, FieldEntityType } from './useFieldAutosave';

export interface UseAdditionalFieldValuesParams {
  tenantId: string;
  projectId: string;
  costEstimateId: string;
  entityType: FieldEntityType;
  entityId: string;
}

export interface UseAdditionalFieldValuesReturn {
  /**
   * Zaplanuj zapis wartości pola dodatkowego (z debounce 700ms).
   * Automatycznie wykonuje optimistic update w cache React Query.
   */
  scheduleAdditionalFieldSave: (
    additionalFieldId: string,
    fieldType: AdditionalFieldType,
    value: string | undefined,
    currentFieldValueId?: string | null,
  ) => void;

  /** Wymuś natychmiastowy zapis wszystkich oczekujących zmian */
  flushPendingChanges: () => Promise<void>;

  /** Czy są oczekujące zmiany */
  hasPendingChanges: () => boolean;
}

/**
 * Mapuje AdditionalFieldType na FieldValueType używany w autosave
 */
function mapAdditionalFieldTypeToValueType(fieldType: AdditionalFieldType): FieldValueType {
  switch (fieldType) {
    case 1: // Decimal
      return 'numeric';
    case 2: // Boolean
      return 'boolean';
    case 3: // DateTime
      return 'date';
    case 0: // String
    default:
      return 'string';
  }
}

/**
 * Buduje zaktualizowaną listę wartości pól po zmianie jednego pola.
 * Jeśli pole już istnieje — aktualizuje, jeśli nie — dodaje nowe (optimistic).
 */
function buildUpdatedFieldValues(
  existing: CostEstimateAdditionalFieldValueWeb[],
  additionalFieldId: string,
  fieldType: AdditionalFieldType,
  value: string | undefined,
  savedId?: string | null,
): CostEstimateAdditionalFieldValueWeb[] {
  const existingIndex = existing.findIndex((fv) => fv.additionalFieldId === additionalFieldId);

  const updatedValue: CostEstimateAdditionalFieldValueWeb = {
    id: savedId ?? (existingIndex >= 0 ? existing[existingIndex].id : `optimistic_${additionalFieldId}`),
    additionalFieldId,
    stringValue: undefined,
    decimalValue: undefined,
    boolValue: undefined,
    dateTimeValue: undefined,
  };

  if (value !== undefined && value !== '') {
    switch (fieldType) {
      case 1: { // Decimal
        const parsed = parseFloat(value.replace(',', '.'));
        updatedValue.decimalValue = isNaN(parsed) ? undefined : parsed;
        break;
      }
      case 2: // Boolean
        updatedValue.boolValue = value === 'true' || value === '1';
        break;
      case 3: // DateTime
        updatedValue.dateTimeValue = value;
        break;
      case 0: // String
      default:
        updatedValue.stringValue = value;
        break;
    }
  }

  if (existingIndex >= 0) {
    const updated = [...existing];
    updated[existingIndex] = updatedValue;
    return updated;
  }
  return [...existing, updatedValue];
}

/**
 * Wykonuje optimistic update dla grupy w cache details
 */
function applyGroupOptimisticUpdate(
  details: CostEstimateDetailsWeb,
  groupId: string,
  additionalFieldId: string,
  fieldType: AdditionalFieldType,
  value: string | undefined,
): CostEstimateDetailsWeb {
  function updateGroup(group: CostEstimateGroupWeb): CostEstimateGroupWeb {
    if (group.id === groupId) {
      return {
        ...group,
        additionalFieldValues: buildUpdatedFieldValues(
          group.additionalFieldValues,
          additionalFieldId,
          fieldType,
          value,
        ),
      };
    }
    return {
      ...group,
      childGroups: group.childGroups.map(updateGroup),
    };
  }

  return {
    ...details,
    rootGroups: details.rootGroups.map(updateGroup),
  };
}

/**
 * Wykonuje optimistic update dla pozycji w cache details
 */
function applyItemOptimisticUpdate(
  details: CostEstimateDetailsWeb,
  itemId: string,
  additionalFieldId: string,
  fieldType: AdditionalFieldType,
  value: string | undefined,
): CostEstimateDetailsWeb {
  function updateItem(item: CostEstimateItemWeb): CostEstimateItemWeb {
    if (item.id === itemId) {
      return {
        ...item,
        additionalFieldValues: buildUpdatedFieldValues(
          item.additionalFieldValues,
          additionalFieldId,
          fieldType,
          value,
        ),
      };
    }
    const updatedOptions = item.options?.map(updateItem);
    const updatedComponents = item.components?.map(updateItem);
    if (updatedOptions === item.options && updatedComponents === item.components) {
      return item;
    }
    return { ...item, options: updatedOptions, components: updatedComponents };
  }

  return {
    ...details,
    rootGroups: details.rootGroups.map((group) => ({
      ...group,
      items: group.items.map(updateItem),
      childGroups: group.childGroups.map((childGroup) => ({
        ...childGroup,
        items: childGroup.items.map(updateItem),
      })),
    })),
  };
}

export function useAdditionalFieldValues(
  params: UseAdditionalFieldValuesParams,
): UseAdditionalFieldValuesReturn {
  const { tenantId, projectId, costEstimateId, entityType, entityId } = params;
  const queryClient = useQueryClient();
  const detailQueryKey = costEstimateKeys.detail(tenantId, projectId, costEstimateId);

  // Mapa: additionalFieldId -> AdditionalFieldType (dla optimistic update)
  // Stabilna referencja przez cały czas życia hooka
  const fieldTypeMapRef = useRef<Map<string, AdditionalFieldType>>(new Map());
  const fieldTypeMap = fieldTypeMapRef.current;

  const { scheduleFieldSave, flushPendingChanges, hasPendingChanges } = useFieldAutosave({
    params: { tenantId, projectId, costEstimateId },
    onSaveSuccess: (fieldInfo, savedFieldValueId) => {
      // Aktualizuj ID w cache po potwierdzeniu z serwera (jeśli nowe pole)
      if (savedFieldValueId && fieldInfo.additionalFieldId) {
        const currentDetails = queryClient.getQueryData<CostEstimateDetailsWeb>(detailQueryKey);
        if (currentDetails) {
          // Zamień optimistic ID na prawdziwy ID z API
          const updateId = (fv: CostEstimateAdditionalFieldValueWeb): CostEstimateAdditionalFieldValueWeb => {
            if (
              fv.additionalFieldId === fieldInfo.additionalFieldId &&
              fv.id.startsWith('optimistic_')
            ) {
              return { ...fv, id: savedFieldValueId };
            }
            return fv;
          };

          function updateGroupIds(group: CostEstimateGroupWeb): CostEstimateGroupWeb {
            return {
              ...group,
              additionalFieldValues: group.additionalFieldValues.map(updateId),
              childGroups: group.childGroups.map(updateGroupIds),
            };
          }

          function updateItemIds(item: CostEstimateItemWeb): CostEstimateItemWeb {
            return {
              ...item,
              additionalFieldValues: item.additionalFieldValues.map(updateId),
              options: item.options?.map(updateItemIds),
              components: item.components?.map(updateItemIds),
            };
          }

          const updatedDetails: CostEstimateDetailsWeb = {
            ...currentDetails,
            rootGroups: currentDetails.rootGroups.map((group) => ({
              ...updateGroupIds(group),
              items: group.items.map(updateItemIds),
            })),
          };
          queryClient.setQueryData<CostEstimateDetailsWeb>(detailQueryKey, updatedDetails);
        }
      }
    },
    onSaveError: () => {
      // Przy błędzie odśwież dane z serwera
      void queryClient.invalidateQueries({ queryKey: detailQueryKey });
    },
  });

  const scheduleAdditionalFieldSave = useCallback(
    (
      additionalFieldId: string,
      fieldType: AdditionalFieldType,
      value: string | undefined,
      currentFieldValueId?: string | null,
    ): void => {
      // Zapamiętaj typ pola dla optimistic update
      fieldTypeMap.set(additionalFieldId, fieldType);

      // Optimistic update w cache
      const currentDetails = queryClient.getQueryData<CostEstimateDetailsWeb>(detailQueryKey);
      if (currentDetails) {
        let updated: CostEstimateDetailsWeb;
        if (entityType === 'group') {
          updated = applyGroupOptimisticUpdate(
            currentDetails,
            entityId,
            additionalFieldId,
            fieldType,
            value,
          );
        } else {
          updated = applyItemOptimisticUpdate(
            currentDetails,
            entityId,
            additionalFieldId,
            fieldType,
            value,
          );
        }
        queryClient.setQueryData<CostEstimateDetailsWeb>(detailQueryKey, updated);
      }

      // Zaplanuj zapis z debounce
      scheduleFieldSave(
        {
          entityType,
          entityId,
          fieldType: 'additional',
          name: additionalFieldId,
          additionalFieldId,
          fieldValueId: currentFieldValueId ?? null,
          valueType: mapAdditionalFieldTypeToValueType(fieldType),
        },
        value,
      );
    },
    [entityType, entityId, queryClient, detailQueryKey, scheduleFieldSave, fieldTypeMap],
  );

  return {
    scheduleAdditionalFieldSave,
    flushPendingChanges,
    hasPendingChanges,
  };
}
