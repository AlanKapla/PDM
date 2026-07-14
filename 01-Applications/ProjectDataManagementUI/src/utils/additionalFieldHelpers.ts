/**
 * Helpery do pracy z polami dodatkowymi kosztorysu.
 *
 * Pola dodatkowe (CostEstimateAdditionalFieldWeb) to użytkownikiem definiowane pola
 * dołączone do schematu kosztorysu. Ich wartości (CostEstimateAdditionalFieldValueWeb)
 * są przechowywane na grupach i pozycjach.
 */

import type {
  CostEstimateAdditionalFieldWeb,
  CostEstimateAdditionalFieldValueWeb,
  CostEstimateFieldSchemaWeb,
} from '../types/costEstimate.types.new';
import { AdditionalFieldType } from '../types/costEstimate.types.new';
import type { FieldValueType } from '../hooks/useFieldAutosave';
import { sanitizeNumericInput } from './numericInputUtils';

export type AdditionalFieldInputKind = 'text' | 'number' | 'boolean' | 'date';

/**
 * Filtruje wpisywany tekst do dozwolonych znaków liczbowych (cyfry + jeden separator , lub .).
 */
export function sanitizeDecimalInput(raw: string): string {
  return sanitizeNumericInput(raw);
}

/**
 * Mapuje typ pola dodatkowego (AdditionalFieldType / CostEstimateFieldType 0–3) na rodzaj inputu.
 */
export function getAdditionalFieldInputKind(fieldType: number): AdditionalFieldInputKind {
  switch (fieldType) {
    case AdditionalFieldType.Decimal:
      return 'number';
    case AdditionalFieldType.Boolean:
      return 'boolean';
    case AdditionalFieldType.DateTime:
      return 'date';
    default:
      return 'text';
  }
}

export function getAdditionalFieldAutosaveValueType(fieldType: number): FieldValueType {
  const kind = getAdditionalFieldInputKind(fieldType);
  if (kind === 'number') {
    return 'numeric';
  }
  if (kind === 'boolean') {
    return 'boolean';
  }
  if (kind === 'date') {
    return 'date';
  }
  return 'string';
}

/**
 * Wartość daty w formacie YYYY-MM-DD dla natywnego date pickera.
 */
export function getAdditionalFieldDateInputValue(
  fieldValues: CostEstimateAdditionalFieldValueWeb[],
  additionalFieldId: string,
): string {
  const fv = getAdditionalFieldValue(fieldValues, additionalFieldId);
  if (!fv?.dateTimeValue) {
    return '';
  }
  return fv.dateTimeValue.length >= 10 ? fv.dateTimeValue.slice(0, 10) : fv.dateTimeValue;
}

/**
 * Definicje pól dodatkowych z API details (fallback: fieldSchemas).
 */
export function resolveAdditionalFieldDefinitions(details: {
  additionalFields?: CostEstimateAdditionalFieldWeb[];
  fieldSchemas?: CostEstimateFieldSchemaWeb[];
}): CostEstimateAdditionalFieldWeb[] {
  if (details.additionalFields && details.additionalFields.length > 0) {
    return details.additionalFields;
  }

  return (details.fieldSchemas ?? [])
    .filter((field) => field.isAdditionalField)
    .map((field) => ({
      id: field.id,
      costEstimateId: field.costEstimateId,
      name: field.fieldName,
      fieldType: field.fieldType as number as AdditionalFieldType,
      order: field.order,
      createdAt: field.createdAt,
      updatedAt: field.updatedAt,
    }));
}

/**
 * Zwraca typ pola dodatkowego z details (additionalFields lub fieldSchemas).
 */
export function resolveAdditionalFieldType(
  details: {
    additionalFields?: CostEstimateAdditionalFieldWeb[];
    fieldSchemas?: CostEstimateFieldSchemaWeb[];
  },
  additionalFieldId: string,
): number {
  const fromAdditional = details.additionalFields?.find((field) => field.id === additionalFieldId);
  if (fromAdditional) {
    return fromAdditional.fieldType;
  }

  const fromSchema = details.fieldSchemas?.find(
    (field) => field.id === additionalFieldId && field.isAdditionalField,
  );
  if (fromSchema) {
    return fromSchema.fieldType;
  }

  return AdditionalFieldType.String;
}

export function formatAdditionalFieldAutosaveValue(
  value: string | number | boolean | null,
): string | undefined {
  if (value === null) {
    return undefined;
  }
  if (typeof value === 'boolean') {
    return value ? 'true' : 'false';
  }
  return String(value);
}

/**
 * Głęboka kopia wartości pól dodatkowych (do backupu / dziedziczenia z opcji).
 */
export function cloneAdditionalFieldValues(
  values: CostEstimateAdditionalFieldValueWeb[] | undefined,
): CostEstimateAdditionalFieldValueWeb[] {
  return (values ?? []).map((fv) => ({ ...fv }));
}

/**
 * Buduje wpis wartości pola dodatkowego z poprawnym polem DTO (string/decimal/bool/dateTime).
 */
export function buildAdditionalFieldValueEntry(
  additionalFieldId: string,
  fieldType: number,
  value: string | number | boolean | null,
  existingId?: string,
): CostEstimateAdditionalFieldValueWeb {
  const entry: CostEstimateAdditionalFieldValueWeb = {
    id: existingId ?? `temp_${additionalFieldId}`,
    additionalFieldId,
    stringValue: undefined,
    decimalValue: undefined,
    boolValue: undefined,
    dateTimeValue: undefined,
  };

  if (value === null || value === undefined || value === '') {
    return entry;
  }

  switch (fieldType) {
    case AdditionalFieldType.Decimal: {
      if (typeof value === 'string') {
        entry.stringValue = value;
        const parsed = parseFloat(value.replace(',', '.'));
        entry.decimalValue = Number.isNaN(parsed) ? undefined : parsed;
      } else if (typeof value === 'number') {
        entry.decimalValue = value;
        entry.stringValue = String(value);
      }
      break;
    }
    case AdditionalFieldType.Boolean:
      entry.boolValue = typeof value === 'boolean' ? value : value === 'true';
      break;
    case AdditionalFieldType.DateTime:
      entry.dateTimeValue = typeof value === 'string' ? value : String(value);
      break;
    default:
      entry.stringValue = typeof value === 'string' ? value : String(value);
      break;
  }

  return entry;
}

/**
 * Aktualizuje lub dodaje wartość pola dodatkowego w tablicy (optimistic update).
 */
export function upsertAdditionalFieldValue(
  fieldValues: CostEstimateAdditionalFieldValueWeb[],
  additionalFieldId: string,
  fieldType: number,
  value: string | number | boolean | null,
): CostEstimateAdditionalFieldValueWeb[] {
  const existingIdx = fieldValues.findIndex(
    (fv) => fv.additionalFieldId === additionalFieldId,
  );
  const existingId = existingIdx >= 0 ? fieldValues[existingIdx].id : undefined;
  const entry = buildAdditionalFieldValueEntry(
    additionalFieldId,
    fieldType,
    value,
    existingId,
  );

  if (existingIdx >= 0) {
    const updated = [...fieldValues];
    updated[existingIdx] = entry;
    return updated;
  }

  return [...fieldValues, entry];
}

/**
 * Zwraca wartość pola dodatkowego dla danego additionalFieldId.
 * Zwraca undefined jeśli pole nie ma wartości.
 */
export function getAdditionalFieldValue(
  fieldValues: CostEstimateAdditionalFieldValueWeb[],
  additionalFieldId: string,
): CostEstimateAdditionalFieldValueWeb | undefined {
  return fieldValues.find((fv) => fv.additionalFieldId === additionalFieldId);
}

/**
 * Zwraca wartość pola dodatkowego jako string.
 * Używane do wyświetlania i jako value dla inputów.
 * Zwraca undefined jeśli pole nie ma wartości (wyświetl placeholder).
 */
export function getAdditionalFieldValueAsString(
  fieldValues: CostEstimateAdditionalFieldValueWeb[],
  additionalFieldId: string,
): string | undefined {
  const fv = getAdditionalFieldValue(fieldValues, additionalFieldId);
  if (!fv) return undefined;
  if (fv.stringValue !== undefined && fv.stringValue !== null) return fv.stringValue;
  if (fv.decimalValue !== undefined && fv.decimalValue !== null) return String(fv.decimalValue);
  if (fv.boolValue !== undefined && fv.boolValue !== null) return String(fv.boolValue);
  if (fv.dateTimeValue !== undefined && fv.dateTimeValue !== null) return fv.dateTimeValue;
  return undefined;
}

/**
 * Zwraca wartość pola dodatkowego jako number.
 * Zwraca 0 jeśli pole nie ma wartości numerycznej.
 */
export function getAdditionalFieldValueAsNumber(
  fieldValues: CostEstimateAdditionalFieldValueWeb[],
  additionalFieldId: string,
): number {
  const fv = getAdditionalFieldValue(fieldValues, additionalFieldId);
  if (!fv) return 0;
  if (fv.decimalValue !== undefined && fv.decimalValue !== null) return fv.decimalValue;
  if (fv.stringValue !== undefined && fv.stringValue !== null) {
    return parseFloat(fv.stringValue) || 0;
  }
  return 0;
}

/**
 * Zwraca wartość pola dodatkowego jako boolean.
 * Zwraca false jeśli pole nie ma wartości.
 */
export function getAdditionalFieldValueAsBoolean(
  fieldValues: CostEstimateAdditionalFieldValueWeb[],
  additionalFieldId: string,
): boolean {
  const fv = getAdditionalFieldValue(fieldValues, additionalFieldId);
  if (!fv) return false;
  if (fv.boolValue !== undefined && fv.boolValue !== null) return fv.boolValue;
  if (fv.stringValue !== undefined && fv.stringValue !== null) {
    return fv.stringValue === 'true' || fv.stringValue === '1';
  }
  return false;
}

/**
 * Zwraca definicję pola dodatkowego na podstawie ID.
 * Zwraca undefined jeśli pole nie istnieje w schemacie.
 */
export function getAdditionalFieldDefinition(
  fields: CostEstimateAdditionalFieldWeb[],
  fieldId: string,
): CostEstimateAdditionalFieldWeb | undefined {
  return fields.find((f) => f.id === fieldId);
}

/**
 * Sprawdza czy wartość pola dodatkowego jest pusta (brak jakiejkolwiek wartości).
 */
export function isAdditionalFieldValueEmpty(
  fv: CostEstimateAdditionalFieldValueWeb,
): boolean {
  return (
    (fv.stringValue === undefined || fv.stringValue === null || fv.stringValue === '') &&
    (fv.decimalValue === undefined || fv.decimalValue === null) &&
    (fv.boolValue === undefined || fv.boolValue === null) &&
    (fv.dateTimeValue === undefined || fv.dateTimeValue === null || fv.dateTimeValue === '')
  );
}

/**
 * Mapuje AdditionalFieldType na opisową nazwę typu wartości.
 * Używane do wyświetlania w SchemaManager.
 */
export function getAdditionalFieldTypeName(fieldType: AdditionalFieldType): string {
  switch (fieldType) {
    case 0: return 'Tekst';
    case 1: return 'Liczba';
    case 2: return 'Tak/Nie';
    case 3: return 'Data';
    default: return 'Tekst';
  }
}

/**
 * Sortuje pola dodatkowe według order (rosnąco).
 * Nie mutuje oryginalnej tablicy.
 */
export function sortAdditionalFields(
  fields: CostEstimateAdditionalFieldWeb[],
): CostEstimateAdditionalFieldWeb[] {
  return [...fields].sort((a, b) => a.order - b.order);
}

/**
 * Tworzy mapę: additionalFieldId -> CostEstimateAdditionalFieldValueWeb.
 * Szybki dostęp O(1) zamiast .find() w pętlach renderowania.
 */
export function buildAdditionalFieldValueMap(
  fieldValues: CostEstimateAdditionalFieldValueWeb[],
): Map<string, CostEstimateAdditionalFieldValueWeb> {
  const map = new Map<string, CostEstimateAdditionalFieldValueWeb>();
  for (const fv of fieldValues) {
    map.set(fv.additionalFieldId, fv);
  }
  return map;
}
