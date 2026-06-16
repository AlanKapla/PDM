/**
 * Position Card - Card for single position (pozycja)
 * Read-only: name + net/gross; editing in modal on click
 * Interactive: checkbox (sumuj), radio (opcje)
 */

import React, { useCallback, useState } from 'react';
import { Box, Flex, HStack, VStack, Text, SimpleGrid, Collapse } from '@chakra-ui/react';
import { Trash2 } from 'lucide-react';
import type { CostEstimateItemWeb } from '../../../types/costEstimate.types.new';
import { isTemporaryId } from '../../../types/costEstimate.types.new';
import { GhostActionButton, ChevronButton } from '../PrototypeActionButtons';
import { OptionSubCard } from './OptionSubCard';
import { OptionRadioButton } from './OptionRadioButton';
import { useCostEstimateItemFieldState } from '../../../hooks/useCostEstimateItemFieldState';
import { getItemRowSurface } from '../TreeView/treeViewRowSurfaces';
import { CardAmountSummary, CardRowAside, CardRowDivider } from './CardAmountSummary';
import { CardSumujControl } from './CardSumujControl';

interface AutosaveParams {
  entityType: 'group' | 'item';
  entityId: string;
  fieldValueId?: string | null;
  fieldDefinitionId?: string;
  fieldType?: number;
  additionalFieldId?: string;
  fieldName?: string;
  fieldKind?: 'base' | 'additional';
  valueType: 'string' | 'numeric' | 'boolean' | 'date';
  value: string | undefined;
}

interface PositionCardProps {
  item: CostEstimateItemWeb;
  groupId: string;
  isEditMode: boolean;
  onFieldChange: (
    groupId: string,
    itemId: string | null,
    fieldId: string,
    value: string | number | boolean | null
  ) => void;
  onFieldAutosave?: (params: AutosaveParams) => void;
  onAddComponent: (groupId: string, itemId: string) => void;
  onAddOption: (groupId: string, itemId: string) => void;
  onDeleteItem: (itemId: string) => void;
  onSelectOption: (groupId: string, itemId: string, optionId: string) => void;
  onOpenItemDetail?: (itemId: string, groupId: string) => void;
}

function isInteractiveTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) {
    return false;
  }
  return Boolean(
    target.closest('button') ||
    target.closest('input') ||
    target.closest('[role="checkbox"]') ||
    target.closest('[role="radio"]') ||
    target.closest('[role="button"]')
  );
}

export const PositionCard: React.FC<PositionCardProps> = ({
  item,
  groupId,
  isEditMode,
  onFieldChange,
  onFieldAutosave,
  onAddComponent,
  onAddOption,
  onDeleteItem,
  onSelectOption,
  onOpenItemDetail,
}) => {
  const isComponent = item.relationType === 2;
  const isOption = item.relationType === 1;
  const fieldState = useCostEstimateItemFieldState(item);
  const { hasComponents, hasOptions } = fieldState;
  const hasChildren = hasComponents || hasOptions;
  const [isChildrenExpanded, setIsChildrenExpanded] = useState(true);

  const positionName = item.name || 'Bez nazwy';
  const isSelected = item.isSelected;
  const totalNet = item.netValue ?? 0;
  const totalGross = item.grossValue ?? 0;
  const itemLevel = isComponent ? 3 : isOption ? 4 : 2;
  const rowSurface = getItemRowSurface(itemLevel);

  const triggerBaseFieldAutosave = useCallback(
    (fieldName: string, valueType: AutosaveParams['valueType'], value: string | undefined) => {
      if (!onFieldAutosave || isTemporaryId(item.id)) {
        return;
      }
      onFieldAutosave({
        entityType: 'item',
        entityId: item.id,
        fieldKind: 'base',
        fieldName,
        valueType,
        value,
      });
    },
    [onFieldAutosave, item.id]
  );

  const handleRadio = () => {
    if (isOption && item.parentItemId) {
      onSelectOption(groupId, item.parentItemId, item.id);
    }
  };

  const handleCardClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (isInteractiveTarget(e.target)) {
      return;
    }
    onOpenItemDetail?.(item.id, groupId);
  };

  return (
    <Box
      border="1px solid"
      borderColor="neutral.100"
      borderRadius="12px"
      overflow="hidden"
      bg={rowSurface.bg}
      opacity={isOption && !isSelected ? 0.75 : 1}
      cursor="pointer"
      transition="background 0.12s"
      _hover={{ bg: rowSurface.hoverBg }}
      onClick={handleCardClick}
    >
      <Flex align="center" gap={{ base: 1.5, md: 2.5 }} px={{ base: 2.5, md: 3.5 }} py={{ base: 2, md: 2.75 }} className="trow">
        <HStack spacing={1.5} flexShrink={0}>
          {hasChildren && !isOption && (
            <Box onClick={(e) => e.stopPropagation()}>
              <ChevronButton
                isExpanded={isChildrenExpanded}
                onClick={() => setIsChildrenExpanded((prev) => !prev)}
                blendWithRow
              />
            </Box>
          )}

          {isOption && (
            <Box onClick={(e) => e.stopPropagation()}>
              <OptionRadioButton
                isSelected={isSelected}
                isDisabled={!isEditMode}
                onSelect={handleRadio}
              />
            </Box>
          )}
        </HStack>

        <Text
          flex={1}
          minW={0}
          fontSize="sm"
          fontWeight={isComponent || isOption ? 'semibold' : 'bold'}
          noOfLines={2}
        >
          {positionName}
        </Text>

        <CardAmountSummary
          net={totalNet}
          gross={totalGross}
          size="sm"
          layout="stacked"
        />

        {(!isOption || isEditMode) && (
          <CardRowAside>
            {!isOption && (
              <CardSumujControl
                isChecked={isSelected}
                isDisabled={!isEditMode}
                onChange={(checked) => {
                  onFieldChange(groupId, item.id, 'isSelected', checked);
                  triggerBaseFieldAutosave('isSelected', 'boolean', checked ? 'true' : 'false');
                }}
              />
            )}

            {isEditMode && (
              <>
                {!isOption && <CardRowDivider />}
                <HStack spacing={0.5}>
                  {!isOption && (
                    <>
                      {!isComponent && !hasOptions && (
                        <GhostActionButton
                          label="Dodaj komponent"
                          icon={<Box as="span" fontSize="sm" fontWeight="bold" lineHeight="1">K+</Box>}
                          variant="add"
                          onClick={() => onAddComponent(groupId, item.id)}
                          blendWithRow
                        />
                      )}
                      {(isComponent || (!isComponent && !hasComponents)) && (
                        <GhostActionButton
                          label="Dodaj opcję"
                          icon={<Box as="span" fontSize="sm" fontWeight="bold" lineHeight="1">O+</Box>}
                          variant="add"
                          onClick={() => onAddOption(groupId, item.id)}
                          blendWithRow
                        />
                      )}
                    </>
                  )}
                  <GhostActionButton
                    label="Usuń"
                    icon={<Trash2 size={14} />}
                    variant="delete"
                    onClick={() => onDeleteItem(item.id)}
                    blendWithRow
                  />
                </HStack>
              </>
            )}
          </CardRowAside>
        )}
      </Flex>

      <Collapse in={isChildrenExpanded} animateOpacity unmountOnExit={false}>
        {hasComponents && (
          <Box
            pl={8}
            pr={3}
            pt={2.5}
            pb={3}
            borderTop="1px solid"
            borderColor="neutral.100"
          >
            <VStack spacing={2} align="stretch">
              {item.components!.map((comp) => (
                <PositionCard
                  key={comp.id}
                  item={comp}
                  groupId={groupId}
                  isEditMode={isEditMode}
                  onFieldChange={onFieldChange}
                  onFieldAutosave={onFieldAutosave}
                  onAddComponent={onAddComponent}
                  onAddOption={onAddOption}
                  onDeleteItem={(id) => onDeleteItem(id)}
                  onSelectOption={onSelectOption}
                  onOpenItemDetail={onOpenItemDetail}
                />
              ))}
            </VStack>
          </Box>
        )}

        {hasOptions && (
          <Box
            px={3}
            py={2.5}
            borderTop="1px solid"
            borderColor="neutral.100"
          >
            <SimpleGrid columns={{ base: 1, sm: 2 }} spacing={2}>
              {item.options!.map((opt) => (
                <OptionSubCard
                  key={opt.id}
                  option={opt}
                  groupId={groupId}
                  parentItemId={item.id}
                  isEditMode={isEditMode}
                  onDeleteItem={onDeleteItem}
                  onSelectOption={onSelectOption}
                  onOpenItemDetail={onOpenItemDetail}
                />
              ))}
            </SimpleGrid>
          </Box>
        )}
      </Collapse>
    </Box>
  );
};
