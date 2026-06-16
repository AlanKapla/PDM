export interface ColumnDef {
  id: string;
  label: string;
  description?: string;
  fieldType: 'string' | 'numeric' | 'boolean' | 'datetime';
  appliesTo: Array<'group' | 'item'>;
  width?: string;
  isAdditional?: boolean;
  isSortable?: boolean;
  textAlign?: 'left' | 'right' | 'center';
  fieldKey?: string;
  schemaFieldId?: string;
  /** Raw CostEstimateFieldType from API schema (106 = NetValue, 107 = GrossValue, …). */
  schemaFieldType?: number;
}

export interface SortConfig {
  field: string;
  direction: 'asc' | 'desc';
}

/** Stable column key — fieldKey for schema columns, id as fallback. */
export function getColumnFieldKey(col: ColumnDef): string {
  return col.fieldKey ?? col.id;
}

export const ALWAYS_VISIBLE_FIELD_KEYS = new Set(['name', 'actions']);

export function isAlwaysVisibleColumn(col: ColumnDef): boolean {
  return ALWAYS_VISIBLE_FIELD_KEYS.has(getColumnFieldKey(col));
}
