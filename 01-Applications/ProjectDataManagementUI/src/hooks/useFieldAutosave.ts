/**
 * Hook do autosave pojedynczych pól kosztorysu z debounce.
 * 
 * Każda zmiana pola trafia jako osobny request do API.
 * Debounce 700ms od ostatniej zmiany - request leci dopiero gdy user skończy pisać.
 */

import { useRef, useCallback, useEffect } from 'react';
import { costEstimateApi } from '../api/costEstimateApi';
import type { UpsertFieldValueRequestDto } from '../types/costEstimate.types.new';

/** Klucz identyfikujący unikalne pole */
type FieldKey = string;

/** Stan oczekującej zmiany */
interface PendingChange {
  value: string | undefined;
  fieldType: number;
  valueType: FieldValueType;
  timeoutId: ReturnType<typeof setTimeout>;
}

/** Parametry wywołania autosave */
interface AutosaveParams {
  tenantId: string;
  projectId: string;
  costEstimateId: string;
}

/** Typ encji - grupa lub pozycja */
type EntityType = 'group' | 'item';

/** Typ wartości pola - określa jak mapować wartość na DTO */
export type FieldValueType = 'string' | 'numeric' | 'boolean' | 'date';

/** Informacje o polu do zapisu */
interface FieldInfo {
  entityType: EntityType;
  entityId: string;         // groupId lub itemId
  fieldValueId: string | null;  // null = nowe pole (create), guid = istniejące (update)
  fieldDefinitionId: string;    // wymagane gdy fieldValueId === null
  fieldType: number;
  /** Typ wartości pola - określa które pole DTO wypełnić */
  valueType: FieldValueType;
}

/** Callback wywoływany po udanym zapisie.
 * savedFieldValueId - ID zwrócone przez API (ważne gdy fieldValueId było null - pierwsze zapisanie pola)
 * savedValue        - wartość która została zapisana (potrzebna do uzupełnienia lokalnego stanu)
 */
type OnSaveSuccess = (fieldInfo: FieldInfo, savedFieldValueId: string, savedValue: string | undefined) => void;

/** Callback wywoływany przy błędzie */
type OnSaveError = (fieldInfo: FieldInfo, error: Error) => void;

const DEBOUNCE_MS = 700;

/**
 * Tworzy DTO do aktualizacji pola na podstawie typu wartości i wartości
 * 
 * Typy wartości (valueType):
 * - 'string' - zapisz do stringValue
 * - 'numeric' - parsuj i zapisz do decimalValue
 * - 'boolean' - parsuj i zapisz do boolValue
 * - 'date' - zapisz do dateTimeValue
 */
function createUpsertDto(
  fieldValueId: string | null,
  fieldDefinitionId: string,
  valueType: FieldValueType,
  value: string | undefined
): UpsertFieldValueRequestDto {
  const dto: UpsertFieldValueRequestDto = {
    fieldValueId,
    // fieldDefinitionId wymagane tylko przy tworzeniu (fieldValueId === null)
    fieldDefinitionId: fieldValueId === null ? fieldDefinitionId : null,
  };

  if (value === undefined || value === '') {
    // Wyczyść wszystkie wartości
    dto.stringValue = null;
    dto.decimalValue = null;
    dto.boolValue = null;
    dto.dateTimeValue = null;
    return dto;
  }

  switch (valueType) {
    case 'numeric': {
      const parsed = parseFloat(value.replace(',', '.'));
      dto.decimalValue = isNaN(parsed) ? null : parsed;
      break;
    }
    case 'boolean':
      dto.boolValue = value === 'true' || value === '1';
      break;
    case 'date':
      dto.dateTimeValue = value;
      break;
    case 'string':
    default:
      dto.stringValue = value;
      break;
  }

  return dto;
}

export interface UseFieldAutosaveOptions {
  params: AutosaveParams | null;
  onSaveSuccess?: OnSaveSuccess;
  onSaveError?: OnSaveError;
  /** Czy autosave jest włączony (domyślnie true) */
  enabled?: boolean;
}

export interface UseFieldAutosaveReturn {
  /**
   * Zgłoś zmianę pola - zostanie zapisana po debounce
   */
  scheduleFieldSave: (fieldInfo: FieldInfo, value: string | undefined) => void;
  
  /**
   * Wymuś natychmiastowy zapis wszystkich oczekujących zmian
   */
  flushPendingChanges: () => Promise<void>;
  
  /**
   * Anuluj oczekujące zmiany (np. przy utracie focusa bez zapisu)
   */
  cancelPendingChanges: () => void;
  
  /**
   * Czy są jakieś oczekujące zmiany
   */
  hasPendingChanges: () => boolean;
  
  /**
   * Ilość oczekujących zmian
   */
  pendingCount: () => number;
}

export function useFieldAutosave({
  params,
  onSaveSuccess,
  onSaveError,
  enabled = true,
}: UseFieldAutosaveOptions): UseFieldAutosaveReturn {
  
  // Mapa oczekujących zmian: fieldKey -> PendingChange
  const pendingChangesRef = useRef<Map<FieldKey, PendingChange & { fieldInfo: FieldInfo }>>(new Map());

  /**
   * Generuje unikalny klucz dla pola
   */
  const getFieldKey = useCallback((fieldInfo: FieldInfo): FieldKey => {
    return `${fieldInfo.entityType}:${fieldInfo.entityId}:${fieldInfo.fieldDefinitionId}`;
  }, []);

  /**
   * Wykonuje zapis pola do API
   */
  const saveField = useCallback(async (fieldInfo: FieldInfo, value: string | undefined) => {
    if (!params || !enabled) return;

    const { tenantId, projectId, costEstimateId } = params;
    const dto = createUpsertDto(fieldInfo.fieldValueId, fieldInfo.fieldDefinitionId, fieldInfo.valueType, value);

    try {
      let savedFieldValueId: string;
      if (fieldInfo.entityType === 'group') {
        savedFieldValueId = await costEstimateApi.upsertGroupField(
          tenantId,
          projectId,
          costEstimateId,
          fieldInfo.entityId,
          dto
        );
      } else {
        savedFieldValueId = await costEstimateApi.upsertItemField(
          tenantId,
          projectId,
          costEstimateId,
          fieldInfo.entityId,
          dto
        );
      }

      onSaveSuccess?.(fieldInfo, savedFieldValueId, value);

      // Jeśli podczas lotu requestu użytkownik edytował to samo pole ponownie,
      // to pending change ma jeszcze stary fieldValueId (null lub inny).
      // Aktualizujemy fieldValueId w oczekującej zmianie, żeby kolejny zapis
      // wysłał update zamiast ponownie tworzyć nowy rekord.
      const key = getFieldKey(fieldInfo);
      const stillPending = pendingChangesRef.current.get(key);
      if (stillPending && stillPending.fieldInfo.fieldValueId !== savedFieldValueId) {
        pendingChangesRef.current.set(key, {
          ...stillPending,
          fieldInfo: { ...stillPending.fieldInfo, fieldValueId: savedFieldValueId },
        });
      }
    } catch (error) {
      onSaveError?.(fieldInfo, error instanceof Error ? error : new Error(String(error)));
    }
  }, [params, enabled, onSaveSuccess, onSaveError, getFieldKey]);

  /**
   * Zaplanuj zapis pola z debounce
   */
  const scheduleFieldSave = useCallback((fieldInfo: FieldInfo, value: string | undefined) => {
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
      fieldType: fieldInfo.fieldType,
      valueType: fieldInfo.valueType,
      timeoutId,
      fieldInfo,
    });
  }, [params, enabled, getFieldKey, saveField]);

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
    const savePromises = Array.from(pending.values()).map(change => 
      saveField(change.fieldInfo, change.value)
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
      // Anuluj wszystkie timeouty przy unmount
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
