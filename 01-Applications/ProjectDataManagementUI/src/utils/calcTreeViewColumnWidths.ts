import type { ColumnDef } from '../components/CostEstimate/TreeView/costEstimateColumnTypes';
import type {
  CostEstimateDetailsWeb,
  CostEstimateGroupWeb,
  CostEstimateItemWeb,
  CostEstimateAdditionalFieldWeb,
} from '../types/costEstimate.types.new';
import {
  getAdditionalFieldAutosaveValueType,
  getAdditionalFieldValueAsString,
} from './additionalFieldHelpers';
import { MIN_COL_WIDTHS } from './costEstimateFieldSchema';

const HEADER_CHAR_WIDTH = 7.2;
const CELL_CHAR_WIDTH = 8;
const MONO_CHAR_WIDTH = 8.2;
const CELL_PADDING = 28;
const SORT_ICON_WIDTH = 16;
const EMPTY_CELL = '—';

/** Fixed UI elements inside the name cell (drag, chevron, tag, dot, gaps). */
const NAME_FIXED_OVERHEAD = 130;
const NAME_INDENT_PER_LEVEL = 28;
const NAME_CHAR_WIDTH = 8;
const NAME_BREATHING = 32;
const NAME_MIN_WIDTH = 150;
const NAME_MAX_WIDTH = 600;

function fmtNum(val: number | undefined | null): string {
  if (val === undefined || val === null) return '';
  return val.toFixed(2);
}

function updateMaxChars(map: Record<string, number>, colId: string, text: string): void {
  const len = text.length;
  if (len > (map[colId] ?? 0)) {
    map[colId] = len;
  }
}

function widthFromCharCount(
  col: ColumnDef,
  maxContentChars: number,
  minWidth: number
): number {
  const headerChars = col.label.length;
  const chars = Math.max(maxContentChars, headerChars);
  const charWidth =
    col.fieldType === 'numeric' ? MONO_CHAR_WIDTH : col.id === 'name' ? NAME_CHAR_WIDTH : CELL_CHAR_WIDTH;
  const headerWidth = headerChars * HEADER_CHAR_WIDTH;
  const contentWidth = maxContentChars * charWidth;
  const sortExtra = col.isSortable ? SORT_ICON_WIDTH : 0;
  const width = Math.max(headerWidth, contentWidth) + CELL_PADDING + sortExtra;
  return Math.max(minWidth, Math.ceil(width));
}

/**
 * Content-aware width for the name column (indentation + UI overhead).
 */
export function calcNameColumnWidth(details: CostEstimateDetailsWeb): number {
  let maxWidth = 0;

  function traverseGroups(groups: CostEstimateGroupWeb[], level: number): void {
    for (const group of groups) {
      const indent = level * NAME_INDENT_PER_LEVEL;
      const groupWidth = group.name.length * NAME_CHAR_WIDTH + indent + NAME_FIXED_OVERHEAD + NAME_BREATHING;
      if (groupWidth > maxWidth) {
        maxWidth = groupWidth;
      }

      for (const item of group.items ?? []) {
        const itemIndent = (level + 1) * NAME_INDENT_PER_LEVEL;
        const itemWidth = item.name.length * NAME_CHAR_WIDTH + itemIndent + NAME_FIXED_OVERHEAD + NAME_BREATHING;
        if (itemWidth > maxWidth) {
          maxWidth = itemWidth;
        }

        for (const comp of item.components ?? []) {
          const compIndent = (level + 2) * NAME_INDENT_PER_LEVEL;
          const compWidth = comp.name.length * NAME_CHAR_WIDTH + compIndent + NAME_FIXED_OVERHEAD + NAME_BREATHING;
          if (compWidth > maxWidth) {
            maxWidth = compWidth;
          }
        }

        for (const opt of item.options ?? []) {
          const optIndent = (level + 2) * NAME_INDENT_PER_LEVEL;
          const optWidth = opt.name.length * NAME_CHAR_WIDTH + optIndent + NAME_FIXED_OVERHEAD + NAME_BREATHING;
          if (optWidth > maxWidth) {
            maxWidth = optWidth;
          }
        }
      }

      traverseGroups(group.childGroups ?? [], level + 1);
    }
  }

  traverseGroups(details.rootGroups, 0);
  return Math.max(NAME_MIN_WIDTH, Math.min(NAME_MAX_WIDTH, Math.ceil(maxWidth)));
}

function getItemFieldText(item: CostEstimateItemWeb, fieldKey: string): string {
  switch (fieldKey) {
    case 'quantity':
      return fmtNum(item.quantity);
    case 'unit':
      return item.unit ?? '';
    case 'unitPriceNet':
      return fmtNum(item.unitPriceNet);
    case 'vatRate':
      return item.vatRate !== undefined && item.vatRate !== null
        ? String(Math.round(item.vatRate * 100))
        : '';
    case 'unitPriceGross':
      return fmtNum(item.unitPriceGross);
    case 'netValue':
      return fmtNum(item.netValue);
    case 'grossValue':
      return fmtNum(item.grossValue);
    case 'vatValue':
      return fmtNum(item.vatValue);
    case 'isSelected':
    case 'isStageWork':
      return '✓';
    case 'files':
      return String(item.files?.length ?? 0);
    default:
      return '';
  }
}

function processAdditionalValues(
  fieldValues: CostEstimateGroupWeb['additionalFieldValues'],
  additionalFieldDefs: CostEstimateAdditionalFieldWeb[],
  maxChars: Record<string, number>
): void {
  for (const fieldDef of additionalFieldDefs) {
    const valueType = getAdditionalFieldAutosaveValueType(fieldDef.fieldType);
    if (valueType === 'boolean') {
      updateMaxChars(maxChars, fieldDef.id, '✓');
      continue;
    }
    const text = getAdditionalFieldValueAsString(fieldValues ?? [], fieldDef.id);
    if (text) {
      updateMaxChars(maxChars, fieldDef.id, text);
    }
  }
}

function processItem(
  item: CostEstimateItemWeb,
  columns: ColumnDef[],
  additionalFieldDefs: CostEstimateAdditionalFieldWeb[],
  maxChars: Record<string, number>
): void {
  for (const col of columns) {
    if (col.id === 'name' || col.id === 'actions') {
      continue;
    }
    if (col.isAdditional) {
      continue;
    }
    const text = getItemFieldText(item, col.fieldKey ?? col.id);
    if (text) {
      updateMaxChars(maxChars, col.id, text);
    }
  }
  processAdditionalValues(item.additionalFieldValues, additionalFieldDefs, maxChars);
}

function processGroup(
  group: CostEstimateGroupWeb,
  columns: ColumnDef[],
  additionalFieldDefs: CostEstimateAdditionalFieldWeb[],
  maxChars: Record<string, number>
): void {
  const netText = fmtNum(group.totalNet) || EMPTY_CELL;
  const grossText = fmtNum(group.totalGross) || EMPTY_CELL;
  updateMaxChars(maxChars, 'netValue', netText);
  updateMaxChars(maxChars, 'grossValue', grossText);
  processAdditionalValues(group.additionalFieldValues, additionalFieldDefs, maxChars);

  for (const item of group.items ?? []) {
    processItem(item, columns, additionalFieldDefs, maxChars);
    for (const comp of item.components ?? []) {
      processItem(comp, columns, additionalFieldDefs, maxChars);
    }
    for (const opt of item.options ?? []) {
      processItem(opt, columns, additionalFieldDefs, maxChars);
    }
  }

  for (const child of group.childGroups ?? []) {
    processGroup(child, columns, additionalFieldDefs, maxChars);
  }
}

function getFixedColumnWidth(col: ColumnDef): number | null {
  if (col.id === 'name') {
    return null;
  }
  if (col.id === 'actions') {
    return Math.max(90, col.label.length * HEADER_CHAR_WIDTH + CELL_PADDING + 48);
  }
  if (col.id === 'files') {
    return Math.max(48, col.label.length * HEADER_CHAR_WIDTH + CELL_PADDING);
  }
  if (col.fieldType === 'boolean') {
    return Math.max(52, col.label.length * HEADER_CHAR_WIDTH + CELL_PADDING);
  }
  return null;
}

/**
 * Computes pixel widths for all visible columns from header labels and cell content.
 */
export function calcTreeViewColumnWidths(
  details: CostEstimateDetailsWeb,
  columns: ColumnDef[],
  additionalFieldDefs: CostEstimateAdditionalFieldWeb[]
): Record<string, number> {
  const maxChars: Record<string, number> = {};

  for (const col of columns) {
    updateMaxChars(maxChars, col.id, col.label);
    if (col.fieldType === 'boolean') {
      updateMaxChars(maxChars, col.id, '✓');
    }
  }

  for (const group of details.rootGroups) {
    processGroup(group, columns, additionalFieldDefs, maxChars);
  }

  const result: Record<string, number> = {};

  for (const col of columns) {
    const minWidth = MIN_COL_WIDTHS[col.id] ?? MIN_COL_WIDTHS[col.fieldKey ?? ''] ?? 60;
    const fixed = getFixedColumnWidth(col);

    if (col.id === 'name') {
      result[col.id] = calcNameColumnWidth(details);
      continue;
    }

    if (fixed !== null) {
      result[col.id] = Math.max(minWidth, fixed, widthFromCharCount(col, maxChars[col.id] ?? 0, minWidth));
      continue;
    }

    result[col.id] = widthFromCharCount(col, maxChars[col.id] ?? col.label.length, minWidth);
  }

  return result;
}

export function getColumnCellJustify(textAlign: ColumnDef['textAlign'] | undefined): string {
  if (textAlign === 'right') {
    return 'flex-end';
  }
  if (textAlign === 'center') {
    return 'center';
  }
  return 'flex-start';
}
