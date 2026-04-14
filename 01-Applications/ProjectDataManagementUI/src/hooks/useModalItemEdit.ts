import { useState, useRef, useCallback, useMemo } from 'react';
import type { CostEstimateItemWeb, CostEstimateFieldValueWeb } from '../types/costEstimate.types.new';
import type { FieldSource } from '../components/CostEstimate/costEstimateTableTypes';
import {
  getAllValues,
  recalculateItem,
  recalculateOption,
  isSourceFieldType,
  isCalculatedFieldType,
  normalizeFieldType,
  type AllItemValues,
} from '../utils/costEstimateCalculations';
import {
  readItemFieldValue,
  getOrderedFields,
  getFieldSourceForDef,
} from '../components/CostEstimate/mobile/MobileFieldInput';

export interface UseModalItemEditOptions {
  item: CostEstimateItemWeb;
  templateStructure: any;
  /**
   * Callback wywoływany dla każdego pola które zmieniło wartość (bezpośrednio lub kaskadowo).
   * Odpowiada za autozapis — implementacja identyczna jak updateItemFieldValue w CostEstimateTableView.
   */
  onSaveField: (fieldId: string, fieldSource: FieldSource, value: string | undefined) => void;
}

export interface UseModalItemEditReturn {
  virtualItem: CostEstimateItemWeb;
  allValues: AllItemValues;
  orderedFields: any[];
  handleFieldChange: (fieldId: string, fieldDef: any, newValue: string | undefined) => void;
}

/**
 * Hook wspólny dla ItemEditModal i ComponentEditModal.
 *
 * Kolejność operacji przy zmianie pola (spójna z CostEstimateTableView.updateItemFieldValue):
 *   1. Zaktualizuj fieldValues wirtualnej kopii
 *   2. Przelicz pola kalkulowane (recalculateItem)
 *   3. Gdy zmieniono ilość (fieldType 101) → przelicz opcje/warianty (recalculateOption)
 *   4. Autozapis każdego pola które zmieniło wartość względem stanu sprzed zmiany
 */
export function useModalItemEdit({
  item,
  templateStructure,
  onSaveField,
}: UseModalItemEditOptions): UseModalItemEditReturn {
  const [virtualItem, setVirtualItem] = useState<CostEstimateItemWeb>(() => ({
    ...item,
    fieldValues: [...item.fieldValues],
  }));
  const virtualItemRef = useRef(virtualItem);
  virtualItemRef.current = virtualItem;

  const allSaveable = useMemo(
    () => [
      ...(templateStructure?.systemFields ?? []),
      ...(templateStructure?.calculatedFields ?? []),
      ...(templateStructure?.genericFields ?? []),
    ],
    [templateStructure]
  );

  const handleFieldChange = useCallback(
    (fieldId: string, fieldDef: any, newValue: string | undefined) => {
      const prev = virtualItemRef.current;
      const fieldValues = [...prev.fieldValues];
      const idx = fieldValues.findIndex((fv) => fv.fieldDefinitionId === fieldId);
      const fieldType = fieldDef?.fieldType ?? fieldDef?.fieldTypeConfig?.fieldType ?? 0;
      const fieldScope = fieldDef?.fieldScope ?? fieldDef?.fieldTypeConfig?.fieldScope ?? 0;
      const isBoolean = fieldDef?.fieldTypeConfig?.isBoolean === true;

      const updatedFv: CostEstimateFieldValueWeb = {
        id: idx !== -1 ? fieldValues[idx].id : `temp_${fieldId}_${Date.now()}`,
        fieldDefinitionId: fieldId,
        fieldType,
        fieldScope,
        fieldName: fieldDef?.fieldName,
        fieldLabel: fieldDef?.label,
        stringValue: newValue,
        decimalValue:
          !isBoolean && newValue !== undefined && newValue !== ''
            ? parseFloat(newValue) || undefined
            : undefined,
        boolValue: isBoolean ? newValue === 'true' : undefined,
      };

      if (idx !== -1) {
        fieldValues[idx] = { ...fieldValues[idx], ...updatedFv };
      } else if (newValue !== undefined) {
        fieldValues.push(updatedFv);
      }

      const updated: CostEstimateItemWeb = { ...prev, fieldValues };

      // Logika obliczeniowa spójna z CostEstimateTableView.updateItemFieldValue:
      // - pola źródłowe (101/200/201/207) → przelicz wszystkie obliczane
      // - pola obliczane edytowane ręcznie (gdy brak danych źródłowych) → przelicz pozostałe (pomiń zmienione)
      // - pozostałe pola (generic, name, unit...) → bez przeliczania
      let finalItem: CostEstimateItemWeb;
      if (isSourceFieldType(fieldType, fieldScope)) {
        finalItem = recalculateItem(updated, templateStructure);
      } else if (isCalculatedFieldType(fieldType, fieldScope)) {
        finalItem = recalculateItem(updated, templateStructure, normalizeFieldType(fieldType, fieldScope));
      } else {
        finalItem = updated;
      }

      // Gdy zmieniono ilość (101) → przelicz opcje — spójne z CostEstimateTableView
      const isQuantityField = fieldType === 101 || (fieldType === 1 && fieldScope === 1);
      if (isQuantityField && finalItem.options && finalItem.options.length > 0) {
        finalItem = {
          ...finalItem,
          options: finalItem.options.map((opt) => ({
            ...opt,
            fieldValues: recalculateOption(opt.fieldValues || [], templateStructure, finalItem),
          })),
        };
      }

      virtualItemRef.current = finalItem;
      setVirtualItem(finalItem);

      // Autozapis: każde pole które zmieniło wartość względem stanu sprzed zmiany
      for (const field of allSaveable) {
        const prevVal = readItemFieldValue(prev, field.id);
        const newVal = readItemFieldValue(finalItem, field.id);
        if (newVal !== prevVal) {
          onSaveField(field.id, getFieldSourceForDef(field, templateStructure), newVal);
        }
      }
    },
    [templateStructure, onSaveField]
  );

  const allValues = useMemo(
    () => getAllValues(virtualItem, templateStructure),
    [virtualItem, templateStructure]
  );
  const orderedFields = useMemo(
    () => getOrderedFields(templateStructure),
    [templateStructure]
  );

  return { virtualItem, allValues, orderedFields, handleFieldChange };
}
