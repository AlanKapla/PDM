/**
 * Modern Cost Estimate View - Wrapper with Tree/Card toggle
 *
 * Supports two view modes from prototype:
 * - Tree View (Variant A): Table-tree with hierarchy
 * - Card View (Variant B): Accordion cards with chips
 */

import React, { forwardRef, useImperativeHandle, useRef, useMemo } from 'react';
import { Box } from '@chakra-ui/react';
import { useIsMobile } from '../../hooks/useIsMobile';
import type {
  CostEstimateDetailsWeb,
} from '../../types/costEstimate.types.new';
import { filterCostEstimateGroupsBySearch } from '../../utils/costEstimateUtils';
import { CostEstimateTreeView, type CostEstimateTreeViewHandle } from './TreeView/CostEstimateTreeView';
import { CostEstimateCardView, type CostEstimateCardViewHandle } from './CardView/CostEstimateCardView';

export type CostEstimateViewMode = 'tree' | 'card';

interface CostEstimateModernViewProps {
  details: CostEstimateDetailsWeb;
  isEditMode: boolean;
  tenantId: string;
  projectId: string;
  searchQuery: string;
  onSearchChange: (query: string) => void;
  visibleColIds?: Set<string>;
  onToggleColVisibility?: (fieldId: string) => void;
  onFieldChange: (
    groupId: string,
    itemId: string | null,
    fieldId: string,
    value: string | number | boolean | null
  ) => void;
  /** Autosave callback — wywoływany przy każdej zmianie pola, wysyła request do API z debounce */
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
  viewMode: CostEstimateViewMode;
  /** Wypełnij dostępną wysokość rodzica (np. pełny ekran) zamiast calc() */
  fillHeight?: boolean;
}

export interface CostEstimateModernViewHandle {
  expandAll: () => void;
  collapseAll: () => void;
}

export const CostEstimateModernView = forwardRef<
  CostEstimateModernViewHandle,
  CostEstimateModernViewProps
>(({
  details,
  isEditMode,
  tenantId,
  projectId,
  searchQuery,
  onSearchChange,
  visibleColIds,
  onToggleColVisibility,
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
  viewMode,
  fillHeight = false,
}, ref) => {
  const isMobile = useIsMobile();
  const treeViewRef = useRef<CostEstimateTreeViewHandle>(null);
  const cardViewRef = useRef<CostEstimateCardViewHandle>(null);
  const effectiveViewMode: CostEstimateViewMode = isMobile ? 'card' : viewMode;

  const filteredDetails = useMemo((): CostEstimateDetailsWeb => {
    if (!searchQuery.trim()) {
      return details;
    }
    return {
      ...details,
      rootGroups: filterCostEstimateGroupsBySearch(
        details.rootGroups,
        searchQuery,
        details.additionalFields ?? [],
      ),
    };
  }, [details, searchQuery]);

  useImperativeHandle(ref, () => ({
    expandAll: () => {
      if (isMobile || effectiveViewMode === 'card') {
        cardViewRef.current?.expandAll();
      } else {
        treeViewRef.current?.expandAll();
      }
    },
    collapseAll: () => {
      if (isMobile || effectiveViewMode === 'card') {
        cardViewRef.current?.collapseAll();
      } else {
        treeViewRef.current?.collapseAll();
      }
    },
  }), [isMobile, effectiveViewMode]);

  if (isMobile) {
    return (
      <Box>
        <CostEstimateCardView
          ref={cardViewRef}
          details={filteredDetails}
          isEditMode={isEditMode}
          onFieldChange={onFieldChange}
          onFieldAutosave={onFieldAutosave}
          onAddGroup={onAddGroup}
          onAddSubGroup={onAddSubGroup}
          onAddItem={onAddItem}
          onAddComponent={onAddComponent}
          onAddOption={onAddOption}
          onDeleteGroup={onDeleteGroup}
          onDeleteItem={onDeleteItem}
          onSelectOption={onSelectOption}
          onUploadFiles={onUploadFiles}
        />
      </Box>
    );
  }

  return (
    <Box
      display={fillHeight ? 'flex' : undefined}
      flexDirection={fillHeight ? 'column' : undefined}
      flex={fillHeight ? 1 : undefined}
      minH={fillHeight ? 0 : undefined}
      h={fillHeight ? '100%' : undefined}
    >
      {effectiveViewMode === 'tree' ? (
        <Box
          flex={fillHeight ? 1 : undefined}
          minH={fillHeight ? 0 : undefined}
          maxH={fillHeight ? '100%' : 'calc(100dvh - 380px)'}
          display="flex"
          flexDirection="column"
        >
          <CostEstimateTreeView
            ref={treeViewRef}
            details={details}
            isEditMode={isEditMode}
            tenantId={tenantId}
            projectId={projectId}
            searchQuery={searchQuery}
            onSearchChange={onSearchChange}
            visibleColIds={visibleColIds}
            onToggleColVisibility={onToggleColVisibility}
            onFieldChange={onFieldChange}
            onFieldAutosave={onFieldAutosave}
            onAddGroup={onAddGroup}
            onAddSubGroup={onAddSubGroup}
            onAddItem={onAddItem}
            onAddComponent={onAddComponent}
            onAddOption={onAddOption}
            onDeleteGroup={onDeleteGroup}
            onDeleteItem={onDeleteItem}
            onSelectOption={onSelectOption}
            onUploadFiles={onUploadFiles}
            onReorderGroups={onReorderGroups}
            onReorderItems={onReorderItems}
            onReorderItemChildren={onReorderItemChildren}
            onToggleFieldVisibility={onToggleFieldVisibility}
            onAddField={onAddField}
          />
        </Box>
      ) : (
        <CostEstimateCardView
          ref={cardViewRef}
          details={details}
          isEditMode={isEditMode}
          onFieldChange={onFieldChange}
          onFieldAutosave={onFieldAutosave}
          onAddGroup={onAddGroup}
          onAddSubGroup={onAddSubGroup}
          onAddItem={onAddItem}
          onAddComponent={onAddComponent}
          onAddOption={onAddOption}
          onDeleteGroup={onDeleteGroup}
          onDeleteItem={onDeleteItem}
          onSelectOption={onSelectOption}
          onUploadFiles={onUploadFiles}
        />
      )}
    </Box>
  );
});

CostEstimateModernView.displayName = 'CostEstimateModernView';
