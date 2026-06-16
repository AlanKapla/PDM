/**
 * Współdzielone typy i stałe używane przez CostEstimateTableView i komponenty wierszy.
 */

import type { AllItemValues } from '../../utils/costEstimateCalculations';
import type {
  CostEstimateGroupWeb,
  CostEstimateItemWeb,
} from '../../types/costEstimate.types.new';

// ---------------------------------------------------------------------------
// Stałe
// ---------------------------------------------------------------------------

// ---------------------------------------------------------------------------
// Typy
// ---------------------------------------------------------------------------

/** Spłaszczony wiersz tabeli — używany w flatRows */
export interface FlatRow {
  type: 'group' | 'item' | 'groupSummary';
  level: number;
  groupId?: string;
  group?: CostEstimateGroupWeb;
  /** Hierarchiczny numer grupy (np. "1", "1.1", "1.1.1") */
  groupNumber?: string;
  item?: CostEstimateItemWeb;
  itemIndex?: number;
}

/** Rozszerzona kolumna — po rozwinięciu pól collection w osobne kolumny */
export interface ExpandedColumn {
  fieldId: string;
  label: string;
  width?: string;
  type: 'regular' | 'childField';
  fieldDef?: any;
  childField?: any;
  parentFieldDef?: any;
  originalColumn: any;
  isSortable: boolean;
  isFilterable: boolean;
  isBoolean: boolean;
  isNumeric: boolean;
  /** FieldScope (0=Group, 1=ItemSystem, 2=ItemCalculated, 3=ItemGeneric) */
  fieldScope: number;
}

// ---------------------------------------------------------------------------
// Wspólne typy callbacków dla komponentów wierszy
// ---------------------------------------------------------------------------

export type FieldSource = 'system' | 'calculated' | 'generic';

export type RenderFieldInputFn = (
  fieldDef: any,
  value: string | undefined,
  onChange: (value: string | undefined) => void,
  disabled?: boolean,
  itemAllValues?: AllItemValues,
  /** ID pozycji — wymagane dla pól typu pliki (upload) */
  itemId?: string,
  /** ID definicji pola — wymagane dla pól typu pliki (upload) */
  fieldDefinitionId?: string,
  /** Pliki dołączone do pozycji */
  files?: import('../../types/costEstimate.types.new').CostEstimateItemFileWeb[] | null
) => React.ReactNode;

export type FormatDisplayValueFn = (
  value: string | undefined,
  fieldDef?: any
) => React.ReactNode;

export type GetColumnWidthFn = (
  fieldId: string,
  defaultWidth?: string,
  label?: string
) => number;
