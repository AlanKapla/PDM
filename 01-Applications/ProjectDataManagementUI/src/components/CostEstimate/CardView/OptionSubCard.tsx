/**
 * Compact option tile rendered inside a parent position/component card.
 * Read-only: name + net/gross; editing in modal on click
 */

import React, { useCallback } from 'react';
import { Box, Flex, Text } from '@chakra-ui/react';
import { Trash2 } from 'lucide-react';
import type { CostEstimateItemWeb } from '../../../types/costEstimate.types.new';
import { GhostActionButton } from '../PrototypeActionButtons';
import { OptionRadioButton } from './OptionRadioButton';
import { getItemRowSurface } from '../TreeView/treeViewRowSurfaces';
import { CardAmountSummary } from './CardAmountSummary';

interface OptionSubCardProps {
  option: CostEstimateItemWeb;
  groupId: string;
  parentItemId: string;
  isEditMode: boolean;
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

export const OptionSubCard: React.FC<OptionSubCardProps> = ({
  option,
  groupId,
  parentItemId,
  isEditMode,
  onDeleteItem,
  onSelectOption,
  onOpenItemDetail,
}) => {
  const isSelected = option.isSelected;
  const optionName = option.name || 'Bez nazwy';
  const totalNet = option.netValue ?? 0;
  const totalGross = option.grossValue ?? 0;
  const rowSurface = getItemRowSurface(4);

  const handleSelect = useCallback(() => {
    onSelectOption(groupId, parentItemId, option.id);
  }, [groupId, onSelectOption, option.id, parentItemId]);

  const handleCardClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (isInteractiveTarget(e.target)) {
      return;
    }
    onOpenItemDetail?.(option.id, groupId);
  };

  return (
    <Box
      border="1px solid"
      borderColor="neutral.100"
      borderRadius="10px"
      bg={rowSurface.bg}
      px={2.5}
      py={2}
      minW="160px"
      flex="1 1 160px"
      maxW="100%"
      cursor="pointer"
      opacity={isSelected ? 1 : 0.75}
      transition="background 0.12s"
      _hover={{ bg: rowSurface.hoverBg }}
      onClick={handleCardClick}
    >
      <Flex align="center" gap={2.5}>
        <Box flexShrink={0} onClick={(e) => e.stopPropagation()}>
          <OptionRadioButton
            isSelected={isSelected}
            isDisabled={!isEditMode}
            onSelect={handleSelect}
            size="sm"
          />
        </Box>

        <Text
          flex={1}
          minW={0}
          fontSize="xs"
          fontWeight="semibold"
          noOfLines={2}
        >
          {optionName}
        </Text>

        <CardAmountSummary
          net={totalNet}
          gross={totalGross}
          size="sm"
          layout="stacked"
        />

        {isEditMode && (
          <Box flexShrink={0} onClick={(e) => e.stopPropagation()}>
            <GhostActionButton
              label="Usuń"
              icon={<Trash2 size={13} />}
              variant="delete"
              onClick={() => onDeleteItem(option.id)}
              blendWithRow
            />
          </Box>
        )}
      </Flex>
    </Box>
  );
};
