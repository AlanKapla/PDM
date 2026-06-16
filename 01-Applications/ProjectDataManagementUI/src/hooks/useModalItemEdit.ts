/**
 * Stub hooka useModalItemEdit — do edycji pozycji w modalach mobilnych.
 * @deprecated Ten hook jest częścią deprecated mobile view.
 */

import { useState, useCallback } from 'react';
import type { CostEstimateItemWeb } from '../types/costEstimate.types.new';
import type { AllItemValues } from '../utils/costEstimateCalculations';
import { getAllValues } from '../utils/costEstimateCalculations';
import type { FieldSource } from '../components/CostEstimate/costEstimateTableTypes';

interface UseModalItemEditParams {
  item: CostEstimateItemWeb;
  templateStructure: unknown;
  onSaveField: (fieldId: string, fieldSource: FieldSource, value: string | undefined) => void;
}

interface UseModalItemEditResult {
  virtualItem: CostEstimateItemWeb;
  allValues: AllItemValues;
  orderedFields: unknown[];
  handleFieldChange: (fieldId: string, fieldDef: unknown, value: string | undefined) => void;
}

export function useModalItemEdit({
  item,
  templateStructure,
  onSaveField,
}: UseModalItemEditParams): UseModalItemEditResult {
  const [virtualItem, setVirtualItem] = useState<CostEstimateItemWeb>(item);

  const allValues = getAllValues(virtualItem);

  // Flatten all fields from templateStructure (legacy support)
  const orderedFields: unknown[] = (() => {
    if (!templateStructure || typeof templateStructure !== 'object') return [];
    const ts = templateStructure as Record<string, unknown>;
    const system = Array.isArray(ts['systemFields']) ? (ts['systemFields'] as unknown[]) : [];
    const calculated = Array.isArray(ts['calculatedFields']) ? (ts['calculatedFields'] as unknown[]) : [];
    const generic = Array.isArray(ts['genericFields']) ? (ts['genericFields'] as unknown[]) : [];
    return [...system, ...calculated, ...generic];
  })();

  const handleFieldChange = useCallback(
    (fieldId: string, _fieldDef: unknown, value: string | undefined) => {
      // Determine field source from templateStructure
      let fieldSource: FieldSource = 'system';
      if (templateStructure && typeof templateStructure === 'object') {
        const ts = templateStructure as Record<string, unknown>;
        const calculated = Array.isArray(ts['calculatedFields']) ? (ts['calculatedFields'] as Array<{ id?: string }>) : [];
        const generic = Array.isArray(ts['genericFields']) ? (ts['genericFields'] as Array<{ id?: string }>) : [];
        if (calculated.some((f) => f.id === fieldId)) fieldSource = 'calculated';
        else if (generic.some((f) => f.id === fieldId)) fieldSource = 'generic';
      }

      onSaveField(fieldId, fieldSource, value);

      // Optimistic update on virtual item's fieldValues (legacy)
      setVirtualItem((prev) => {
        const existingFv = (prev.fieldValues ?? []).find((fv) => fv.fieldDefinitionId === fieldId);
        if (existingFv) {
          return {
            ...prev,
            fieldValues: (prev.fieldValues ?? []).map((fv) =>
              fv.fieldDefinitionId === fieldId ? { ...fv, stringValue: value } : fv
            ),
          };
        }
        return prev;
      });
    },
    [onSaveField, templateStructure]
  );

  return { virtualItem, allValues, orderedFields, handleFieldChange };
}
