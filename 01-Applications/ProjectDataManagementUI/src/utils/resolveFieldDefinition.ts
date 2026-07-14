/**
 * Resolver definicji pól kosztorysu — eliminuje powtarzalny wzorzec wyszukiwania
 * pola w systemFields / calculatedFields / genericFields.
 *
 * Dlaczego: ten sam 3-krokowy find powtarzał się ~10 razy w CostEstimateTableView.
 */

import type {
  CostEstimateSchemaWeb,
  CostEstimateFieldDefinitionWeb,
} from '../types/costEstimate.types.new';
import {
  getSystemFields,
  getCalculatedFields,
  getGenericFields,
} from './schemaHelpers';

export type FieldSource = 'system' | 'calculated' | 'generic';

export interface ResolvedField {
  /** Znaleziona definicja pola */
  fieldDef: CostEstimateFieldDefinitionWeb;
  /** Skąd pochodzi definicja */
  source: FieldSource;
}

/**
 * Szuka definicji pola po `id` w systemFields, calculatedFields, genericFields.
 * Zwraca `undefined` jeśli pole nie zostało znalezione.
 */
export function resolveFieldById(
  fieldId: string,
  schema: CostEstimateSchemaWeb
): ResolvedField | undefined {
  const systemFields = getSystemFields(schema);
  const sysDef = systemFields.find((f) => f.id === fieldId);
  if (sysDef) return { fieldDef: sysDef, source: 'system' };

  const calculatedFields = getCalculatedFields(schema);
  const calcDef = calculatedFields.find((f) => f.id === fieldId);
  if (calcDef) return { fieldDef: calcDef, source: 'calculated' };

  const genericFields = getGenericFields(schema);
  const genDef = genericFields.find((f) => f.id === fieldId);
  if (genDef) return { fieldDef: genDef, source: 'generic' };

  return undefined;
}

/**
 * Szuka definicji pola po `fieldName` w systemFields, calculatedFields, genericFields.
 */
export function resolveFieldByName(
  fieldName: string,
  schema: CostEstimateSchemaWeb
): ResolvedField | undefined {
  const systemFields = getSystemFields(schema);
  const sysDef = systemFields.find((f) => f.fieldName === fieldName);
  if (sysDef) return { fieldDef: sysDef, source: 'system' };

  const calculatedFields = getCalculatedFields(schema);
  const calcDef = calculatedFields.find((f) => f.fieldName === fieldName);
  if (calcDef) return { fieldDef: calcDef, source: 'calculated' };

  const genericFields = getGenericFields(schema);
  const genDef = genericFields.find((f) => f.fieldName === fieldName);
  if (genDef) return { fieldDef: genDef, source: 'generic' };

  return undefined;
}

/**
 * Szuka definicji pola po `id` lub `fieldName` — przeszukuje jedno i drugie.
 * Przydatne gdy kolumna może mapować pole po id lub po nazwie.
 */
export function resolveFieldByIdOrName(
  fieldId: string | undefined,
  fieldName: string | undefined,
  schema: CostEstimateSchemaWeb
): ResolvedField | undefined {
  if (fieldId) {
    const byId = resolveFieldById(fieldId, schema);
    if (byId) return byId;
  }
  if (fieldName) {
    return resolveFieldByName(fieldName, schema);
  }
  return undefined;
}

/**
 * Szuka definicji pola po `id` w głównych polach.
 * Schema-based structure nie ma childFields - wszystkie pola są w fieldDefinitions.
 */
export function resolveFieldIncludingChildren(
  fieldId: string,
  schema: CostEstimateSchemaWeb
): { fieldDef: CostEstimateFieldDefinitionWeb; source: FieldSource; fieldType?: number } | undefined {
  // Szukaj w głównych polach
  const mainResult = resolveFieldById(fieldId, schema);
  if (mainResult) {
    const ft = mainResult.fieldDef.fieldType;
    return { ...mainResult, fieldType: ft };
  }

  // Schema-based structure nie ma childFields - wszystkie pola są w fieldDefinitions
  return undefined;
}

/**
 * Określa źródło (source) pola po jego id — przydatne gdy mamy już fieldDef.
 */
export function getFieldSource(fieldId: string, schema: CostEstimateSchemaWeb): FieldSource {
  const systemFields = getSystemFields(schema);
  if (systemFields.find((f) => f.id === fieldId)) {
    return 'system';
  }
  const calculatedFields = getCalculatedFields(schema);
  if (calculatedFields.find((f) => f.id === fieldId)) {
    return 'calculated';
  }
  return 'generic';
}
