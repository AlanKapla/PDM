/**
 * Resolver definicji pól kosztorysu — eliminuje powtarzalny wzorzec wyszukiwania
 * pola w systemFields / calculatedFields / genericFields.
 *
 * Dlaczego: ten sam 3-krokowy find powtarzał się ~10 razy w CostEstimateTableView.
 */

import type {
  SystemFieldWeb,
  CalculatedFieldWeb,
  GenericFieldWeb,
} from '../types/costEstimate.types';

export type FieldSource = 'system' | 'calculated' | 'generic';

export interface ResolvedField {
  /** Znaleziona definicja pola */
  fieldDef: SystemFieldWeb | CalculatedFieldWeb | GenericFieldWeb;
  /** Skąd pochodzi definicja */
  source: FieldSource;
}

/**
 * Szuka definicji pola po `id` w systemFields, calculatedFields, genericFields.
 * Zwraca `undefined` jeśli pole nie zostało znalezione.
 */
export function resolveFieldById(
  fieldId: string,
  templateStructure: any
): ResolvedField | undefined {
  const sysDef = (templateStructure.systemFields || []).find(
    (f: SystemFieldWeb) => f.id === fieldId
  );
  if (sysDef) return { fieldDef: sysDef, source: 'system' };

  const calcDef = (templateStructure.calculatedFields || []).find(
    (f: CalculatedFieldWeb) => f.id === fieldId
  );
  if (calcDef) return { fieldDef: calcDef, source: 'calculated' };

  const genDef = (templateStructure.genericFields || []).find(
    (f: GenericFieldWeb) => f.id === fieldId
  );
  if (genDef) return { fieldDef: genDef, source: 'generic' };

  return undefined;
}

/**
 * Szuka definicji pola po `fieldName` w systemFields, calculatedFields, genericFields.
 */
export function resolveFieldByName(
  fieldName: string,
  templateStructure: any
): ResolvedField | undefined {
  const sysDef = (templateStructure.systemFields || []).find(
    (f: SystemFieldWeb) => f.fieldName === fieldName
  );
  if (sysDef) return { fieldDef: sysDef, source: 'system' };

  const calcDef = (templateStructure.calculatedFields || []).find(
    (f: CalculatedFieldWeb) => f.fieldName === fieldName
  );
  if (calcDef) return { fieldDef: calcDef, source: 'calculated' };

  const genDef = (templateStructure.genericFields || []).find(
    (f: GenericFieldWeb) => f.fieldName === fieldName
  );
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
  templateStructure: any
): ResolvedField | undefined {
  if (fieldId) {
    const byId = resolveFieldById(fieldId, templateStructure);
    if (byId) return byId;
  }
  if (fieldName) {
    return resolveFieldByName(fieldName, templateStructure);
  }
  return undefined;
}

/**
 * Szuka definicji pola po `id` w głównych polach ORAZ w childFields (pola Options).
 * Używane przy aktualizacji opcji — childField definitions mają inne id niż główne.
 */
export function resolveFieldIncludingChildren(
  fieldId: string,
  templateStructure: any
): { fieldDef: any; source: FieldSource; fieldType?: number } | undefined {
  // Szukaj w głównych polach
  const mainResult = resolveFieldById(fieldId, templateStructure);
  if (mainResult) {
    const ft = (mainResult.fieldDef as any).fieldType ??
      (mainResult.fieldDef as any).fieldTypeConfig?.fieldType;
    return { ...mainResult, fieldType: ft };
  }

  // Szukaj w childFields
  for (const sysField of (templateStructure.systemFields || [])) {
    if (sysField.childFields) {
      const childDef = sysField.childFields.find((cf: any) => cf.id === fieldId);
      if (childDef) {
        const ft = childDef.fieldType ?? childDef.fieldTypeConfig?.fieldType;
        return { fieldDef: childDef, source: 'system', fieldType: ft };
      }
    }
  }

  return undefined;
}

/**
 * Określa źródło (source) pola po jego id — przydatne gdy mamy już fieldDef.
 */
export function getFieldSource(fieldId: string, templateStructure: any): FieldSource {
  if ((templateStructure.systemFields || []).find((f: any) => f.id === fieldId)) {
    return 'system';
  }
  if ((templateStructure.calculatedFields || []).find((f: any) => f.id === fieldId)) {
    return 'calculated';
  }
  return 'generic';
}
