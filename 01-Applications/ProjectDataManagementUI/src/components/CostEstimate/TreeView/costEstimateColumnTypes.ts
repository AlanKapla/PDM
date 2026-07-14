import { CostEstimateFieldType } from '../../../types/costEstimate.types.new';

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

export type FinancialValueColumnKind = 'net' | 'vat' | 'gross';

const FINANCIAL_LABELS: Record<FinancialValueColumnKind, readonly string[]> = {
  net: ['wartość netto', 'wartosc netto'],
  vat: ['wartość vat', 'wartosc vat'],
  gross: ['wartość brutto', 'wartosc brutto'],
};

function resolveSchemaFieldType(schemaFieldType: ColumnDef['schemaFieldType']): number | undefined {
  if (schemaFieldType === undefined || schemaFieldType === null) {
    return undefined;
  }
  const numeric = Number(schemaFieldType);
  return Number.isFinite(numeric) ? numeric : undefined;
}

/** Rozpoznaje kolumny wartości netto / VAT / brutto (także legacy pola dodatkowe po etykiecie). */
export function getFinancialValueColumnKind(col: ColumnDef): FinancialValueColumnKind | null {
  const fieldKey = getColumnFieldKey(col);
  const schemaFieldType = resolveSchemaFieldType(col.schemaFieldType);

  if (fieldKey === 'netValue' || schemaFieldType === CostEstimateFieldType.NetValue) {
    return 'net';
  }
  if (fieldKey === 'grossValue' || schemaFieldType === CostEstimateFieldType.GrossValue) {
    return 'gross';
  }
  if (fieldKey === 'vatValue' || schemaFieldType === CostEstimateFieldType.VatValue) {
    return 'vat';
  }

  const normalizedLabel = col.label.trim().toLowerCase();
  for (const kind of ['net', 'vat', 'gross'] as const) {
    if (FINANCIAL_LABELS[kind].includes(normalizedLabel)) {
      return kind;
    }
  }

  return null;
}

export function isNetValueColumn(col: ColumnDef): boolean {
  return getFinancialValueColumnKind(col) === 'net';
}

export function isGrossValueColumn(col: ColumnDef): boolean {
  return getFinancialValueColumnKind(col) === 'gross';
}

export function isVatValueColumn(col: ColumnDef): boolean {
  return getFinancialValueColumnKind(col) === 'vat';
}

const BASIC_FINANCIAL_FIELD_KEYS = new Set(['netValue', 'grossValue', 'vatValue']);

/** Usuwa legacy pola dodatkowe duplikujące podstawowe kolumny finansowe. */
export function dedupeFinancialSchemaColumns(columns: ColumnDef[]): ColumnDef[] {
  const basicKinds = new Set(
    columns
      .filter((col) => !col.isAdditional)
      .map((col) => getFinancialValueColumnKind(col))
      .filter((kind): kind is FinancialValueColumnKind => kind !== null),
  );

  return columns.filter((col) => {
    if (!col.isAdditional) {
      return true;
    }
    const kind = getFinancialValueColumnKind(col);
    return kind === null || !basicKinds.has(kind);
  });
}

/** Uzupełnia widoczność podstawowych kolumn finansowych po zmianie schematu. */
export function ensureBasicFinancialColumnsVisible(
  visibleColIds: Set<string>,
  columns: ColumnDef[],
): Set<string> {
  const next = new Set(visibleColIds);
  for (const fieldKey of BASIC_FINANCIAL_FIELD_KEYS) {
    const basicCol = columns.find(
      (col) => !col.isAdditional && getColumnFieldKey(col) === fieldKey,
    );
    if (basicCol) {
      next.add(basicCol.id);
    }
  }
  return next;
}
