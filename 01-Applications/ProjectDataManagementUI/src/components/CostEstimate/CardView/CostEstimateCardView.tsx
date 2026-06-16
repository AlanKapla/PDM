/**
 * Modern Cost Estimate Card View
 *
 * - Accordion cards for stages (etapy)
 * - Read-only cards: name + net/gross; editing in modal on row click
 * - Checkbox (sumuj) and radio (opcje) remain interactive on cards
 */

import React, { useState, useCallback, useMemo, forwardRef, useImperativeHandle } from 'react';
import { VStack, Flex } from '@chakra-ui/react';
import type {
  CostEstimateDetailsWeb,
  CostEstimateGroupWeb,
  CostEstimateItemWeb,
} from '../../../types/costEstimate.types.new';
import { StageCard } from './StageCard';
import { AddInlineButton } from '../PrototypeActionButtons';
import { PositionDetailModal } from './PositionDetailModal';
import { GroupDetailModal } from './GroupDetailModal';
import { resolveAdditionalFieldDefinitions } from '../../../utils/additionalFieldHelpers';

interface CostEstimateCardViewProps {
  details: CostEstimateDetailsWeb;
  isEditMode: boolean;
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
}

type DetailSelection =
  | { type: 'item'; itemId: string; groupId: string }
  | { type: 'group'; groupId: string; isSubStage: boolean }
  | null;

function findItemInItems(items: CostEstimateItemWeb[], itemId: string): CostEstimateItemWeb | null {
  for (const item of items) {
    if (item.id === itemId) {
      return item;
    }
    if (item.components) {
      const found = findItemInItems(item.components, itemId);
      if (found) {
        return found;
      }
    }
    if (item.options) {
      const found = findItemInItems(item.options, itemId);
      if (found) {
        return found;
      }
    }
  }
  return null;
}

function findItemInGroups(
  groups: CostEstimateGroupWeb[],
  itemId: string
): CostEstimateItemWeb | null {
  for (const group of groups) {
    for (const item of group.items ?? []) {
      if (item.id === itemId) {
        return item;
      }
      if (item.components) {
        const found = findItemInItems(item.components, itemId);
        if (found) {
          return found;
        }
      }
      if (item.options) {
        const found = findItemInItems(item.options, itemId);
        if (found) {
          return found;
        }
      }
    }
    if (group.childGroups) {
      const found = findItemInGroups(group.childGroups, itemId);
      if (found) {
        return found;
      }
    }
  }
  return null;
}

function findGroupInGroups(
  groups: CostEstimateGroupWeb[],
  groupId: string
): CostEstimateGroupWeb | null {
  for (const group of groups) {
    if (group.id === groupId) {
      return group;
    }
    if (group.childGroups) {
      const found = findGroupInGroups(group.childGroups, groupId);
      if (found) {
        return found;
      }
    }
  }
  return null;
}

function collectAllGroupIds(groups: CostEstimateGroupWeb[]): Set<string> {
  const ids = new Set<string>();
  const collect = (groupList: CostEstimateGroupWeb[]) => {
    for (const group of groupList) {
      ids.add(group.id);
      if (group.childGroups) {
        collect(group.childGroups);
      }
    }
  };
  collect(groups);
  return ids;
}

export interface CostEstimateCardViewHandle {
  expandAll: () => void;
  collapseAll: () => void;
}

export const CostEstimateCardView = forwardRef<
  CostEstimateCardViewHandle,
  CostEstimateCardViewProps
>(({
  details,
  isEditMode,
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
}, ref) => {
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(
    () => collectAllGroupIds(details.rootGroups)
  );
  const [detailSelection, setDetailSelection] = useState<DetailSelection>(null);

  const toggleGroup = useCallback((groupId: string) => {
    setExpandedGroups((prev) => {
      const next = new Set(prev);
      if (next.has(groupId)) {
        next.delete(groupId);
      } else {
        next.add(groupId);
      }
      return next;
    });
  }, []);

  const expandAll = useCallback(() => {
    setExpandedGroups(collectAllGroupIds(details.rootGroups));
  }, [details.rootGroups]);

  const collapseAll = useCallback(() => {
    setExpandedGroups(new Set());
  }, []);

  useImperativeHandle(ref, () => ({
    expandAll,
    collapseAll,
  }), [expandAll, collapseAll]);

  const handleOpenItemDetail = useCallback((itemId: string, groupId: string) => {
    setDetailSelection({ type: 'item', itemId, groupId });
  }, []);

  const handleOpenGroupDetail = useCallback((groupId: string, isSubStage: boolean) => {
    setDetailSelection({ type: 'group', groupId, isSubStage });
  }, []);

  const handleCloseDetail = useCallback(() => {
    setDetailSelection(null);
  }, []);

  const selectedItem = useMemo((): CostEstimateItemWeb | null => {
    if (detailSelection?.type !== 'item') {
      return null;
    }
    return findItemInGroups(details.rootGroups, detailSelection.itemId);
  }, [detailSelection, details.rootGroups]);

  const selectedGroup = useMemo((): CostEstimateGroupWeb | null => {
    if (detailSelection?.type !== 'group') {
      return null;
    }
    return findGroupInGroups(details.rootGroups, detailSelection.groupId);
  }, [detailSelection, details.rootGroups]);

  const additionalFields = useMemo(
    () => resolveAdditionalFieldDefinitions(details),
    [details]
  );

  return (
    <VStack spacing={4} align="stretch">
      {details.rootGroups.map((stage) => (
        <StageCard
          key={stage.id}
          stage={stage}
          isExpanded={expandedGroups.has(stage.id)}
          expandedGroups={expandedGroups}
          isEditMode={isEditMode}
          onToggle={() => toggleGroup(stage.id)}
          onToggleGroup={toggleGroup}
          onFieldChange={onFieldChange}
          onFieldAutosave={onFieldAutosave}
          onAddItem={onAddItem}
          onAddSubGroup={onAddSubGroup}
          onAddComponent={onAddComponent}
          onAddOption={onAddOption}
          onDeleteGroup={() => onDeleteGroup(stage.id)}
          onDeleteItem={onDeleteItem}
          onSelectOption={onSelectOption}
          onOpenItemDetail={handleOpenItemDetail}
          onOpenGroupDetail={handleOpenGroupDetail}
        />
      ))}

      {isEditMode && (
        <Flex justify="flex-start" pt={2}>
          <AddInlineButton onClick={onAddGroup}>
            Dodaj etap
          </AddInlineButton>
        </Flex>
      )}

      {selectedItem && detailSelection?.type === 'item' && (
        <PositionDetailModal
          item={selectedItem}
          groupId={detailSelection.groupId}
          isOpen={true}
          onClose={handleCloseDetail}
          isEditMode={isEditMode}
          additionalFields={additionalFields}
          onFieldChange={onFieldChange}
          onFieldAutosave={onFieldAutosave}
          onAddComponent={onAddComponent}
          onAddOption={onAddOption}
          onDeleteItem={(itemId) => onDeleteItem(detailSelection.groupId, itemId)}
          onSelectOption={onSelectOption}
          onUploadFiles={onUploadFiles}
        />
      )}

      {selectedGroup && detailSelection?.type === 'group' && (
        <GroupDetailModal
          group={selectedGroup}
          isOpen={true}
          onClose={handleCloseDetail}
          isEditMode={isEditMode}
          isSubStage={detailSelection.isSubStage}
          additionalFields={additionalFields}
          onFieldChange={onFieldChange}
          onFieldAutosave={onFieldAutosave}
        />
      )}
    </VStack>
  );
});

CostEstimateCardView.displayName = 'CostEstimateCardView';
