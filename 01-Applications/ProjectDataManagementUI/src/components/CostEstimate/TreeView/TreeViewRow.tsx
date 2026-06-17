/**
 * Tree View Row — Single group or item row with prototype styling
 *
 * Supports full hierarchy: ETAP → PODETAP → POZYCJA → KOMPONENT / OPCJA
 *
 * Base fields are now direct entity properties (name, quantity, unit, unitPriceNet, vatRate…).
 * Additional fields come from additionalFieldDefs + entity.additionalFieldValues.
 */

import React, { useMemo, useCallback, useState } from 'react';
import {
  Flex,
  Box,
  Text,
  Checkbox,
  IconButton,
  Tooltip,
} from '@chakra-ui/react';
import type { FlexProps } from '@chakra-ui/react';
import {
  DndContext,
  closestCenter,
  PointerSensor,
  KeyboardSensor,
  useSensor,
  useSensors,
} from '@dnd-kit/core';
import type { DragEndEvent } from '@dnd-kit/core';
import {
  SortableContext,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
  useSortable,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import type {
  CostEstimateGroupWeb,
  CostEstimateItemWeb,
  CostEstimateAdditionalFieldWeb,
} from '../../../types/costEstimate.types.new';
import { AdditionalFieldType, CostEstimateFieldType, isTemporaryId } from '../../../types/costEstimate.types.new';
import type { ColumnDef, SortConfig } from './CostEstimateTreeView';
import { getColumnFieldKey } from './costEstimateColumnTypes';
import { computeItemFieldFlags, areItemAdditionalFieldsLocked } from '../../../utils/costEstimateItemFlags';
import {
  getAdditionalFieldValue,
  getAdditionalFieldValueAsString,
  getAdditionalFieldAutosaveValueType,
  formatAdditionalFieldAutosaveValue,
} from '../../../utils/additionalFieldHelpers';
import { getColumnCellJustify } from '../../../utils/calcTreeViewColumnWidths';
import { getBaseFieldPlaceholder } from '../../../utils/costEstimateFieldSchema';
import { formatCurrency } from '../../../utils/formatters';
import { AdditionalFieldInput } from '../AdditionalFieldInput';
import {
  PrototypeTextInput,
  PrototypeNumberInput,
  PrototypeTag,
  PrototypeDot,
} from '../PrototypeInputs';
import {
  ChevronButton,
  DragHandle,
  GhostActionButton,
  AddInlineButton,
} from '../PrototypeActionButtons';
import { UnitCombobox } from '../UnitCombobox';
import { Trash2, Upload, FileText } from 'lucide-react';
import {
  ADD_ROW_SURFACE,
  getGroupRowSurface,
  getItemRowSurface,
  TreeViewRowBackground,
  TreeViewStickyCell,
  treeViewRowHoverStyles,
} from './treeViewRowSurfaces';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function isNetValueColumn(col: ColumnDef): boolean {
  const fieldKey = getColumnFieldKey(col);
  return fieldKey === 'netValue' || col.schemaFieldType === CostEstimateFieldType.NetValue;
}

function isGrossValueColumn(col: ColumnDef): boolean {
  const fieldKey = getColumnFieldKey(col);
  return fieldKey === 'grossValue' || col.schemaFieldType === CostEstimateFieldType.GrossValue;
}

const TREE_INDENT_STEP = 28;

/** Etap (level 0) bez wcięcia; podetap zagnieżdżony wcięty jak pozycja na tej głębokości. */
function getTreeIndentPx(level: number): number {
  if (level <= 0) {
    return 0;
  }
  return (level + 1) * TREE_INDENT_STEP;
}

interface GroupTotalValueCellProps {
  value: number | undefined | null;
  level: number;
  variant: 'net' | 'gross';
  width: string;
  justify: FlexProps['justify'];
  currencySymbol: string;
}

/** Read-only sum cell for etap / podetap — aligned with item inputs and summary row */
const GroupTotalValueCell: React.FC<GroupTotalValueCellProps> = ({
  value,
  level,
  variant,
  width,
  justify,
  currencySymbol,
}) => {
  const hasValue = value !== undefined && value !== null;
  const isPositive = (value ?? 0) > 0;

  return (
    <Flex
      flex="0 0 auto"
      w={width}
      justify={justify}
      align="center"
      pr={2}
      position="relative"
      zIndex={1}
    >
      <Text
        fontSize="sm"
        fontWeight={level === 0 ? 'bold' : 'semibold'}
        color={variant === 'net'
          ? (isPositive ? 'neutral.800' : 'neutral.400')
          : (isPositive ? 'neutral.600' : 'neutral.300')}
        sx={{ fontVariantNumeric: 'tabular-nums' }}
        aria-label={variant === 'net' ? 'Suma netto etapu' : 'Suma brutto etapu'}
      >
        {hasValue ? formatCurrency(value, currencySymbol) : '—'}
      </Text>
    </Flex>
  );
};

/** Placeholder em-dash cell for item-only columns displayed in group rows */
const EmptyCell: React.FC<{ width: string }> = ({ width }) => (
  <Flex flex="0 0 auto" w={width} justify="flex-end" pr={2}>
    <Text fontSize="xs" color="neutral.300">
      —
    </Text>
  </Flex>
);

// ---------------------------------------------------------------------------
// Autosave param types (shared)
// ---------------------------------------------------------------------------

type AutosaveParams = {
  entityType: 'group' | 'item';
  entityId: string;
  fieldValueId?: string | null;
  /** @deprecated */
  fieldDefinitionId?: string;
  /** @deprecated */
  fieldType?: number;
  additionalFieldId?: string;
  fieldName?: string;
  fieldKind?: 'base' | 'additional';
  valueType: 'string' | 'numeric' | 'boolean' | 'date';
  value: string | undefined;
};

// ---------------------------------------------------------------------------
// TreeViewRow props
// ---------------------------------------------------------------------------

export interface TreeViewRowProps {
  group: CostEstimateGroupWeb;
  currencySymbol: string;
  level: number;
  isExpanded: boolean;
  isEditMode: boolean;
  baseColumns: ColumnDef[];
  additionalColumns: ColumnDef[];
  additionalFieldDefs: CostEstimateAdditionalFieldWeb[];
  searchQuery: string;
  sortConfig: SortConfig | null;
  projectUnits: string[];
  onAddProjectUnit?: (code: string) => void;
  isAddingUnit?: boolean;
  onToggle: () => void;
  onFieldChange: (
    groupId: string,
    itemId: string | null,
    fieldId: string,
    value: string | number | boolean | null
  ) => void;
  onFieldAutosave?: (params: AutosaveParams) => void;
  onAddItem: () => void;
  onAddSubGroup: () => void;
  onAddItemFromRow: (groupId: string) => void;
  onAddSubGroupFromRow: (parentGroupId: string) => void;
  onDeleteGroupFromRow: (groupId: string) => void;
  onAddComponent: (itemId: string) => void;
  onAddOption: (itemId: string) => void;
  onAddComponentFromRow: (groupId: string, itemId: string) => void;
  onAddOptionFromRow: (groupId: string, itemId: string) => void;
  onDeleteGroup: () => void;
  /** @param itemId ID elementu do usunięcia (pozycji, komponentu lub opcji) */
  onDeleteItem: (itemId: string) => void;
  onDeleteItemFromRow: (groupId: string, itemId: string) => void;
  onSelectOption: (groupId: string, itemId: string, optionId: string) => void;
  onUploadFiles: (itemId: string) => void;
  onReorderItemChildren: (parentItemId: string, itemOrders: Array<{ itemId: string; order: number }>) => void;
  onReorderItems: (groupId: string, itemOrders: Array<{ itemId: string; order: number }>) => void;
  isLast: boolean;
  totalColumnsWidth: number;
  nameColWidth: number;
  actionsColWidth: number;
}

/** Minimalna szerokość obszaru z przyciskami „Dodaj pozycję / podetap”. */
const ADD_ROW_CONTROLS_MIN_WIDTH = 300;

function getLeadingStickyWidth(nameColWidth: number, actionsColWidth: number): number {
  return nameColWidth + actionsColWidth;
}

// ---------------------------------------------------------------------------
// TreeViewRow component
// ---------------------------------------------------------------------------

export const TreeViewRow: React.FC<TreeViewRowProps> = ({
  group,
  currencySymbol,
  level,
  isExpanded,
  isEditMode,
  baseColumns,
  additionalColumns,
  additionalFieldDefs,
  searchQuery,
  sortConfig,
  onToggle,
  onFieldChange,
  onFieldAutosave,
  onAddItem,
  onAddSubGroup,
  onAddItemFromRow,
  onAddSubGroupFromRow,
  onDeleteGroupFromRow,
  onAddComponent,
  onAddOption,
  onAddComponentFromRow,
  onAddOptionFromRow,
  onDeleteGroup,
  onDeleteItem,
  onDeleteItemFromRow,
  onSelectOption,
  onUploadFiles,
  onReorderItemChildren,
  onReorderItems,
  isLast,
  totalColumnsWidth,
  projectUnits,
  onAddProjectUnit,
  isAddingUnit,
  nameColWidth,
  actionsColWidth,
}) => {
  const leadingStickyWidth = getLeadingStickyWidth(nameColWidth, actionsColWidth);
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: group.id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  const hasSubGroups = (group.childGroups?.length ?? 0) > 0;
  const groupTagLevel = level === 0 ? 0 : 1;
  const indentSize = getTreeIndentPx(level);

  const { bg: rowSurfaceBg, hoverBg: rowSurfaceHoverBg } = getGroupRowSurface(level);

  // -------------------------------------------------------------------------
  // Autosave helpers for group
  // -------------------------------------------------------------------------

  const triggerGroupBaseAutosave = useCallback(
    (fieldName: string, valueType: AutosaveParams['valueType'], value: string | undefined) => {
      if (!onFieldAutosave || isTemporaryId(group.id)) return;
      onFieldAutosave({
        entityType: 'group',
        entityId: group.id,
        fieldName,
        fieldKind: 'base',
        valueType,
        value,
      });
    },
    [onFieldAutosave, group.id]
  );

  const triggerGroupAdditionalAutosave = useCallback(
    (
      additionalFieldId: string,
      fieldDef: CostEstimateAdditionalFieldWeb,
      value: string | undefined
    ) => {
      if (!onFieldAutosave || isTemporaryId(group.id)) return;
      const existing = getAdditionalFieldValue(group.additionalFieldValues ?? [], additionalFieldId);
      const fieldValueId = existing?.id && !isTemporaryId(existing.id) ? existing.id : null;
      const valueType = getAdditionalFieldAutosaveValueType(fieldDef.fieldType);
      onFieldAutosave({
        entityType: 'group',
        entityId: group.id,
        fieldValueId,
        additionalFieldId,
        fieldKind: 'additional',
        valueType,
        value,
      });
    },
    [onFieldAutosave, group.id, group.additionalFieldValues]
  );

  // -------------------------------------------------------------------------
  // Sorted items
  // -------------------------------------------------------------------------

  const sortedItems = useMemo(() => {
    const items = [...(group.items ?? [])];
    if (!sortConfig) return items;
    const { field, direction } = sortConfig;
    const sign = direction === 'asc' ? 1 : -1;

    return items.sort((a, b) => {
      let aVal: number | string = '';
      let bVal: number | string = '';

      switch (field) {
        case 'name':
          aVal = a.name ?? '';
          bVal = b.name ?? '';
          break;
        case 'quantity':
          aVal = a.quantity ?? 0;
          bVal = b.quantity ?? 0;
          break;
        case 'unit':
          aVal = a.unit ?? '';
          bVal = b.unit ?? '';
          break;
        case 'unitPriceNet':
          aVal = a.unitPriceNet ?? 0;
          bVal = b.unitPriceNet ?? 0;
          break;
        case 'vatRate':
          aVal = a.vatRate ?? 0;
          bVal = b.vatRate ?? 0;
          break;
        case 'unitPriceGross':
          aVal = a.unitPriceGross ?? 0;
          bVal = b.unitPriceGross ?? 0;
          break;
        case 'netValue':
          aVal = a.netValue ?? 0;
          bVal = b.netValue ?? 0;
          break;
        case 'grossValue':
          aVal = a.grossValue ?? 0;
          bVal = b.grossValue ?? 0;
          break;
        case 'vatValue':
          aVal = a.vatValue ?? 0;
          bVal = b.vatValue ?? 0;
          break;
        case 'isSelected':
          aVal = a.isSelected ? 1 : 0;
          bVal = b.isSelected ? 1 : 0;
          break;
        case 'isStageWork':
          aVal = a.isStageWork ? 1 : 0;
          bVal = b.isStageWork ? 1 : 0;
          break;
        default: {
          // Additional field?
          const aFv = getAdditionalFieldValue(a.additionalFieldValues ?? [], field);
          const bFv = getAdditionalFieldValue(b.additionalFieldValues ?? [], field);
          aVal = aFv?.stringValue ?? aFv?.decimalValue ?? '';
          bVal = bFv?.stringValue ?? bFv?.decimalValue ?? '';
        }
      }

      if (typeof aVal === 'number' && typeof bVal === 'number') {
        return (aVal - bVal) * sign;
      }
      return String(aVal).localeCompare(String(bVal)) * sign;
    });
  }, [group.items, sortConfig]);

  // Sensors for item drag&drop (within this group)
  const itemSensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  // Handle drag end for items within this group
  const handleItemDragEnd = useCallback(
    (event: DragEndEvent) => {
      const { active, over } = event;
      if (!over || active.id === over.id) return;

      const activeId = String(active.id);
      const overId = String(over.id);

      // Find the item indices in sortedItems
      const oldIndex = sortedItems.findIndex((i) => i.id === activeId);
      const newIndex = sortedItems.findIndex((i) => i.id === overId);

      if (oldIndex !== -1 && newIndex !== -1) {
        const newItems = [...sortedItems];
        const [movedItem] = newItems.splice(oldIndex, 1);
        newItems.splice(newIndex, 0, movedItem);
        onReorderItems(group.id, newItems.map((i, index) => ({ itemId: i.id, order: index })));
      }
    },
    [sortedItems, group.id, onReorderItems]
  );

  // -------------------------------------------------------------------------
  // Sorted child groups — sort by the same field respecting hierarchy
  // -------------------------------------------------------------------------

  const sortedChildGroups = useMemo(() => {
    const groups = [...(group.childGroups ?? [])];
    if (!sortConfig) return groups;
    const { field, direction } = sortConfig;
    const sign = direction === 'asc' ? 1 : -1;

    return groups.sort((a, b) => {
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
  }, [group.childGroups, sortConfig]);

  // -------------------------------------------------------------------------
  // Render additional cells for a group row
  // -------------------------------------------------------------------------

  const renderGroupAdditionalCells = () => {
    return additionalColumns.map((col) => {
      const fieldDef = additionalFieldDefs.find((f) => f.id === col.id);
      if (!fieldDef) {
        return <Box key={col.id} flex="0 0 auto" w={col.width ?? '130px'} />;
      }

      const cellJustify = getColumnCellJustify(col.textAlign);

      return (
        <Flex
          key={col.id}
          flex="0 0 auto"
          w={col.width ?? '130px'}
          justify={cellJustify}
          align="center"
          px={1}
          pr={col.textAlign === 'right' ? 2 : 1}
        >
          <AdditionalFieldInput
            field={fieldDef}
            fieldValues={group.additionalFieldValues ?? []}
            isDisabled={!isEditMode}
            blendWithRow
            valueAlign={col.textAlign === 'left' ? 'left' : 'right'}
            onChange={(value) => {
              onFieldChange(group.id, null, col.id, value);
              triggerGroupAdditionalAutosave(
                col.id,
                fieldDef,
                formatAdditionalFieldAutosaveValue(value)
              );
            }}
          />
        </Flex>
      );
    });
  };

  // -------------------------------------------------------------------------
  // Render base cells for group row — totals in net/gross columns, empty elsewhere
  // -------------------------------------------------------------------------

  const renderGroupBaseCells = () => {
    // Iterate through all base columns (except sticky name + actions) in order,
    // matching the item row layout
    return baseColumns
      .filter((c) => c.id !== 'name' && c.id !== 'actions')
      .map((col) => {
        const w = col.width ?? '100px';
        const cellJustify = getColumnCellJustify(col.textAlign);

        if (isNetValueColumn(col)) {
          return (
            <GroupTotalValueCell
              key={col.id}
              value={group.totalNet}
              level={level}
              variant="net"
              width={w}
              justify={cellJustify}
              currencySymbol={currencySymbol}
            />
          );
        }

        if (isGrossValueColumn(col)) {
          return (
            <GroupTotalValueCell
              key={col.id}
              value={group.totalGross}
              level={level}
              variant="gross"
              width={w}
              justify={cellJustify}
              currencySymbol={currencySymbol}
            />
          );
        }

        // All other columns are item-only → render empty cell
        return <EmptyCell key={col.id} width={w} />;
      });
  };

  return (
    <Box ref={setNodeRef} style={style} data-ce-group-id={group.id}>
      {/* Group Row */}
      <Flex
        align="center"
        minH="46px"
        minW={`${totalColumnsWidth}px`}
        borderBottom={isLast && !isExpanded && !hasSubGroups ? 'none' : '1px solid'}
        borderColor="neutral.100"
        _hover={treeViewRowHoverStyles(rowSurfaceHoverBg)}
        px={3.5}
        py={2}
        position="relative"
        role="row"
        className="trow"
      >
        <TreeViewRowBackground bg={rowSurfaceBg} />
        <TreeViewStickyCell
          surfaceBg={rowSurfaceBg}
          left={0}
          width={`${nameColWidth}px`}
          gap={2}
          pl={indentSize > 0 ? `${indentSize}px` : undefined}
        >
          {isEditMode && (
            <Box {...attributes} {...listeners}>
              <DragHandle isDragging={isDragging} blendWithRow />
            </Box>
          )}

          <ChevronButton
            isExpanded={isExpanded}
            onClick={onToggle}
            isLeaf={group.items.length === 0 && !hasSubGroups}
            blendWithRow
          />

          <PrototypeTag level={groupTagLevel} />
          <PrototypeDot level={groupTagLevel} />

          <PrototypeTextInput
            value={group.name ?? ''}
            onChange={(e) => {
              const v = e.target.value;
              onFieldChange(group.id, null, 'name', v);
              triggerGroupBaseAutosave('name', 'string', v);
            }}
            isGroup
            isStage={level === 0}
            isDisabled={!isEditMode}
            placeholder={level === 0 ? 'Nazwa etapu' : 'Nazwa podetapu'}
            blendWithRow
          />
        </TreeViewStickyCell>

        {/* Actions (sticky, right after name) */}
        <TreeViewStickyCell
          surfaceBg={rowSurfaceBg}
          left={`${nameColWidth}px`}
          width={`${actionsColWidth}px`}
          justify="flex-end"
          gap={0.5}
          pr={2}
        >
          <GhostActionButton
            label="Dodaj pozycję"
            icon={<Box as="span" fontSize="sm" fontWeight="bold" lineHeight="1">P+</Box>}
            variant="add"
            onClick={onAddItem}
            isDisabled={!isEditMode}
            blendWithRow
          />
          <GhostActionButton
            label="Dodaj podetap"
            icon={<Box as="span" fontSize="sm" fontWeight="bold" lineHeight="1">E+</Box>}
            variant="add"
            onClick={onAddSubGroup}
            isDisabled={!isEditMode}
            blendWithRow
          />
          <GhostActionButton
            label="Usuń"
            icon={<Trash2 size={15} />}
            variant="delete"
            onClick={onDeleteGroup}
            isDisabled={!isEditMode}
            blendWithRow
          />
        </TreeViewStickyCell>

        {/* Base cells in column order (empty for item-only, values for net/gross) */}
        {renderGroupBaseCells()}

        {/* Additional field cells */}
        {renderGroupAdditionalCells()}
      </Flex>

      {/* Expanded content */}
      {isExpanded && (
        <Box>
          {/* Items (positions) — rendered first per hierarchy: Items → Stages */}
          {sortedItems.length > 0 && isEditMode && (
            <DndContext
              sensors={itemSensors}
              collisionDetection={closestCenter}
              onDragEnd={handleItemDragEnd}
            >
              <SortableContext
                items={sortedItems.map((i) => i.id)}
                strategy={verticalListSortingStrategy}
              >
                {sortedItems.map((item, itemIndex) => (
                  <ItemRow
                    key={item.id}
                    item={item}
                    groupId={group.id}
                    currencySymbol={currencySymbol}
                    level={level + 1}
                    isEditMode={isEditMode}
                    baseColumns={baseColumns}
                    additionalColumns={additionalColumns}
                    additionalFieldDefs={additionalFieldDefs}
                    searchQuery={searchQuery}
                    sortConfig={sortConfig}
                    onFieldChange={onFieldChange}
                    onFieldAutosave={onFieldAutosave}
                    onDeleteItem={onDeleteItem}
                    onAddComponent={onAddComponent}
                    onAddOption={onAddOption}
                    onSelectOption={(parentItemId, optionId) =>
                      onSelectOption(group.id, parentItemId, optionId)
                    }
                    onUploadFiles={onUploadFiles}
                    projectUnits={projectUnits}
                    onAddProjectUnit={onAddProjectUnit}
                    isAddingUnit={isAddingUnit}
                    totalColumnsWidth={totalColumnsWidth}
                    nameColWidth={nameColWidth}
                    actionsColWidth={actionsColWidth}
                    isLast={
                      itemIndex === sortedItems.length - 1 &&
                      sortedChildGroups.length === 0 &&
                      isLast
                    }
                    onReorderItemChildren={onReorderItemChildren}
                  />
                ))}
              </SortableContext>
            </DndContext>
          )}
          {sortedItems.length > 0 && !isEditMode && (
            <>
              {sortedItems.map((item, itemIndex) => (
                <ItemRow
                  key={item.id}
                  item={item}
                  groupId={group.id}
                  currencySymbol={currencySymbol}
                  level={level + 1}
                  isEditMode={isEditMode}
                  baseColumns={baseColumns}
                  additionalColumns={additionalColumns}
                  additionalFieldDefs={additionalFieldDefs}
                  searchQuery={searchQuery}
                  sortConfig={sortConfig}
                  onFieldChange={onFieldChange}
                  onFieldAutosave={onFieldAutosave}
                  onDeleteItem={onDeleteItem}
                  onAddComponent={onAddComponent}
                  onAddOption={onAddOption}
                  onSelectOption={(parentItemId, optionId) =>
                    onSelectOption(group.id, parentItemId, optionId)
                  }
                  onUploadFiles={onUploadFiles}
                  projectUnits={projectUnits}
                  onAddProjectUnit={onAddProjectUnit}
                  isAddingUnit={isAddingUnit}
                  totalColumnsWidth={totalColumnsWidth}
                  nameColWidth={nameColWidth}
                  actionsColWidth={actionsColWidth}
                  isLast={
                    itemIndex === sortedItems.length - 1 &&
                    sortedChildGroups.length === 0 &&
                    isLast
                  }
                  onReorderItemChildren={onReorderItemChildren}
                />
              ))}
            </>
          )}

          {/* Sub-groups (childGroups) — rendered after items per hierarchy */}
          {hasSubGroups && (
            <Box>
              {sortedChildGroups.map((childGroup, childIndex) => (
                <TreeViewRow
                  key={childGroup.id}
                  group={childGroup}
                  currencySymbol={currencySymbol}
                  level={level + 1}
                  isExpanded={true}
                  isEditMode={isEditMode}
                  baseColumns={baseColumns}
                  additionalColumns={additionalColumns}
                  additionalFieldDefs={additionalFieldDefs}
                  searchQuery={searchQuery}
                  sortConfig={sortConfig}
                  onToggle={() => {}}
                  onFieldChange={onFieldChange}
                  onFieldAutosave={onFieldAutosave}
                  onAddItem={() => onAddItemFromRow(childGroup.id)}
                  onAddSubGroup={() => onAddSubGroupFromRow(childGroup.id)}
                  onAddItemFromRow={onAddItemFromRow}
                  onAddSubGroupFromRow={onAddSubGroupFromRow}
                  onDeleteGroupFromRow={onDeleteGroupFromRow}
                  onAddComponent={(itemId) => onAddComponentFromRow(childGroup.id, itemId)}
                  onAddOption={(itemId) => onAddOptionFromRow(childGroup.id, itemId)}
                  onAddComponentFromRow={onAddComponentFromRow}
                  onAddOptionFromRow={onAddOptionFromRow}
                  onDeleteGroup={() => onDeleteGroupFromRow(childGroup.id)}
                  onDeleteItem={(itemId) => onDeleteItemFromRow(childGroup.id, itemId)}
                  onDeleteItemFromRow={onDeleteItemFromRow}
                  onSelectOption={onSelectOption}
                  onUploadFiles={onUploadFiles}
                  projectUnits={projectUnits}
                  onAddProjectUnit={onAddProjectUnit}
                  isAddingUnit={isAddingUnit}
                  totalColumnsWidth={totalColumnsWidth}
                  nameColWidth={nameColWidth}
                  actionsColWidth={actionsColWidth}
                  isLast={
                    childIndex === sortedChildGroups.length - 1 &&
                    isLast
                  }
                  onReorderItemChildren={onReorderItemChildren}
                  onReorderItems={onReorderItems}
                />
              ))}
            </Box>
          )}

          {/* Inline "add item" row */}
          {isEditMode && (
            <Flex
              align="center"
              minH="46px"
              minW={`${totalColumnsWidth}px`}
              borderBottom={isLast ? 'none' : '1px solid'}
              borderColor="neutral.100"
              px={3.5}
              py={2}
              role="row"
              position="relative"
              _hover={treeViewRowHoverStyles(ADD_ROW_SURFACE.hoverBg)}
            >
              <TreeViewRowBackground bg={ADD_ROW_SURFACE.bg} />
              <TreeViewStickyCell
                surfaceBg={ADD_ROW_SURFACE.bg}
                left={0}
                width={`${leadingStickyWidth}px`}
                pl={`${(level + 1) * TREE_INDENT_STEP}px`}
                flexWrap="nowrap"
                overflow="visible"
              >
                <Flex
                  align="center"
                  gap={2}
                  flexWrap="nowrap"
                  flexShrink={0}
                  minW={`${ADD_ROW_CONTROLS_MIN_WIDTH}px`}
                >
                  <Box flexShrink={0}>
                    <AddInlineButton onClick={onAddItem}>Dodaj pozycję</AddInlineButton>
                  </Box>
                  <Box flexShrink={0}>
                    <AddInlineButton onClick={onAddSubGroup}>Dodaj podetap</AddInlineButton>
                  </Box>
                </Flex>
              </TreeViewStickyCell>
            </Flex>
          )}
        </Box>
      )}
    </Box>
  );
};

// ---------------------------------------------------------------------------
// ItemRow
// ---------------------------------------------------------------------------

interface ItemRowProps {
  item: CostEstimateItemWeb;
  groupId: string;
  currencySymbol: string;
  level: number;
  isEditMode: boolean;
  baseColumns: ColumnDef[];
  additionalColumns: ColumnDef[];
  additionalFieldDefs: CostEstimateAdditionalFieldWeb[];
  searchQuery: string;
  sortConfig: SortConfig | null;
  onFieldChange: (
    groupId: string,
    itemId: string | null,
    fieldId: string,
    value: string | number | boolean | null
  ) => void;
  onFieldAutosave?: (params: AutosaveParams) => void;
  /** @param itemId ID elementu do usunięcia (może być ID pozycji, komponentu lub opcji) */
  onDeleteItem: (itemId: string) => void;
  onAddComponent: (itemId: string) => void;
  onAddOption: (itemId: string) => void;
  onSelectOption: (parentItemId: string, optionId: string) => void;
  onUploadFiles: (itemId: string) => void;
  onReorderItemChildren: (parentItemId: string, itemOrders: Array<{ itemId: string; order: number }>) => void;
  isLast: boolean;
  totalColumnsWidth: number;
  projectUnits: string[];
  onAddProjectUnit?: (code: string) => void;
  isAddingUnit?: boolean;
  nameColWidth: number;
  actionsColWidth: number;
}

const ItemRow: React.FC<ItemRowProps> = ({
  item,
  groupId,
  currencySymbol,
  level,
  isEditMode,
  nameColWidth,
  actionsColWidth,
  baseColumns,
  additionalColumns,
  additionalFieldDefs,
  searchQuery,
  sortConfig,
  onFieldChange,
  onFieldAutosave,
  onDeleteItem,
  onAddComponent,
  onAddOption,
  onSelectOption,
  onUploadFiles,
  onReorderItemChildren,
  isLast,
  totalColumnsWidth,
  projectUnits,
  onAddProjectUnit,
  isAddingUnit,
}) => {
  const indentSize = getTreeIndentPx(level);
  const isComponent = item.relationType === 2;
  const isOption = item.relationType === 1;
  const hasComponents = (item.components?.length ?? 0) > 0;
  const hasOptions = (item.options?.length ?? 0) > 0;
  const hasChildren = hasComponents || hasOptions;
  const [isChildrenExpanded, setIsChildrenExpanded] = useState(true);
  const hasSelectedOption = item.options?.some((o) => o.isSelected) ?? false;
  const hasFiles = (item.files?.length ?? 0) > 0;

  const toggleChildrenExpanded = useCallback(() => {
    setIsChildrenExpanded((prev) => !prev);
  }, []);

  // -------------------------------------------------------------------------
  // Drag & Drop: useSortable for this item (dragging within parent group)
  // -------------------------------------------------------------------------
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: item.id });

  const itemRowStyle = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  // -------------------------------------------------------------------------
  // Sorted components and options — respect hierarchy (Options before Components)
  // -------------------------------------------------------------------------

  const sortedComponents = useMemo(() => {
    const comps = [...(item.components ?? [])];
    if (!sortConfig) return comps;
    const { field, direction } = sortConfig;
    const sign = direction === 'asc' ? 1 : -1;

    return comps.sort((a, b) => {
      let aVal: number | string = '';
      let bVal: number | string = '';

      switch (field) {
        case 'name':
          aVal = a.name ?? '';
          bVal = b.name ?? '';
          break;
        case 'quantity':
          aVal = a.quantity ?? 0;
          bVal = b.quantity ?? 0;
          break;
        case 'unit':
          aVal = a.unit ?? '';
          bVal = b.unit ?? '';
          break;
        case 'unitPriceNet':
          aVal = a.unitPriceNet ?? 0;
          bVal = b.unitPriceNet ?? 0;
          break;
        case 'vatRate':
          aVal = a.vatRate ?? 0;
          bVal = b.vatRate ?? 0;
          break;
        case 'unitPriceGross':
          aVal = a.unitPriceGross ?? 0;
          bVal = b.unitPriceGross ?? 0;
          break;
        case 'netValue':
          aVal = a.netValue ?? 0;
          bVal = b.netValue ?? 0;
          break;
        case 'grossValue':
          aVal = a.grossValue ?? 0;
          bVal = b.grossValue ?? 0;
          break;
        case 'vatValue':
          aVal = a.vatValue ?? 0;
          bVal = b.vatValue ?? 0;
          break;
        case 'isSelected':
          aVal = a.isSelected ? 1 : 0;
          bVal = b.isSelected ? 1 : 0;
          break;
        case 'isStageWork':
          aVal = a.isStageWork ? 1 : 0;
          bVal = b.isStageWork ? 1 : 0;
          break;
        default: {
          const aFv = getAdditionalFieldValue(a.additionalFieldValues ?? [], field);
          const bFv = getAdditionalFieldValue(b.additionalFieldValues ?? [], field);
          aVal = aFv?.stringValue ?? aFv?.decimalValue ?? '';
          bVal = bFv?.stringValue ?? bFv?.decimalValue ?? '';
        }
      }

      if (typeof aVal === 'number' && typeof bVal === 'number') {
        return (aVal - bVal) * sign;
      }
      return String(aVal).localeCompare(String(bVal)) * sign;
    });
  }, [item.components, sortConfig]);

  const sortedOptions = useMemo(() => {
    const opts = [...(item.options ?? [])];
    if (!sortConfig) return opts;
    const { field, direction } = sortConfig;
    const sign = direction === 'asc' ? 1 : -1;

    return opts.sort((a, b) => {
      let aVal: number | string = '';
      let bVal: number | string = '';

      switch (field) {
        case 'name':
          aVal = a.name ?? '';
          bVal = b.name ?? '';
          break;
        case 'quantity':
          aVal = a.quantity ?? 0;
          bVal = b.quantity ?? 0;
          break;
        case 'unit':
          aVal = a.unit ?? '';
          bVal = b.unit ?? '';
          break;
        case 'unitPriceNet':
          aVal = a.unitPriceNet ?? 0;
          bVal = b.unitPriceNet ?? 0;
          break;
        case 'vatRate':
          aVal = a.vatRate ?? 0;
          bVal = b.vatRate ?? 0;
          break;
        case 'unitPriceGross':
          aVal = a.unitPriceGross ?? 0;
          bVal = b.unitPriceGross ?? 0;
          break;
        case 'netValue':
          aVal = a.netValue ?? 0;
          bVal = b.netValue ?? 0;
          break;
        case 'grossValue':
          aVal = a.grossValue ?? 0;
          bVal = b.grossValue ?? 0;
          break;
        case 'vatValue':
          aVal = a.vatValue ?? 0;
          bVal = b.vatValue ?? 0;
          break;
        case 'isSelected':
          aVal = a.isSelected ? 1 : 0;
          bVal = b.isSelected ? 1 : 0;
          break;
        case 'isStageWork':
          aVal = a.isStageWork ? 1 : 0;
          bVal = b.isStageWork ? 1 : 0;
          break;
        default: {
          const aFv = getAdditionalFieldValue(a.additionalFieldValues ?? [], field);
          const bFv = getAdditionalFieldValue(b.additionalFieldValues ?? [], field);
          aVal = aFv?.stringValue ?? aFv?.decimalValue ?? '';
          bVal = bFv?.stringValue ?? bFv?.decimalValue ?? '';
        }
      }

      if (typeof aVal === 'number' && typeof bVal === 'number') {
        return (aVal - bVal) * sign;
      }
      return String(aVal).localeCompare(String(bVal)) * sign;
    });
  }, [item.options, sortConfig]);

  // Sensors for child items (options/components) drag&drop
  const childSensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  // Handle drag end for options within this item
  const handleOptionDragEnd = useCallback(
    (event: DragEndEvent) => {
      const { active, over } = event;
      if (!over || active.id === over.id) return;

      const activeId = String(active.id);
      const overId = String(over.id);

      const oldIndex = sortedOptions.findIndex((o) => o.id === activeId);
      const newIndex = sortedOptions.findIndex((o) => o.id === overId);

      if (oldIndex !== -1 && newIndex !== -1) {
        const newOptions = [...sortedOptions];
        const [movedOption] = newOptions.splice(oldIndex, 1);
        newOptions.splice(newIndex, 0, movedOption);
        onReorderItemChildren(item.id, newOptions.map((o, index) => ({ itemId: o.id, order: index })));
      }
    },
    [sortedOptions, item.id, onReorderItemChildren]
  );

  // Handle drag end for components within this item
  const handleComponentDragEnd = useCallback(
    (event: DragEndEvent) => {
      const { active, over } = event;
      if (!over || active.id === over.id) return;

      const activeId = String(active.id);
      const overId = String(over.id);

      const oldIndex = sortedComponents.findIndex((c) => c.id === activeId);
      const newIndex = sortedComponents.findIndex((c) => c.id === overId);

      if (oldIndex !== -1 && newIndex !== -1) {
        const newComponents = [...sortedComponents];
        const [movedComponent] = newComponents.splice(oldIndex, 1);
        newComponents.splice(newIndex, 0, movedComponent);
        onReorderItemChildren(item.id, newComponents.map((c, index) => ({ itemId: c.id, order: index })));
      }
    },
    [sortedComponents, item.id, onReorderItemChildren]
  );

  // Determine item level tag
  const itemLevel = isComponent ? 3 : isOption ? 4 : 2;

  const { bg: rowSurfaceBg, hoverBg: rowSurfaceHoverBg } = getItemRowSurface(itemLevel);

  // -------------------------------------------------------------------------
  // Autosave helpers
  // -------------------------------------------------------------------------

  const triggerBaseAutosave = useCallback(
    (fieldName: string, valueType: AutosaveParams['valueType'], value: string | undefined) => {
      if (!onFieldAutosave || isTemporaryId(item.id)) return;
      onFieldAutosave({
        entityType: 'item',
        entityId: item.id,
        fieldName,
        fieldKind: 'base',
        valueType,
        value,
      });
    },
    [onFieldAutosave, item.id]
  );

  const triggerAdditionalAutosave = useCallback(
    (
      additionalFieldId: string,
      fieldDef: CostEstimateAdditionalFieldWeb,
      value: string | undefined
    ) => {
      if (!onFieldAutosave || isTemporaryId(item.id)) return;
      const existing = getAdditionalFieldValue(item.additionalFieldValues ?? [], additionalFieldId);
      const fieldValueId = existing?.id && !isTemporaryId(existing.id) ? existing.id : null;
      const valueType = getAdditionalFieldAutosaveValueType(fieldDef.fieldType);
      onFieldAutosave({
        entityType: 'item',
        entityId: item.id,
        fieldValueId,
        additionalFieldId,
        fieldKind: 'additional',
        valueType,
        value,
      });
    },
    [onFieldAutosave, item.id, item.additionalFieldValues]
  );

  // -------------------------------------------------------------------------
  // Radio / checkbox for IsSelected on options
  // -------------------------------------------------------------------------

  const handleRadioClick = useCallback(() => {
    if (isOption && item.parentItemId) {
      onSelectOption(item.parentItemId, item.id);
    }
  }, [isOption, onSelectOption, item.id, item.parentItemId]);

  // -------------------------------------------------------------------------
  // Render base field cells (excluding 'name' which is in the name column)
  // -------------------------------------------------------------------------

  const renderBaseFieldCells = () => {
    const flags = computeItemFieldFlags(item);
    return baseColumns
      .filter((c) => c.id !== 'name')
      .map((col) => {
        const w = col.width ?? '100px';
        const cellJustify = getColumnCellJustify(col.textAlign);

        // Group-only or not applicable columns show — for items that are not applicable
        // All base columns except 'name' apply to items (groups show them empty)
        // Financial fields are editable when not computed from other fields

        if (isNetValueColumn(col)) {
          const showCurrency = !isEditMode || flags.netValueComputed;
          return (
            <Flex key={col.id} flex="0 0 auto" w={w} justify={cellJustify} pr={1}>
              {showCurrency ? (
                <Text
                  fontSize="sm"
                  w="full"
                  textAlign={cellJustify === 'flex-start' ? 'left' : 'right'}
                  sx={{ fontVariantNumeric: 'tabular-nums' }}
                >
                  {item.netValue !== undefined && item.netValue !== null
                    ? formatCurrency(item.netValue, currencySymbol)
                    : '—'}
                </Text>
              ) : (
                <PrototypeNumberInput
                  value={item.netValue !== undefined && item.netValue !== null ? String(item.netValue) : ''}
                  onChange={(e) => {
                    const v = e.target.value;
                    onFieldChange(groupId, item.id, 'netValue', v);
                    triggerBaseAutosave('netValue', 'numeric', v);
                  }}
                  isDisabled={!isEditMode || flags.netValueComputed}
                  placeholder={getBaseFieldPlaceholder(col.label)}
                  w="full"
                  blendWithRow
                />
              )}
            </Flex>
          );
        }

        if (isGrossValueColumn(col)) {
          const showCurrency = !isEditMode || flags.grossValueComputed;
          return (
            <Flex key={col.id} flex="0 0 auto" w={w} justify={cellJustify} pr={1}>
              {showCurrency ? (
                <Text
                  fontSize="sm"
                  w="full"
                  textAlign={cellJustify === 'flex-start' ? 'left' : 'right'}
                  sx={{ fontVariantNumeric: 'tabular-nums' }}
                >
                  {item.grossValue !== undefined && item.grossValue !== null
                    ? formatCurrency(item.grossValue, currencySymbol)
                    : '—'}
                </Text>
              ) : (
                <PrototypeNumberInput
                  value={item.grossValue !== undefined && item.grossValue !== null ? String(item.grossValue) : ''}
                  onChange={(e) => {
                    const v = e.target.value;
                    onFieldChange(groupId, item.id, 'grossValue', v);
                    triggerBaseAutosave('grossValue', 'numeric', v);
                  }}
                  isDisabled={!isEditMode || flags.grossValueComputed}
                  placeholder={getBaseFieldPlaceholder(col.label)}
                  w="full"
                  blendWithRow
                />
              )}
            </Flex>
          );
        }

        if (col.id === 'vatValue') {
          return (
            <Flex key={col.id} flex="0 0 auto" w={w} justify={cellJustify} pr={1}>
              <PrototypeNumberInput
                value={item.vatValue !== undefined && item.vatValue !== null ? String(item.vatValue) : ''}
                onChange={(e) => {
                  const v = e.target.value;
                  onFieldChange(groupId, item.id, 'vatValue', v);
                  triggerBaseAutosave('vatValue', 'numeric', v);
                }}
                isDisabled={!isEditMode || flags.vatValueComputed}
                placeholder={getBaseFieldPlaceholder(col.label)}
                w="full"
                blendWithRow
              />
            </Flex>
          );
        }

        if (col.id === 'unitPriceGross') {
          return (
            <Flex key={col.id} flex="0 0 auto" w={w} justify={cellJustify} pr={1}>
              <PrototypeNumberInput
                value={item.unitPriceGross !== undefined && item.unitPriceGross !== null ? String(item.unitPriceGross) : ''}
                onChange={(e) => {
                  const v = e.target.value;
                  onFieldChange(groupId, item.id, 'unitPriceGross', v);
                  triggerBaseAutosave('unitPriceGross', 'numeric', v);
                }}
                isDisabled={!isEditMode || flags.unitPriceGrossComputed}
                placeholder={getBaseFieldPlaceholder(col.label)}
                w="full"
                blendWithRow
              />
            </Flex>
          );
        }

        if (col.id === 'quantity') {
          return (
            <Flex key={col.id} flex="0 0 auto" w={w} justify={cellJustify} pr={1}>
              <PrototypeNumberInput
                value={item.quantity !== undefined && item.quantity !== null ? String(item.quantity) : ''}
                onChange={(e) => {
                  const v = e.target.value;
                  onFieldChange(groupId, item.id, 'quantity', v);
                  triggerBaseAutosave('quantity', 'numeric', v);
                }}
                isDisabled={!isEditMode || flags.financialFieldsLockedByComponents || hasSelectedOption}
                placeholder={getBaseFieldPlaceholder(col.label)}
                w="full"
                blendWithRow
              />
            </Flex>
          );
        }

        if (col.id === 'unit') {
          return (
            <Flex key="unit" flex="0 0 auto" w={w} justify={cellJustify} pr={2}>
              <UnitCombobox
                value={item.unit ?? ''}
                units={projectUnits}
                onChange={(v) => {
                  onFieldChange(groupId, item.id, 'unit', v);
                }}
                onBlur={() => {
                  triggerBaseAutosave('unit', 'string', item.unit ?? '');
                }}
                onAddNewUnit={onAddProjectUnit}
                isAddingUnit={isAddingUnit}
                isDisabled={!isEditMode || flags.financialFieldsLockedByComponents || hasSelectedOption}
                placeholder={getBaseFieldPlaceholder(col.label)}
                w="full"
                blendWithRow
                textAlign="right"
              />
            </Flex>
          );
        }

        if (col.id === 'unitPriceNet') {
          return (
            <Flex key={col.id} flex="0 0 auto" w={w} justify={cellJustify} pr={1}>
              <PrototypeNumberInput
                value={item.unitPriceNet !== undefined && item.unitPriceNet !== null ? String(item.unitPriceNet) : ''}
                onChange={(e) => {
                  const v = e.target.value;
                  onFieldChange(groupId, item.id, 'unitPriceNet', v);
                  triggerBaseAutosave('unitPriceNet', 'numeric', v);
                }}
                isDisabled={!isEditMode || flags.financialFieldsLockedByComponents || hasSelectedOption}
                placeholder={getBaseFieldPlaceholder(col.label)}
                w="full"
                blendWithRow
              />
            </Flex>
          );
        }

        if (col.id === 'vatRate') {
          // Display as percentage (0.23 → "23")
          const displayVat =
            item.vatRate !== undefined && item.vatRate !== null
              ? String(Math.round(item.vatRate * 100))
              : '';
          return (
            <Flex key={col.id} flex="0 0 auto" w={w} justify={cellJustify} pr={1}>
              <PrototypeNumberInput
                value={displayVat}
                onChange={(e) => {
                  const v = e.target.value;
                  // Convert % to decimal for storage
                  const raw = parseFloat(v.replace(',', '.'));
                  const decimal = isNaN(raw) ? v : String(raw / 100);
                  onFieldChange(groupId, item.id, 'vatRate', decimal);
                  triggerBaseAutosave('vatRate', 'numeric', decimal);
                }}
                isDisabled={!isEditMode || flags.financialFieldsLockedByComponents || hasSelectedOption}
                placeholder={getBaseFieldPlaceholder(col.label)}
                w="full"
                blendWithRow
              />
            </Flex>
          );
        }

        if (col.id === 'isSelected') {
          // Options show radio button in the name column; here show empty cell
          if (isOption) {
            return <EmptyCell key="isSelected" width={w} />;
          }
          return (
            <Flex key="isSelected" flex="0 0 auto" w={w} justify={cellJustify} align="center">
              <Checkbox
                isChecked={item.isSelected}
                onChange={(e) => {
                  const v = e.target.checked;
                  onFieldChange(groupId, item.id, 'isSelected', v);
                  triggerBaseAutosave('isSelected', 'boolean', v ? 'true' : 'false');
                }}
                isDisabled={!isEditMode}
                colorScheme="primary"
                size="sm"
                aria-label="Sumuj"
              />
            </Flex>
          );
        }

        if (col.id === 'isStageWork') {
          if (isOption || isComponent) return <EmptyCell key="isStageWork" width={w} />;
          return (
            <Flex key="isStageWork" flex="0 0 auto" w={w} justify={cellJustify} align="center">
              <Checkbox
                isChecked={item.isStageWork}
                onChange={(e) => {
                  const v = e.target.checked;
                  onFieldChange(groupId, item.id, 'isStageWork', v);
                  triggerBaseAutosave('isStageWork', 'boolean', v ? 'true' : 'false');
                }}
                isDisabled={!isEditMode}
                colorScheme="orange"
                size="sm"
                aria-label="Zakres pracy harmonogramu"
              />
            </Flex>
          );
        }

        if (col.id === 'files') {
          return (
            <Flex key="files" flex="0 0 auto" w={w} justify={cellJustify} align="center">
              <Tooltip label={hasFiles ? `${item.files?.length ?? 0} plik(ów)` : 'Dodaj plik'}>
                <IconButton
                  aria-label="Pliki"
                  icon={hasFiles ? <FileText size={13} aria-hidden="true" /> : <Upload size={13} aria-hidden="true" />}
                  size="xs"
                  variant="ghost"
                  colorScheme={hasFiles ? 'primary' : 'gray'}
                  onClick={() => onUploadFiles(item.id)}
                  opacity={hasFiles ? 1 : 0.4}
                  _hover={{ opacity: 1, bg: 'transparent' }}
                  _active={{ bg: 'transparent' }}
                />
              </Tooltip>
            </Flex>
          );
        }

        return null;
      });
  };

  // -------------------------------------------------------------------------
  // Render additional field cells for item
  // -------------------------------------------------------------------------

  const renderAdditionalCells = () => {
    return additionalColumns.map((col) => {
      const fieldDef = additionalFieldDefs.find((f) => f.id === col.id);
      if (!fieldDef) {
        return <Box key={col.id} flex="0 0 auto" w={col.width ?? '130px'} />;
      }

      const additionalFieldsLocked = areItemAdditionalFieldsLocked({
        isComponent,
        isOption,
        hasComponents,
        hasOptions,
        hasSelectedOption,
        flags: computeItemFieldFlags(item),
      });
      const disabled = !isEditMode || additionalFieldsLocked;
      const cellJustify = getColumnCellJustify(col.textAlign);

      return (
        <Flex
          key={col.id}
          flex="0 0 auto"
          w={col.width ?? '130px'}
          justify={cellJustify}
          align="center"
          px={1}
          pr={col.textAlign === 'right' ? 2 : 1}
        >
          <AdditionalFieldInput
            field={fieldDef}
            fieldValues={item.additionalFieldValues ?? []}
            isDisabled={disabled}
            blendWithRow
            valueAlign={col.textAlign === 'left' ? 'left' : 'right'}
            onChange={(value) => {
              onFieldChange(groupId, item.id, col.id, value);
              triggerAdditionalAutosave(
                col.id,
                fieldDef,
                formatAdditionalFieldAutosaveValue(value)
              );
            }}
          />
        </Flex>
      );
    });
  };

  // -------------------------------------------------------------------------
  // Render
  // -------------------------------------------------------------------------

  return (
    <Box>
      {/* Main item row */}
      <Flex
        ref={setNodeRef}
        style={itemRowStyle}
        align="center"
        minH="46px"
        minW={`${totalColumnsWidth}px`}
        borderBottom={isLast && (!hasChildren || !isChildrenExpanded) ? 'none' : '1px solid'}
        borderColor="neutral.100"
        _hover={treeViewRowHoverStyles(rowSurfaceHoverBg)}
        px={3.5}
        py={2}
        position="relative"
        role="row"
        className="trow"
      >
        <TreeViewRowBackground bg={rowSurfaceBg} />
        <TreeViewStickyCell
          surfaceBg={rowSurfaceBg}
          left={0}
          width={`${nameColWidth}px`}
          gap={2}
          pl={`${indentSize}px`}
        >
          {isEditMode && !isOption && (
            <Box {...attributes} {...listeners}>
              <DragHandle isDragging={isDragging} blendWithRow />
            </Box>
          )}
          {hasChildren && !isOption && (
            <ChevronButton
              isExpanded={isChildrenExpanded}
              onClick={toggleChildrenExpanded}
              blendWithRow
            />
          )}
          <PrototypeTag level={itemLevel} />
          <PrototypeDot level={itemLevel} size={itemLevel >= 3 ? 7 : 8} />

          {/* Radio button for options — next to name */}
          {isOption && (
            <Box
              as="button"
              w="16px"
              h="16px"
              minW="16px"
              borderRadius="50%"
              border="2px solid"
              borderColor={item.isSelected ? 'primary.500' : 'neutral.300'}
              bg={item.isSelected ? 'primary.500' : 'transparent'}
              display="flex"
              alignItems="center"
              justifyContent="center"
              onClick={handleRadioClick}
              disabled={!isEditMode}
              aria-label="Wybierz opcję"
              _hover={{ borderColor: 'primary.500' }}
              flexShrink={0}
            >
              {item.isSelected && <Box w="6px" h="6px" borderRadius="50%" bg="white" />}
            </Box>
          )}

          <PrototypeTextInput
            value={item.name ?? ''}
            onChange={(e) => {
              const v = e.target.value;
              onFieldChange(groupId, item.id, 'name', v);
              triggerBaseAutosave('name', 'string', v);
            }}
            isDisabled={!isEditMode || hasSelectedOption}
            placeholder={
              isComponent
                ? 'Nazwa komponentu'
                : isOption
                ? 'Nazwa opcji'
                : 'Nazwa pozycji'
            }
            w="full"
            blendWithRow
          />
        </TreeViewStickyCell>

        {/* Actions (sticky, right after name) */}
        <TreeViewStickyCell
          surfaceBg={rowSurfaceBg}
          left={`${nameColWidth}px`}
          width={`${actionsColWidth}px`}
          justify="flex-end"
          gap={0.5}
          pr={2}
        >
          {!isComponent && !isOption && !hasOptions && (
            <GhostActionButton
              label="Dodaj komponent"
              icon={<Box as="span" fontSize="sm" fontWeight="bold" lineHeight="1">K+</Box>}
              variant="add"
              onClick={() => onAddComponent(item.id)}
              isDisabled={!isEditMode}
              blendWithRow
            />
          )}
          {((isComponent && !isOption) || (!isComponent && !isOption && !hasComponents)) && (
            <GhostActionButton
              label="Dodaj opcję"
              icon={<Box as="span" fontSize="sm" fontWeight="bold" lineHeight="1">O+</Box>}
              variant="add"
              onClick={() => onAddOption(item.id)}
              isDisabled={!isEditMode}
              blendWithRow
            />
          )}
          <GhostActionButton
            label="Usuń"
            icon={<Trash2 size={15} />}
            variant="delete"
            onClick={() => onDeleteItem(item.id)}
            isDisabled={!isEditMode}
            blendWithRow
          />
        </TreeViewStickyCell>

        <Box
          display="contents"
          opacity={isOption && !item.isSelected ? 0.75 : 1}
        >
          {renderBaseFieldCells()}
          {renderAdditionalCells()}
        </Box>
      </Flex>

      {/* Options section — rendered first per hierarchy: Options → Components */}
      {isChildrenExpanded && hasOptions && isEditMode && (
        <DndContext
          sensors={childSensors}
          collisionDetection={closestCenter}
          onDragEnd={handleOptionDragEnd}
        >
          <SortableContext
            items={sortedOptions.map((o) => o.id)}
            strategy={verticalListSortingStrategy}
          >
            <Box borderBottom="1px solid" borderColor="neutral.100">
              {sortedOptions.map((option, optIndex) => (
                <ItemRow
                  key={option.id}
                  item={option}
                  groupId={groupId}
                  currencySymbol={currencySymbol}
                  level={level + 1}
                  isEditMode={isEditMode}
                  baseColumns={baseColumns}
                  additionalColumns={additionalColumns}
                  additionalFieldDefs={additionalFieldDefs}
                  searchQuery={searchQuery}
                  sortConfig={sortConfig}
                  onFieldChange={onFieldChange}
                  onFieldAutosave={onFieldAutosave}
                  onDeleteItem={onDeleteItem}
                  onAddComponent={onAddComponent}
                  onAddOption={onAddOption}
                  onSelectOption={onSelectOption}
                  onUploadFiles={onUploadFiles}
                  projectUnits={projectUnits}
                  onAddProjectUnit={onAddProjectUnit}
                  isAddingUnit={isAddingUnit}
                  totalColumnsWidth={totalColumnsWidth}
                  nameColWidth={nameColWidth}
                  actionsColWidth={actionsColWidth}
                  isLast={optIndex === sortedOptions.length - 1 && !hasComponents}
                  onReorderItemChildren={onReorderItemChildren}
                />
              ))}
            </Box>
          </SortableContext>
        </DndContext>
      )}
      {isChildrenExpanded && hasOptions && !isEditMode && (
        <Box borderBottom="1px solid" borderColor="neutral.100">
          {sortedOptions.map((option, optIndex) => (
            <ItemRow
              key={option.id}
              item={option}
              groupId={groupId}
              currencySymbol={currencySymbol}
              level={level + 1}
              isEditMode={isEditMode}
              baseColumns={baseColumns}
              additionalColumns={additionalColumns}
              additionalFieldDefs={additionalFieldDefs}
              searchQuery={searchQuery}
              sortConfig={sortConfig}
              onFieldChange={onFieldChange}
              onFieldAutosave={onFieldAutosave}
              onDeleteItem={onDeleteItem}
              onAddComponent={onAddComponent}
              onAddOption={onAddOption}
              onSelectOption={onSelectOption}
              onUploadFiles={onUploadFiles}
              projectUnits={projectUnits}
              onAddProjectUnit={onAddProjectUnit}
              isAddingUnit={isAddingUnit}
              totalColumnsWidth={totalColumnsWidth}
              nameColWidth={nameColWidth}
              actionsColWidth={actionsColWidth}
              isLast={optIndex === sortedOptions.length - 1 && !hasComponents}
              onReorderItemChildren={onReorderItemChildren}
            />
          ))}
        </Box>
      )}

      {/* Components section — rendered after options */}
      {isChildrenExpanded && hasComponents && isEditMode && (
        <DndContext
          sensors={childSensors}
          collisionDetection={closestCenter}
          onDragEnd={handleComponentDragEnd}
        >
          <SortableContext
            items={sortedComponents.map((c) => c.id)}
            strategy={verticalListSortingStrategy}
          >
            <Box borderBottom="1px solid" borderColor="neutral.100">
              {sortedComponents.map((component, compIndex) => (
                <ItemRow
                  key={component.id}
                  item={component}
                  groupId={groupId}
                  currencySymbol={currencySymbol}
                  level={level + 1}
                  isEditMode={isEditMode}
                  baseColumns={baseColumns}
                  additionalColumns={additionalColumns}
                  additionalFieldDefs={additionalFieldDefs}
                  searchQuery={searchQuery}
                  sortConfig={sortConfig}
                  onFieldChange={onFieldChange}
                  onFieldAutosave={onFieldAutosave}
                  onDeleteItem={onDeleteItem}
                  onAddComponent={onAddComponent}
                  onAddOption={onAddOption}
                  onSelectOption={onSelectOption}
                  onUploadFiles={onUploadFiles}
                  projectUnits={projectUnits}
                  onAddProjectUnit={onAddProjectUnit}
                  isAddingUnit={isAddingUnit}
                  totalColumnsWidth={totalColumnsWidth}
                  nameColWidth={nameColWidth}
                  actionsColWidth={actionsColWidth}
                  isLast={compIndex === sortedComponents.length - 1}
                  onReorderItemChildren={onReorderItemChildren}
                />
              ))}
            </Box>
          </SortableContext>
        </DndContext>
      )}
      {isChildrenExpanded && hasComponents && !isEditMode && (
        <Box borderBottom="1px solid" borderColor="neutral.100">
          {sortedComponents.map((component, compIndex) => (
            <ItemRow
              key={component.id}
              item={component}
              groupId={groupId}
              currencySymbol={currencySymbol}
              level={level + 1}
              isEditMode={isEditMode}
              baseColumns={baseColumns}
              additionalColumns={additionalColumns}
              additionalFieldDefs={additionalFieldDefs}
              searchQuery={searchQuery}
              sortConfig={sortConfig}
              onFieldChange={onFieldChange}
              onFieldAutosave={onFieldAutosave}
              onDeleteItem={onDeleteItem}
              onAddComponent={onAddComponent}
              onAddOption={onAddOption}
              onSelectOption={onSelectOption}
              onUploadFiles={onUploadFiles}
              projectUnits={projectUnits}
              onAddProjectUnit={onAddProjectUnit}
              isAddingUnit={isAddingUnit}
              totalColumnsWidth={totalColumnsWidth}
              nameColWidth={nameColWidth}
              actionsColWidth={actionsColWidth}
              isLast={compIndex === sortedComponents.length - 1}
              onReorderItemChildren={onReorderItemChildren}
            />
          ))}
        </Box>
      )}
    </Box>
  );
};
