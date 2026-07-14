/**
 * Helpery do pracy ze schematem kosztorysu (CostEstimateSchemaWeb)
 */

import type { CostEstimateSchemaWeb, CostEstimateFieldDefinitionWeb } from '../types/costEstimate.types.new';

/** Pobiera definicję pola z schema po fieldType */
export function getFieldDefByType(
  schema: CostEstimateSchemaWeb,
  fieldType: number,
): CostEstimateFieldDefinitionWeb | undefined {
  return schema.fieldDefinitions.find((f) => f.fieldType === fieldType);
}

/** Pobiera definicję pola z schema po fieldName (Guid string) */
export function getFieldDefByName(
  schema: CostEstimateSchemaWeb,
  fieldName: string,
): CostEstimateFieldDefinitionWeb | undefined {
  return schema.fieldDefinitions.find((f) => f.fieldName === fieldName);
}

/** Pobiera wszystkie definicje pól z danego scope (Group=0, ItemSystem=1, ItemCalculated=2, ItemGeneric=3) */
export function getFieldDefsByScope(
  schema: CostEstimateSchemaWeb,
  fieldScope: number,
): CostEstimateFieldDefinitionWeb[] {
  return schema.fieldDefinitions.filter((f) => f.fieldScope === fieldScope);
}

/** Pobiera definicje pól systemowych (ItemSystem, FieldScope=1) */
export function getSystemFields(schema: CostEstimateSchemaWeb): CostEstimateFieldDefinitionWeb[] {
  return getFieldDefsByScope(schema, 1); // FieldScope.ItemSystem = 1
}

/** Pobiera definicje pól kalkulowanych (ItemCalculated, FieldScope=2) */
export function getCalculatedFields(schema: CostEstimateSchemaWeb): CostEstimateFieldDefinitionWeb[] {
  return getFieldDefsByScope(schema, 2); // FieldScope.ItemCalculated = 2
}

/** Pobiera definicje pól użytkownika (ItemGeneric, FieldScope=3) */
export function getGenericFields(schema: CostEstimateSchemaWeb): CostEstimateFieldDefinitionWeb[] {
  return getFieldDefsByScope(schema, 3); // FieldScope.ItemGeneric = 3
}

/** Pobiera definicje pól grupy (Group, FieldScope=0) */
export function getGroupFields(schema: CostEstimateSchemaWeb): CostEstimateFieldDefinitionWeb[] {
  return getFieldDefsByScope(schema, 0); // FieldScope.Group = 0
}

/** Pobiera wszystkie widoczne pola (isVisible = true), posortowane po order */
export function getVisibleFields(schema: CostEstimateSchemaWeb): CostEstimateFieldDefinitionWeb[] {
  return schema.fieldDefinitions
    .filter((f) => f.isVisible)
    .sort((a, b) => a.order - b.order);
}

/**
 * Fixed field types (hardcoded Guids from backend)
 * Use these for querying standard fields by type instead of fieldName
 */
export const FieldType = {
  // Group fields (0-99)
  GroupName: 0,

  // System fields (100-199)
  ItemSystemName: 100,
  ItemSystemQuantity: 101,
  ItemSystemUnit: 102,
  ItemSystemCategory: 103,
  ItemSystemSelected: 104,
  ItemSystemFiles: 105,

  // Calculated fields (200-299)
  ItemCalculatedUnitPriceNet: 200,
  ItemCalculatedVatRate: 201,
  ItemCalculatedUnitPriceGross: 202,
  ItemCalculatedValueNet: 203,
  ItemCalculatedValueGross: 204,
  ItemCalculatedTotalVat: 205,
  ItemCalculatedUnitVat: 206,

  // Generic/User fields start at 300
} as const;

/**
 * Standard field names (fixed Guids from backend CreateDefaultSchema)
 */
export const StandardFieldNames = {
  GroupName: '00000000-0000-0000-0000-000000000001',
  ItemSystemName: '00000000-0000-0000-0000-000000000100',
  ItemSystemQuantity: '00000000-0000-0000-0000-000000000101',
  ItemSystemUnit: '00000000-0000-0000-0000-000000000102',
  ItemCalculatedUnitPriceNet: '00000000-0000-0000-0000-000000000200',
  ItemCalculatedVatRate: '00000000-0000-0000-0000-000000000201',
  ItemCalculatedUnitPriceGross: '00000000-0000-0000-0000-000000000202',
  ItemCalculatedValueNet: '00000000-0000-0000-0000-000000000203',
  ItemCalculatedValueGross: '00000000-0000-0000-0000-000000000204',
  ItemCalculatedTotalVat: '00000000-0000-0000-0000-000000000205',
} as const;
