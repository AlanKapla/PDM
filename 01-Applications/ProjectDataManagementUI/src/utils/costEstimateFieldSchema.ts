import type { ColumnDef } from '../components/CostEstimate/TreeView/costEstimateColumnTypes';
import { getColumnFieldKey } from '../components/CostEstimate/TreeView/costEstimateColumnTypes';
import type {
  CostEstimateFieldSchemaWeb,
  CostEstimateAdditionalFieldWeb,
} from '../types/costEstimate.types.new';
import { CostEstimateFieldType } from '../types/costEstimate.types.new';

const GROUP_APPLICABLE_KEYS = new Set(['name', 'netValue', 'grossValue']);

const FIELD_DESCRIPTIONS: Record<string, string> = {
  name: 'Nazwa etapu lub pozycji',
  actions: 'Dodaj komponent, opcję, usuń element',
  quantity: 'Ilość jednostek',
  unit: 'Jednostka miary (szt, m², godz...)',
  unitPriceNet: 'Cena jednostkowa netto',
  vatRate: 'Stawka VAT (%)',
  unitPriceGross: 'Cena jednostkowa brutto = netto × (1 + VAT)',
  netValue: 'Wartość netto = ilość × cena netto',
  grossValue: 'Wartość brutto = wartość netto + VAT',
  vatValue: 'Wartość VAT = wartość netto × stawka VAT',
  isSelected: 'Sumuj — czy wliczać do sum etapu i kosztorysu',
  isStageWork: 'Zakres pracy harmonogramu — powiąż pozycję z harmonogramem',
  files: 'Załączone pliki',
};

function getAutoFitWidth(label: string, id: string, isSortable: boolean): string {
  if (id === 'name') return '180px';
  if (id === 'actions') return '120px';
  if (id === 'files') return '55px';

  const charWidth = 8;
  const padding = 32;
  const sortIconWidth = isSortable ? 20 : 0;
  const width = Math.max(60, label.length * charWidth + padding + sortIconWidth);
  return `${width}px`;
}

function mapFieldTypeToColumnFieldType(
  fieldType: CostEstimateFieldType
): ColumnDef['fieldType'] {
  switch (fieldType) {
    case CostEstimateFieldType.Number:
    case CostEstimateFieldType.Quantity:
    case CostEstimateFieldType.UnitPriceNet:
    case CostEstimateFieldType.VatRate:
    case CostEstimateFieldType.UnitPriceGross:
    case CostEstimateFieldType.NetValue:
    case CostEstimateFieldType.GrossValue:
    case CostEstimateFieldType.VatValue:
      return 'numeric';
    case CostEstimateFieldType.Boolean:
    case CostEstimateFieldType.IsSelected:
    case CostEstimateFieldType.IsStageWork:
      return 'boolean';
    case CostEstimateFieldType.Date:
      return 'datetime';
    default:
      return 'string';
  }
}

function getAppliesTo(field: CostEstimateFieldSchemaWeb): Array<'group' | 'item'> {
  if (field.isAdditionalField) {
    return ['group', 'item'];
  }

  if (GROUP_APPLICABLE_KEYS.has(field.fieldKey)) {
    return ['group', 'item'];
  }

  return ['item'];
}

function getTextAlign(field: CostEstimateFieldSchemaWeb): ColumnDef['textAlign'] {
  return getCostEstimateFieldTextAlign(field.fieldKey, field.fieldType);
}

/** Wyrównanie pola kosztorysu — ta sama reguła co w TreeView. */
export function getCostEstimateFieldTextAlign(
  fieldKey: string,
  fieldType: CostEstimateFieldType | number
): NonNullable<ColumnDef['textAlign']> {
  if (fieldKey === 'name') {
    return 'left';
  }

  if (fieldKey === 'actions' || fieldKey === 'files') {
    return 'center';
  }

  const columnFieldType = mapFieldTypeToColumnFieldType(fieldType as CostEstimateFieldType);
  if (columnFieldType === 'boolean') {
    return 'center';
  }

  return 'right';
}

/** Wyrównanie wartości w inputach (center → right, jak w TreeViewRow). */
export function getInputTextAlign(
  fieldKey: string,
  fieldType: CostEstimateFieldType | number
): 'left' | 'right' {
  const align = getCostEstimateFieldTextAlign(fieldKey, fieldType);
  return align === 'left' ? 'left' : 'right';
}

function isSortableField(field: CostEstimateFieldSchemaWeb): boolean {
  return field.fieldKey !== 'actions' && field.fieldKey !== 'files';
}

export function schemaFieldToColumnDef(field: CostEstimateFieldSchemaWeb): ColumnDef {
  const columnId = field.isAdditionalField ? field.id : field.fieldKey;
  const label = field.fieldName;
  const sortable = isSortableField(field);

  return {
    id: columnId,
    label,
    description: FIELD_DESCRIPTIONS[field.fieldKey],
    fieldType: mapFieldTypeToColumnFieldType(field.fieldType),
    appliesTo: getAppliesTo(field),
    width: getAutoFitWidth(label, field.fieldKey, sortable),
    isAdditional: field.isAdditionalField,
    isSortable: sortable,
    textAlign: getTextAlign(field),
    fieldKey: field.fieldKey,
    schemaFieldId: field.id,
    schemaFieldType: field.fieldType,
  };
}

export function buildColumnsFromSchema(
  fieldSchemas: CostEstimateFieldSchemaWeb[] | undefined
): ColumnDef[] {
  if (!fieldSchemas || fieldSchemas.length === 0) {
    return [];
  }

  return [...fieldSchemas]
    .sort((a, b) => a.order - b.order)
    .map(schemaFieldToColumnDef);
}

export function buildAdditionalFieldColumns(
  additionalFields: CostEstimateAdditionalFieldWeb[] | undefined
): ColumnDef[] {
  if (!additionalFields || additionalFields.length === 0) {
    return [];
  }

  return additionalFields
    .sort((a, b) => a.order - b.order)
    .map((field) =>
      schemaFieldToColumnDef({
        id: field.id,
        costEstimateId: field.costEstimateId,
        fieldName: field.name,
        fieldKey: field.id,
        fieldType: field.fieldType as unknown as CostEstimateFieldType,
        isBasicField: false,
        isAdditionalField: true,
        order: field.order,
        createdAt: field.createdAt,
        updatedAt: field.updatedAt,
      })
    );
}

/** Placeholder pól bazowych — nazwa kolumny z schematu (jak pola dodatkowe używają field.name). */
export function getBaseFieldPlaceholder(columnLabel: string): string {
  return columnLabel;
}

/** Etykieta pola bazowego po fieldKey — ta sama nazwa co kolumna w Tree View. */
export function getFieldLabelByKey(columns: ColumnDef[], fieldKey: string): string {
  const col = columns.find((c) => getColumnFieldKey(c) === fieldKey);
  return col?.label ?? fieldKey;
}

export function getSchemaColumns(details: {
  fieldSchemas?: CostEstimateFieldSchemaWeb[];
  additionalFields?: CostEstimateAdditionalFieldWeb[];
}): ColumnDef[] {
  if (details.fieldSchemas && details.fieldSchemas.length > 0) {
    return buildColumnsFromSchema(details.fieldSchemas);
  }

  return buildAdditionalFieldColumns(details.additionalFields);
}

/**
 * Pełna lista kolumn widoku drzewa — schemat z API uzupełniony o brakujące kolumny bazowe
 * (w tym netto/brutto wymagane dla sum etapów).
 */
export function mergeMissingBaseColumns(
  columns: ColumnDef[],
  fallbackBaseColumns: ColumnDef[]
): ColumnDef[] {
  const byKey = new Map<string, ColumnDef>();
  for (const col of columns) {
    byKey.set(getColumnFieldKey(col), col);
  }

  const merged: ColumnDef[] = [];
  for (const fallbackCol of fallbackBaseColumns) {
    const key = getColumnFieldKey(fallbackCol);
    merged.push(byKey.get(key) ?? fallbackCol);
    byKey.delete(key);
  }

  for (const col of columns) {
    const key = getColumnFieldKey(col);
    if (!merged.some((existing) => getColumnFieldKey(existing) === key)) {
      merged.push(col);
    }
  }

  return merged;
}

export function resolveTreeViewSchemaColumns(
  details: {
    fieldSchemas?: CostEstimateFieldSchemaWeb[];
    additionalFields?: CostEstimateAdditionalFieldWeb[];
  },
  fallbackBaseColumns: ColumnDef[]
): ColumnDef[] {
  if (details.fieldSchemas && details.fieldSchemas.length > 0) {
    const fromApi = buildColumnsFromSchema(details.fieldSchemas);
    if (fromApi.length === 0) {
      return fallbackBaseColumns;
    }
    return mergeMissingBaseColumns(fromApi, fallbackBaseColumns);
  }

  const additionalColumns = buildAdditionalFieldColumns(details.additionalFields);
  if (additionalColumns.length > 0) {
    return mergeMissingBaseColumns([...fallbackBaseColumns, ...additionalColumns], fallbackBaseColumns);
  }

  return fallbackBaseColumns;
}

export const MIN_COL_WIDTHS: Record<string, number> = {
  name: 120,
  actions: 90,
  quantity: 60,
  unit: 80,
  unitPriceNet: 110,
  vatRate: 80,
  unitPriceGross: 120,
  netValue: 100,
  grossValue: 110,
  vatValue: 90,
  isSelected: 55,
  isStageWork: 80,
  files: 45,
};
