/**
 * Hook do autosave pojedynczych pól kosztorysu z debounce.
 *
 * Obsługuje dwa typy zapisów:
 * - Base fields (name, quantity, unit, unitPriceNet, vatRate, netValue, grossValue, vatValue, unitPriceGross) → updateItemBaseFields / updateGroupBaseFields
 * - Additional fields (pola dodatkowe) → upsertItemAdditionalField / upsertGroupAdditionalField
 *
 * Debounce 700ms od ostatniej zmiany - request leci dopiero gdy user skończy pisać.
 */

import { useRef, useCallback, useEffect } from 'react';
import {
  isInProgressNumericInput,
  isPartialNumericInput,
  parseNumericInput,
  roundToDecimals,
} from '../utils/numericInputUtils';
import {
  upsertGroupAdditionalField,
  upsertItemAdditionalField,
  updateItemBaseFields,
  updateGroupBaseFields,
} from '../api/costEstimateApi';

/** Klucz identyfikujący unikalne pole */
type FieldKey = string;

/** Typ encji - grupa lub pozycja */
export type FieldEntityType = 'group' | 'item';

/** Typ wartości pola - określa jak mapować wartość na DTO */
export type FieldValueType = 'string' | 'numeric' | 'boolean' | 'date';

/** Informacje o polu do zapisu */
export interface FieldInfo {
  entityType: FieldEntityType;
  entityId: string;               // groupId lub itemId
  /** 'base' = pole systemowe (name/quantity/unit/unitPriceNet/vatRate), 'additional' = pole dodatkowe */
  fieldType: 'base' | 'additional';
  /** Nazwa pola bazowego (np. 'name', 'quantity') — używane gdy fieldType='base' */
  name: string;
  /** ID definicji pola dodatkowego — używane gdy fieldType='additional' */
  additionalFieldId?: string;
  /** ID istniejącej wartości pola dodatkowego — null = nowy rekord */
  fieldValueId?: string | null;
  /** Typ wartości pola - określa które pole DTO wypełnić */
  valueType: FieldValueType;
}

/** Stan oczekującej zmiany */
interface PendingChange {
  value: string | undefined;
  valueType: FieldValueType;
  timeoutId: ReturnType<typeof setTimeout>;
  fieldInfo: FieldInfo;
}

/** Parametry wywołania autosave */
interface AutosaveParams {
  tenantId: string;
  projectId: string;
  costEstimateId: string;
}

/** Callback wywoływany po udanym zapisie.
 * savedFieldValueId - ID zwrócone przez API (dla additional fields przy pierwszym zapisie)
 * savedValue        - wartość która została zapisana
 */
type OnSaveSuccess = (
  fieldInfo: FieldInfo,
  savedFieldValueId: string | undefined,
  savedValue: string | undefined,
) => void;

/** Callback wywoływany przy błędzie */
type OnSaveError = (fieldInfo: FieldInfo, error: Error) => void;

const DEBOUNCE_MS = 700;

/**
 * Mapuje string value na właściwą wartość bazową wg valueType
 */
function parseBaseFieldValue(
  name: string,
  valueType: FieldValueType,
  value: string | undefined,
): Record<string, string | number | boolean | null | undefined> {
  if (value === undefined || value === '') {
    const data: Record<string, string | number | boolean | null | undefined> = {
      [name]: null,
    };

    if (valueType === 'string') {
      if (name === 'name') {
        return { clearName: true };
      }
      if (name === 'unit') {
        return { clearUnit: true };
      }
      return { [name]: '' };
    }

    if (name === 'vatRate') {
      (data as Record<string, unknown>).clearVatRate = true;
    } else if (name === 'quantity') {
      (data as Record<string, unknown>).clearQuantity = true;
    } else if (name === 'unitPriceNet') {
      (data as Record<string, unknown>).clearUnitPriceNet = true;
    } else if (name === 'netValue') {
      (data as Record<string, unknown>).clearNetValue = true;
    } else if (name === 'vatValue') {
      (data as Record<string, unknown>).clearVatValue = true;
    } else if (name === 'grossValue') {
      (data as Record<string, unknown>).clearGrossValue = true;
    } else if (name === 'unitPriceGross') {
      (data as Record<string, unknown>).clearUnitPriceGross = true;
    }

    return data;
  }
  switch (valueType) {
    case 'numeric': {
      if (name === 'vatRate') {
        if (value !== undefined && isPartialNumericInput(value)) {
          return {};
        }
        const percent = value === undefined ? null : parseNumericInput(value);
        return { [name]: percent === null ? null : roundToDecimals(percent / 100, 4) };
      }
      if (value !== undefined && isPartialNumericInput(value)) {
        return {};
      }
      const parsed = value === undefined ? null : parseNumericInput(value);
      return { [name]: parsed };
    }
    case 'boolean':
      return { [name]: value === 'true' || value === '1' };
    default:
      return { [name]: value };
  }
}

/**
 * Buduje payload dla upsert pola dodatkowego
 */
function buildAdditionalFieldPayload(
  additionalFieldId: string,
  valueType: FieldValueType,
  value: string | undefined,
): {
  additionalFieldId: string;
  stringValue?: string | null;
  decimalValue?: number | null;
  boolValue?: boolean | null;
  dateTimeValue?: string | null;
} {
  if (value === undefined || value === '') {
    return {
      additionalFieldId,
      stringValue: null,
      decimalValue: null,
      boolValue: null,
      dateTimeValue: null,
    };
  }

  switch (valueType) {
    case 'numeric': {
      const parsed = value === undefined ? null : parseNumericInput(value);
      return { additionalFieldId, decimalValue: parsed };
    }
    case 'boolean':
      return { additionalFieldId, boolValue: value === 'true' || value === '1' };
    case 'date':
      return { additionalFieldId, dateTimeValue: value };
    case 'string':
    default:
      return { additionalFieldId, stringValue: value };
  }
}

export interface UseFieldAutosaveOptions {
  params: AutosaveParams | null;
  onSaveSuccess?: OnSaveSuccess;
  onSaveError?: OnSaveError;
  /** Czy autosave jest włączony (domyślnie true) */
  enabled?: boolean;
}

export interface UseFieldAutosaveReturn {
  /** Zgłoś zmianę pola - zostanie zapisana po debounce */
  scheduleFieldSave: (fieldInfo: FieldInfo, value: string | undefined) => void;

  /** Wymuś natychmiastowy zapis wszystkich oczekujących zmian */
  flushPendingChanges: () => Promise<void>;

  /** Anuluj oczekujące zmiany (np. przy utracie focusa bez zapisu) */
  cancelPendingChanges: () => void;

  /** Czy są jakieś oczekujące zmiany */
  hasPendingChanges: () => boolean;

  /** Ilość oczekujących zmian */
  pendingCount: () => number;
}

export function useFieldAutosave({
  params,
  onSaveSuccess,
  onSaveError,
  enabled = true,
}: UseFieldAutosaveOptions): UseFieldAutosaveReturn {

  // Mapa oczekujących zmian: fieldKey -> PendingChange
  const pendingChangesRef = useRef<Map<FieldKey, PendingChange>>(new Map());

  /**
   * Generuje unikalny klucz dla pola.
   * Dla pól bazowych: entityType:entityId:base:name
   * Dla pól dodatkowych: entityType:entityId:additional:additionalFieldId
   */
  const getFieldKey = useCallback((fieldInfo: FieldInfo): FieldKey => {
    if (fieldInfo.fieldType === 'base') {
      return `${fieldInfo.entityType}:${fieldInfo.entityId}:base:${fieldInfo.name}`;
    }
    return `${fieldInfo.entityType}:${fieldInfo.entityId}:additional:${fieldInfo.additionalFieldId ?? ''}`;
  }, []);

  /**
   * Wykonuje zapis pola do API
   */
  const saveField = useCallback(
    async (fieldInfo: FieldInfo, value: string | undefined) => {
      if (!params || !enabled) return;

      if (fieldInfo.valueType === 'numeric' && value !== undefined && isInProgressNumericInput(value)) {
        return;
      }

      const { tenantId, projectId, costEstimateId } = params;

      try {
        if (fieldInfo.fieldType === 'base') {
          // Zapis pola bazowego
          const data = parseBaseFieldValue(fieldInfo.name, fieldInfo.valueType, value);

          if (fieldInfo.entityType === 'group') {
            await updateGroupBaseFields(tenantId, projectId, costEstimateId, fieldInfo.entityId, data as {
              name?: string;
              clearName?: boolean;
            });
          } else {
            await updateItemBaseFields(
              tenantId,
              projectId,
              costEstimateId,
              fieldInfo.entityId,
              data as {
                name?: string;
                quantity?: number | null;
                unit?: string | null;
                unitPriceNet?: number | null;
                vatRate?: number | null;
                netValue?: number | null;
                grossValue?: number | null;
                vatValue?: number | null;
                unitPriceGross?: number | null;
                clearName?: boolean;
                clearQuantity?: boolean;
                clearUnit?: boolean;
                clearUnitPriceNet?: boolean;
                clearVatRate?: boolean;
                clearNetValue?: boolean;
                clearGrossValue?: boolean;
                clearVatValue?: boolean;
                clearUnitPriceGross?: boolean;
                isSelected?: boolean | null;
                isStageWork?: boolean | null;
              },
            );
          }
          onSaveSuccess?.(fieldInfo, undefined, value);
        } else {
          // Zapis pola dodatkowego
          const additionalFieldId = fieldInfo.additionalFieldId ?? '';
          const payload = buildAdditionalFieldPayload(additionalFieldId, fieldInfo.valueType, value);

          let savedFieldValueId: string;
          if (fieldInfo.entityType === 'group') {
            savedFieldValueId = await upsertGroupAdditionalField(
              tenantId,
              projectId,
              costEstimateId,
              fieldInfo.entityId,
              payload,
            );
          } else {
            savedFieldValueId = await upsertItemAdditionalField(
              tenantId,
              projectId,
              costEstimateId,
              fieldInfo.entityId,
              payload,
            );
          }

          onSaveSuccess?.(fieldInfo, savedFieldValueId, value);

          // Jeśli podczas lotu requestu użytkownik edytował to samo pole ponownie,
          // aktualizujemy fieldValueId żeby kolejny zapis wysłał update zamiast create.
          const key = getFieldKey(fieldInfo);
          const stillPending = pendingChangesRef.current.get(key);
          if (stillPending && stillPending.fieldInfo.fieldValueId !== savedFieldValueId) {
            pendingChangesRef.current.set(key, {
              ...stillPending,
              fieldInfo: { ...stillPending.fieldInfo, fieldValueId: savedFieldValueId },
            });
          }
        }
      } catch (error) {
        onSaveError?.(fieldInfo, error instanceof Error ? error : new Error(String(error)));
      }
    },
    [params, enabled, onSaveSuccess, onSaveError, getFieldKey],
  );

  /**
   * Zaplanuj zapis pola z debounce
   */
  const scheduleFieldSave = useCallback(
    (fieldInfo: FieldInfo, value: string | undefined) => {
      if (!params || !enabled) return;

      const key = getFieldKey(fieldInfo);
      const pending = pendingChangesRef.current;

      // Anuluj poprzedni timeout dla tego pola
      const existing = pending.get(key);
      if (existing) {
        clearTimeout(existing.timeoutId);
      }

      // Ustaw nowy timeout
      const timeoutId = setTimeout(async () => {
        const change = pending.get(key);
        if (change) {
          pending.delete(key);
          await saveField(change.fieldInfo, change.value);
        }
      }, DEBOUNCE_MS);

      pending.set(key, {
        value,
        valueType: fieldInfo.valueType,
        timeoutId,
        fieldInfo,
      });
    },
    [params, enabled, getFieldKey, saveField],
  );

  /**
   * Wymuś natychmiastowy zapis wszystkich oczekujących zmian
   */
  const flushPendingChanges = useCallback(async () => {
    const pending = pendingChangesRef.current;

    // Anuluj wszystkie timeouty
    for (const change of pending.values()) {
      clearTimeout(change.timeoutId);
    }

    // Zapisz wszystkie zmiany równolegle
    const savePromises = Array.from(pending.values()).map((change) =>
      saveField(change.fieldInfo, change.value),
    );

    pending.clear();

    await Promise.all(savePromises);
  }, [saveField]);

  /**
   * Anuluj wszystkie oczekujące zmiany
   */
  const cancelPendingChanges = useCallback(() => {
    const pending = pendingChangesRef.current;

    for (const change of pending.values()) {
      clearTimeout(change.timeoutId);
    }

    pending.clear();
  }, []);

  /**
   * Czy są oczekujące zmiany
   */
  const hasPendingChanges = useCallback(() => {
    return pendingChangesRef.current.size > 0;
  }, []);

  /**
   * Ilość oczekujących zmian
   */
  const pendingCount = useCallback(() => {
    return pendingChangesRef.current.size;
  }, []);

  // Cleanup przy unmount
  useEffect(() => {
    return () => {
      for (const change of pendingChangesRef.current.values()) {
        clearTimeout(change.timeoutId);
      }
    };
  }, []);

  return {
    scheduleFieldSave,
    flushPendingChanges,
    cancelPendingChanges,
    hasPendingChanges,
    pendingCount,
  };
}
