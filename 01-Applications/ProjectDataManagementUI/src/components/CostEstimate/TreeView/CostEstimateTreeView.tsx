/**
 * Modern Cost Estimate Tree View - Prototype Design
 *
 * Features:
 * - Full hierarchy: ETAP → PODETAP → POZYCJA → KOMPONENT / OPCJA
 * - Inline editing with prototype-styled inputs
 * - Base fields as direct entity properties (name, quantity, unit, unitPriceNet, vatRate…)
 * - Additional fields from details.additionalFields schema
 * - Sort by column, search/filter across name and text additional fields
 * - Drag & drop reordering
 * - Real-time calculations
 */

import React, { useState, useMemo, useCallback, useContext, forwardRef, useImperativeHandle } from 'react';
import { useProjectUnits, useAddProjectUnit } from '../../../hooks/useProjectUnits';
import {
  Box,
  Flex,
  HStack,
  VStack,
  Text,
} from '@chakra-ui/react';
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
} from '@dnd-kit/core';
import type { DragEndEvent } from '@dnd-kit/core';
import {
  SortableContext,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import type {
  CostEstimateDetailsWeb,
  CostEstimateGroupWeb,
  CostEstimateAdditionalFieldWeb,
} from '../../../types/costEstimate.types.new';
import { AdditionalFieldType } from '../../../types/costEstimate.types.new';
import { getSchemaColumns, resolveTreeViewSchemaColumns, MIN_COL_WIDTHS } from '../../../utils/costEstimateFieldSchema';
import { calcTreeViewColumnWidths } from '../../../utils/calcTreeViewColumnWidths';
import { resolveAdditionalFieldDefinitions } from '../../../utils/additionalFieldHelpers';
import { getCostEstimateTotals, resolveCostEstimateCurrencySymbol } from '../../../utils/costEstimateUtils';
import { formatCurrency } from '../../../utils/formatters';
import { TreeViewHeader, TREE_VIEW_HEADER_HEIGHT } from './TreeViewHeader';
import { TreeViewRow } from './TreeViewRow';
import { AddInlineButton } from '../PrototypeActionButtons';
import { AuthContext } from '../../../context/AuthContext';

// ---------------------------------------------------------------------------
// Column definition types
// ---------------------------------------------------------------------------

import { getColumnFieldKey, isAlwaysVisibleColumn, type ColumnDef, type SortConfig } from './costEstimateColumnTypes';
export type { ColumnDef, SortConfig } from './costEstimateColumnTypes';
export { getColumnFieldKey, isAlwaysVisibleColumn };

// ---------------------------------------------------------------------------
// Base column definitions — fallback gdy API nie zwróci schematu
// ---------------------------------------------------------------------------

export const BASE_COLUMNS: ColumnDef[] = getSchemaColumns({
  fieldSchemas: [
    { id: 'name', costEstimateId: '', fieldName: 'Nazwa', fieldKey: 'name', fieldType: 100, isBasicField: true, isAdditionalField: false, order: 0, createdAt: '' },
    { id: 'actions', costEstimateId: '', fieldName: 'Akcje', fieldKey: 'actions', fieldType: 112, isBasicField: true, isAdditionalField: false, order: 1, createdAt: '' },
    { id: 'quantity', costEstimateId: '', fieldName: 'Ilość', fieldKey: 'quantity', fieldType: 101, isBasicField: true, isAdditionalField: false, order: 2, createdAt: '' },
    { id: 'unit', costEstimateId: '', fieldName: 'Jednostka', fieldKey: 'unit', fieldType: 102, isBasicField: true, isAdditionalField: false, order: 3, createdAt: '' },
    { id: 'unitPriceNet', costEstimateId: '', fieldName: 'Cena jednostkowa netto', fieldKey: 'unitPriceNet', fieldType: 103, isBasicField: true, isAdditionalField: false, order: 4, createdAt: '' },
    { id: 'vatRate', costEstimateId: '', fieldName: 'Stawka VAT', fieldKey: 'vatRate', fieldType: 104, isBasicField: true, isAdditionalField: false, order: 5, createdAt: '' },
    { id: 'unitPriceGross', costEstimateId: '', fieldName: 'Cena jednostkowa brutto', fieldKey: 'unitPriceGross', fieldType: 105, isBasicField: true, isAdditionalField: false, order: 6, createdAt: '' },
    { id: 'netValue', costEstimateId: '', fieldName: 'Wartość netto', fieldKey: 'netValue', fieldType: 106, isBasicField: true, isAdditionalField: false, order: 7, createdAt: '' },
    { id: 'grossValue', costEstimateId: '', fieldName: 'Wartość brutto', fieldKey: 'grossValue', fieldType: 107, isBasicField: true, isAdditionalField: false, order: 8, createdAt: '' },
    { id: 'vatValue', costEstimateId: '', fieldName: 'Wartość VAT', fieldKey: 'vatValue', fieldType: 108, isBasicField: true, isAdditionalField: false, order: 9, createdAt: '' },
    { id: 'isSelected', costEstimateId: '', fieldName: 'Sumuj', fieldKey: 'isSelected', fieldType: 109, isBasicField: true, isAdditionalField: false, order: 10, createdAt: '' },
    { id: 'isStageWork', costEstimateId: '', fieldName: 'Zakres harmonogramu', fieldKey: 'isStageWork', fieldType: 110, isBasicField: true, isAdditionalField: false, order: 11, createdAt: '' },
    { id: 'files', costEstimateId: '', fieldName: 'Plik', fieldKey: 'files', fieldType: 111, isBasicField: true, isAdditionalField: false, order: 12, createdAt: '' },
  ],
});

// ---------------------------------------------------------------------------
// Column visibility helpers (sessionStorage)
// ---------------------------------------------------------------------------

export const VISIBLE_COLS_KEY = (userId: string, costEstimateId: string) => `ce-visible-cols-${costEstimateId}-${userId}`;

function ensureAlwaysVisibleCols(cols: Set<string>, columns: ColumnDef[]): Set<string> {
  const next = new Set(cols);
  for (const col of columns) {
    if (isAlwaysVisibleColumn(col)) {
      next.add(col.id);
    }
  }
  return next;
}

export function loadVisibleCols(userId: string, costEstimateId: string, columns: ColumnDef[]): Set<string> {
  const allColIds = columns.map((c) => c.id);
  try {
    const raw = sessionStorage.getItem(VISIBLE_COLS_KEY(userId, costEstimateId));
    if (raw) {
      const parsed: string[] = JSON.parse(raw) as string[];
      return ensureAlwaysVisibleCols(
        new Set(parsed.filter((id) => allColIds.includes(id))),
        columns
      );
    }
  } catch { /* ignore */ }
  return new Set(allColIds);
}

export function saveVisibleCols(userId: string, costEstimateId: string, cols: Set<string>): void {
  try {
    sessionStorage.setItem(VISIBLE_COLS_KEY(userId, costEstimateId), JSON.stringify([...cols]));
  } catch { /* ignore */ }
}

// ---------------------------------------------------------------------------
// Props
// ---------------------------------------------------------------------------

interface CostEstimateTreeViewProps {
  details: CostEstimateDetailsWeb;
  currencySymbol?: string;
  isEditMode: boolean;
  tenantId: string;
  projectId: string;
  searchQuery: string;
  onSearchChange: (query: string) => void;
  /** External column visibility control (optional — if provided, component is controlled) */
  visibleColIds?: Set<string>;
  onToggleColVisibility?: (fieldId: string) => void;
  onFieldChange: (
    groupId: string,
    itemId: string | null,
    fieldId: string,
    value: string | number | boolean | null
  ) => void;
  onFieldAutosave?: (params: {
    entityType: 'group' | 'item';
    entityId: string;
    fieldValueId?: string | null;
    /** @deprecated Używaj additionalFieldId */
    fieldDefinitionId?: string;
    /** @deprecated Nie używany w nowym API */
    fieldType?: number;
    additionalFieldId?: string;
    fieldName?: string;
    fieldKind?: 'base' | 'additional';
    valueType: 'string' | 'numeric' | 'boolean' | 'date';
    value: string | undefined;
  }) => void;
  onAddGroup: () => void;
  onAddSubGroup: (parentGroupId: string) => void;
  onAddItem: (groupId: string) => void;
  onAddComponent: (groupId: string, itemId: string) => void;
  onAddOption: (groupId: string, itemId: string) => void;
  onDeleteGroup: (groupId: string) => void;
  onDeleteItem: (groupId: string, itemId: string) => void;
  onSelectOption: (groupId: string, itemId: string, optionId: string) => void;
  onUploadFiles: (itemId: string) => void;
  onReorderGroups: (groupIds: string[]) => void;
  onReorderItems: (groupId: string, itemOrders: Array<{ itemId: string; order: number }>) => void;
  onReorderItemChildren: (parentItemId: string, itemOrders: Array<{ itemId: string; order: number }>) => void;
  onToggleFieldVisibility: (fieldId: string) => void;
  onAddField: (label: string, fieldScope: number, fieldType: number) => void;
}

export interface CostEstimateTreeViewHandle {
  expandAll: () => void;
  collapseAll: () => void;
}

// ---------------------------------------------------------------------------
// Component
// ---------------------------------------------------------------------------

export const CostEstimateTreeView = forwardRef<
  CostEstimateTreeViewHandle,
  CostEstimateTreeViewProps
>(({
  details,
  currencySymbol: currencySymbolProp,
  isEditMode,
  tenantId,
  projectId,
  searchQuery,
  onSearchChange,
  visibleColIds: externalVisibleColIds,
  onToggleColVisibility: externalToggleColVisibility,
  onFieldChange,
  onFieldAutosave,
  onAddGroup,
  onAddSubGroup,
  onAddItem,
  onAddComponent,
  onAddOption,
  onDeleteGroup,
  onDeleteItem,
  onSelectOption,
  onUploadFiles,
  onReorderGroups,
  onReorderItems,
  onReorderItemChildren,
  onToggleFieldVisibility,
  onAddField,
}, ref) => {
  const currencySymbol = useMemo(
    () => currencySymbolProp ?? resolveCostEstimateCurrencySymbol(details),
    [currencySymbolProp, details.selectedCurrencySymbol, details.selectedCurrencyCode],
  );
  const { user } = useContext(AuthContext);
  const userId = user?.id ?? 'anonymous';

  const { data: unitsData } = useProjectUnits(tenantId, projectId);
  const projectUnits = useMemo(
    () => (unitsData ?? []).map((u) => u.code),
    [unitsData]
  );
  const addUnitMutation = useAddProjectUnit(tenantId, projectId);

  const handleAddProjectUnit = useCallback(
    (code: string) => {
      addUnitMutation.mutate({ code, name: code });
    },
    [addUnitMutation]
  );

  // Expanded state for groups
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(
    new Set(details.rootGroups.map((g) => g.id))
  );

  // Sort / filter state (searchQuery is managed by parent)
  const [sortConfig, setSortConfig] = useState<SortConfig | null>(null);

  // Column visibility state (persisted in sessionStorage — per cost estimate)
  const costEstimateId = details.id;
  const schemaColumns = useMemo(
    () => resolveTreeViewSchemaColumns(details, BASE_COLUMNS),
    [details]
  );

  const additionalFieldDefs = useMemo(
    () => resolveAdditionalFieldDefinitions(details),
    [details]
  );

  const allColIds = useMemo(() => schemaColumns.map((c) => c.id), [schemaColumns]);
  const [internalVisibleColIds, setInternalVisibleColIds] = useState<Set<string>>(() =>
    loadVisibleCols(userId, costEstimateId, schemaColumns)
  );

  // Use external controlled state if provided, otherwise internal
  const isExternallyControlled = externalVisibleColIds !== undefined && externalToggleColVisibility !== undefined;
  const visibleColIds = isExternallyControlled ? externalVisibleColIds : internalVisibleColIds;

  const contentColWidths = useMemo(
    () => calcTreeViewColumnWidths(details, schemaColumns, additionalFieldDefs),
    [details, schemaColumns, additionalFieldDefs]
  );

  const [manualColWidths, setManualColWidths] = useState<Record<string, number>>({});

  const colWidths = useMemo(() => {
    const merged: Record<string, number> = {};
    for (const colId of new Set([...Object.keys(contentColWidths), ...Object.keys(manualColWidths)])) {
      const contentWidth = contentColWidths[colId] ?? MIN_COL_WIDTHS[colId] ?? 60;
      const manualWidth = manualColWidths[colId];
      const minWidth = MIN_COL_WIDTHS[colId] ?? 60;
      merged[colId] = Math.max(minWidth, contentWidth, manualWidth ?? 0);
    }
    return merged;
  }, [contentColWidths, manualColWidths]);

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  const toggleGroup = useCallback((groupId: string) => {
    setExpandedGroups((prev) => {
      const next = new Set(prev);
      if (next.has(groupId)) next.delete(groupId);
      else next.add(groupId);
      return next;
    });
  }, []);

  const expandAll = useCallback(() => {
    const allIds = new Set<string>();
    const collectIds = (groups: CostEstimateGroupWeb[]) => {
      for (const g of groups) {
        allIds.add(g.id);
        collectIds(g.childGroups || []);
      }
    };
    collectIds(details.rootGroups);
    setExpandedGroups(allIds);
  }, [details.rootGroups]);

  const collapseAll = useCallback(() => {
    setExpandedGroups(new Set());
  }, []);

  useImperativeHandle(ref, () => ({
    expandAll,
    collapseAll,
  }), [expandAll, collapseAll]);

  const handleResizeColumn = useCallback((colId: string, newWidth: number) => {
    setManualColWidths((prev) => ({
      ...prev,
      [colId]: Math.max(newWidth, MIN_COL_WIDTHS[colId] ?? 60),
    }));
  }, []);

  const handleDragEnd = useCallback(
    (event: DragEndEvent) => {
      const { active, over } = event;
      if (!over || active.id === over.id) return;

      const activeId = String(active.id);
      const overId = String(over.id);

      const activeGroup = details.rootGroups.find((g) => g.id === activeId);
      const overGroup = details.rootGroups.find((g) => g.id === overId);

      if (activeGroup && overGroup) {
        const oldIndex = details.rootGroups.findIndex((g) => g.id === activeId);
        const newIndex = details.rootGroups.findIndex((g) => g.id === overId);
        const newGroups = [...details.rootGroups];
        const [movedGroup] = newGroups.splice(oldIndex, 1);
        newGroups.splice(newIndex, 0, movedGroup);
        onReorderGroups(newGroups.map((g) => g.id));
      } else {
        const parentGroup = details.rootGroups.find(
          (g) =>
            g.items.some((i) => i.id === activeId) &&
            g.items.some((i) => i.id === overId)
        );
        if (parentGroup) {
          const oldIndex = parentGroup.items.findIndex((i) => i.id === activeId);
          const newIndex = parentGroup.items.findIndex((i) => i.id === overId);
          const newItems = [...parentGroup.items];
          const [movedItem] = newItems.splice(oldIndex, 1);
          newItems.splice(newIndex, 0, movedItem);
          onReorderItems(parentGroup.id, newItems.map((i, index) => ({ itemId: i.id, order: index })));
        }
      }
    },
    [details.rootGroups, onReorderGroups, onReorderItems]
  );

  // -------------------------------------------------------------------------
  // Sort handler: toggle direction on same field, set asc on new field
  // -------------------------------------------------------------------------
  const handleSort = useCallback((field: string) => {
    setSortConfig((prev) => {
      if (prev && prev.field === field) {
        return { field, direction: prev.direction === 'asc' ? 'desc' : 'asc' };
      }
      return { field, direction: 'asc' };
    });
  }, []);

  // -------------------------------------------------------------------------
  // Build column definitions from field schema
  // -------------------------------------------------------------------------
  const baseColumns = useMemo(
    () =>
      schemaColumns
        .filter((col) => !col.isAdditional)
        .map((col) => ({
          ...col,
          width: `${colWidths[col.id] ?? parseInt(col.width ?? '100', 10)}px`,
        })),
    [schemaColumns, colWidths]
  );

  const additionalColumns = useMemo(
    () =>
      schemaColumns
        .filter((col) => col.isAdditional)
        .map((col) => ({
          ...col,
          width: `${colWidths[col.id] ?? parseInt(col.width ?? '130', 10)}px`,
        })),
    [schemaColumns, colWidths]
  );

  // -------------------------------------------------------------------------
  // Column visibility toggle (sessionStorage + external callback)
  // -------------------------------------------------------------------------
  const handleToggleColVisibility = useCallback(
    (fieldId: string) => {
      const col = schemaColumns.find((c) => c.id === fieldId);
      if (col && isAlwaysVisibleColumn(col)) {
        return;
      }
      if (isExternallyControlled) {
        externalToggleColVisibility!(fieldId);
      } else {
        setInternalVisibleColIds((prev) => {
          const next = new Set(prev);
          if (next.has(fieldId)) {
            next.delete(fieldId);
          } else {
            next.add(fieldId);
          }
          saveVisibleCols(userId, costEstimateId, next);
          return next;
        });
      }
      onToggleFieldVisibility(fieldId);
    },
    [userId, costEstimateId, onToggleFieldVisibility, isExternallyControlled, externalToggleColVisibility, schemaColumns]
  );

  // Filtered columns passed to child components
  const visibleBaseColumns = useMemo(
    () => baseColumns.filter((c) => visibleColIds.has(c.id) || isAlwaysVisibleColumn(c)),
    [baseColumns, visibleColIds]
  );

  const visibleAdditionalColumns = useMemo(
    () => additionalColumns.filter((c) => {
      // New additional columns (not yet in saved preferences) are visible by default
      if (visibleColIds.size === allColIds.length && !additionalColumns.some((a) => visibleColIds.has(a.id))) {
        return true;
      }
      return visibleColIds.has(c.id);
    }),
    [additionalColumns, visibleColIds, allColIds]
  );

  // -------------------------------------------------------------------------
  // Total columns width — for horizontal scroll and sticky name column
  // -------------------------------------------------------------------------
  const totalColumnsWidth = useMemo(() => {
    const nameCol = colWidths['name'] ?? 200;
    // 'actions' jest teraz w BASE_COLUMNS, więc jego width idzie w baseCols
    const baseCols = visibleBaseColumns
      .filter((c) => c.id !== 'name')
      .reduce((sum, c) => sum + parseInt(c.width ?? '100'), 0);
    const addCols = visibleAdditionalColumns
      .reduce((sum, c) => sum + parseInt(c.width ?? '130'), 0);
    return nameCol + baseCols + addCols + 28;
  }, [visibleBaseColumns, visibleAdditionalColumns, colWidths]);

  // -------------------------------------------------------------------------
  // Filtered groups (search)
  // -------------------------------------------------------------------------
  const filteredGroups = useMemo(() => {
    if (!searchQuery.trim()) return details.rootGroups;

    const q = searchQuery.trim().toLowerCase();

    /** Returns true if the group (or any of its descendants) matches the query */
    const groupMatches = (group: CostEstimateGroupWeb): boolean => {
      if (group.name.toLowerCase().includes(q)) return true;

      // Check string additional field values on group
      const additionalMatch = (group.additionalFieldValues ?? []).some((fv) => {
        const fieldDef = (details.additionalFields ?? []).find(
          (af: CostEstimateAdditionalFieldWeb) => af.id === fv.additionalFieldId
        );
        if (!fieldDef || fieldDef.fieldType !== AdditionalFieldType.String) return false;
        return (fv.stringValue ?? '').toLowerCase().includes(q);
      });
      if (additionalMatch) return true;

      // Check items
      const itemMatch = (group.items ?? []).some((item) => {
        if (item.name.toLowerCase().includes(q)) return true;
        return (item.additionalFieldValues ?? []).some((fv) => {
          const fieldDef = (details.additionalFields ?? []).find(
            (af: CostEstimateAdditionalFieldWeb) => af.id === fv.additionalFieldId
          );
          if (!fieldDef || fieldDef.fieldType !== AdditionalFieldType.String) return false;
          return (fv.stringValue ?? '').toLowerCase().includes(q);
        });
      });
      if (itemMatch) return true;

      // Check child groups
      return (group.childGroups ?? []).some(groupMatches);
    };

    return details.rootGroups.filter(groupMatches);
  }, [details.rootGroups, details.additionalFields, searchQuery]);

  // -------------------------------------------------------------------------
  // Sorted + filtered groups (sorted by sortConfig)
  // -------------------------------------------------------------------------
  const filteredAndSortedGroups = useMemo(() => {
    let groups = filteredGroups;

    if (sortConfig) {
      const { field, direction } = sortConfig;
      const sign = direction === 'asc' ? 1 : -1;

      groups = [...groups].sort((a, b) => {
        let aVal: number | string = '';
        let bVal: number | string = '';

        switch (field) {
          case 'name':
            aVal = a.name ?? '';
            bVal = b.name ?? '';
            break;
          case 'netValue':
            aVal = a.totalNet ?? 0;
            bVal = b.totalNet ?? 0;
            break;
          case 'grossValue':
            aVal = a.totalGross ?? 0;
            bVal = b.totalGross ?? 0;
            break;
          case 'vatValue':
            aVal = a.totalVat ?? 0;
            bVal = b.totalVat ?? 0;
            break;
          // Item-only fields — groups have neutral sort value (0)
          case 'quantity':
          case 'unit':
          case 'unitPriceNet':
          case 'vatRate':
          case 'unitPriceGross':
          case 'isSelected':
          case 'isStageWork':
          case 'files':
            aVal = 0;
            bVal = 0;
            break;
          default: {
            const aFv = (a.additionalFieldValues ?? []).find((v) => v.additionalFieldId === field);
            const bFv = (b.additionalFieldValues ?? []).find((v) => v.additionalFieldId === field);
            aVal = aFv?.stringValue ?? (aFv?.decimalValue !== undefined ? aFv.decimalValue : '');
            bVal = bFv?.stringValue ?? (bFv?.decimalValue !== undefined ? bFv.decimalValue : '');
          }
        }

        if (typeof aVal === 'number' && typeof bVal === 'number') {
          return (aVal - bVal) * sign;
        }
        return String(aVal).localeCompare(String(bVal)) * sign;
      });
    }

    return groups;
  }, [filteredGroups, sortConfig]);

  // -------------------------------------------------------------------------
  // Grand totals for summary row
  // -------------------------------------------------------------------------
  const totals = useMemo(() => getCostEstimateTotals(details), [details]);

  // ---------------------------------------------------------------------------
  // Summary row renderer
  // ---------------------------------------------------------------------------

  const renderSummaryRow = (variant: 'top' | 'bottom') => {
    const isTop = variant === 'top';
    const bg = isTop ? 'neutral.25' : 'neutral.50';

    return (
    <Flex
      align="center"
      minH="52px"
      borderTop={isTop ? '1px solid' : '2px solid'}
      borderBottom={isTop ? '1px solid' : undefined}
      borderColor="neutral.300"
      bg={bg}
      minW={`${totalColumnsWidth}px`}
      role="row"
      position="sticky"
      top={isTop ? `${TREE_VIEW_HEADER_HEIGHT}px` : undefined}
      bottom={isTop ? undefined : 0}
      zIndex={9}
      flexShrink={0}
      boxShadow={
        isTop
          ? '0 2px 4px rgba(20,33,47,0.06)'
          : '0 -4px 6px -1px rgba(20,33,47,0.08)'
      }
    >
      {/* Sticky Name cell */}
      <Box
        flex="0 0 auto"
        w={`${colWidths['name'] ?? 200}px`}
        position="sticky"
        left={0}
        zIndex={12}
        bg={bg}
        px={3.5}
        display="flex"
        alignItems="center"
      >
        <Text fontSize="xs" fontWeight="bold" color="neutral.700" textTransform="uppercase" letterSpacing="0.05em">
          Razem
        </Text>
      </Box>

      {/* Actions sticky spacer — matches group/item row layout */}
      <Box
        flex="0 0 auto"
        w={`${colWidths['actions'] ?? 120}px`}
        position="sticky"
        left={`${colWidths['name'] ?? 200}px`}
        zIndex={12}
        bg={bg}
      />

      {/* Base columns rendered in natural order — values only in netValue/grossValue columns */}
      {visibleBaseColumns
        .filter((c) => c.id !== 'name' && c.id !== 'actions')
        .map((col) => {
          const w = col.width ?? '100px';
          const fieldKey = getColumnFieldKey(col);

          if (fieldKey === 'netValue') {
            return (
              <Flex key={col.id} flex="0 0 auto" w={w} justify="flex-end" pr={2}>
                <Text fontSize="sm" fontWeight="bold" color="neutral.800"
                  sx={{ fontVariantNumeric: 'tabular-nums' }}>
                  {formatCurrency(totals.net, currencySymbol)}
                </Text>
              </Flex>
            );
          }

          if (fieldKey === 'grossValue') {
            return (
              <Flex key={col.id} flex="0 0 auto" w={w} justify="flex-end" pr={2}>
                <Text fontSize="sm" fontWeight="bold" color="neutral.800"
                  sx={{ fontVariantNumeric: 'tabular-nums' }}>
                  {formatCurrency(totals.gross, currencySymbol)}
                </Text>
              </Flex>
            );
          }

          // All other base columns — empty cell
          return <Box key={col.id} flex="0 0 auto" w={w} />;
        })}

      {/* Additional columns — empty */}
      {visibleAdditionalColumns.map((col) => (
        <Box key={col.id} flex="0 0 auto" w={col.width ?? '130px'} />
      ))}
    </Flex>
    );
  };

  return (
    <Box
      bg="white"
      border="1px solid"
      borderColor="neutral.200"
      borderRadius="14px"
      boxShadow="0 1px 2px rgba(20,33,47,.05), 0 1px 3px rgba(20,33,47,.04)"
      overflow="hidden"
      position="relative"
      display="flex"
      flexDirection="column"
      h="100%"
      minH={0}
    >
      <Box
        flex="1"
        minH={0}
        overflow="auto"
        minW="100%"
      >
        <Box minH="100%" display="flex" flexDirection="column">
          <TreeViewHeader
            baseColumns={visibleBaseColumns}
            additionalColumns={visibleAdditionalColumns}
            sortConfig={sortConfig}
            onSort={handleSort}
            totalColumnsWidth={totalColumnsWidth}
            onResizeColumn={handleResizeColumn}
            nameColWidth={colWidths['name'] ?? 200}
            actionsColWidth={colWidths['actions'] ?? 120}
          />

          {renderSummaryRow('top')}

          <Box flex="1" minH={0}>
            <DndContext
              sensors={sensors}
              collisionDetection={closestCenter}
              onDragEnd={handleDragEnd}
            >
              <SortableContext
                items={details.rootGroups.map((g) => g.id)}
                strategy={verticalListSortingStrategy}
              >
                <VStack spacing={0} align="stretch">
                  {filteredAndSortedGroups.map((group, index) => (
                    <TreeViewRow
                      key={group.id}
                      group={group}
                      currencySymbol={currencySymbol}
                      level={0}
                      isExpanded={expandedGroups.has(group.id)}
                      isEditMode={isEditMode}
                      baseColumns={visibleBaseColumns}
                      additionalColumns={visibleAdditionalColumns}
                      additionalFieldDefs={additionalFieldDefs}
                      searchQuery={searchQuery}
                      sortConfig={sortConfig}
                      onToggle={() => toggleGroup(group.id)}
                      onFieldChange={onFieldChange}
                      onFieldAutosave={onFieldAutosave}
                      onAddItem={() => onAddItem(group.id)}
                      onAddSubGroup={() => onAddSubGroup(group.id)}
                      onAddSubGroupFromRow={(parentGroupId) => onAddSubGroup(parentGroupId)}
                      onAddComponent={(itemId) => onAddComponent(group.id, itemId)}
                      onAddOption={(itemId) => onAddOption(group.id, itemId)}
                      onDeleteGroup={() => onDeleteGroup(group.id)}
                      onDeleteItem={(itemId) => onDeleteItem(group.id, itemId)}
                      onSelectOption={(gId, itemId, optionId) =>
                        onSelectOption(gId, itemId, optionId)
                      }
                      onUploadFiles={(itemId) => onUploadFiles(itemId)}
                      projectUnits={projectUnits}
                      onAddProjectUnit={handleAddProjectUnit}
                      isAddingUnit={addUnitMutation.isPending}
                      totalColumnsWidth={totalColumnsWidth}
                      nameColWidth={colWidths['name'] ?? 200}
                      actionsColWidth={colWidths['actions'] ?? 120}
                      isLast={index === filteredAndSortedGroups.length - 1}
                      onReorderItemChildren={onReorderItemChildren}
                      onReorderItems={onReorderItems}
                    />
                  ))}
                </VStack>
              </SortableContext>
            </DndContext>
          </Box>

          {renderSummaryRow('bottom')}
        </Box>
      </Box>

      {/* Footer */}
      {isEditMode && (
        <Flex
          px={4}
          py={3}
          bg="neutral.50"
          borderTop="1px solid"
          borderColor="neutral.200"
          justify="space-between"
          align="center"
          flexShrink={0}
        >
          <AddInlineButton onClick={onAddGroup}>
            Dodaj etap
          </AddInlineButton>

          <HStack spacing={2}>
            <Text fontSize="xs" color="neutral.500">
              {details.rootGroups.length}{' '}
              {details.rootGroups.length === 1 ? 'etap' : 'etapów'}
            </Text>
          </HStack>
        </Flex>
      )}
    </Box>
  );
});

CostEstimateTreeView.displayName = 'CostEstimateTreeView';
