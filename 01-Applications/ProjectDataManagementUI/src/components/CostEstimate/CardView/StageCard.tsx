/**
 * Stage Card - Accordion card for top-level group (etap)
 * Read-only header: name + net/gross; editing in modal on click
 */

import React from 'react';
import {
  Box,
  Flex,
  HStack,
  VStack,
  Text,
  Collapse,
} from '@chakra-ui/react';
import { ChevronDown, Trash2 } from 'lucide-react';
import type { CostEstimateGroupWeb } from '../../../types/costEstimate.types.new';
import { GhostActionButton, AddInlineButton } from '../PrototypeActionButtons';
import { PositionCard } from './PositionCard';
import { ADD_ROW_SURFACE, getGroupRowSurface } from '../TreeView/treeViewRowSurfaces';
import { CardAmountSummary } from './CardAmountSummary';

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

interface SubStageSectionProps {
  subGroup: CostEstimateGroupWeb;
  currencySymbol: string;
  isExpanded: boolean;
  onToggle: () => void;
  isEditMode: boolean;
  onFieldChange: (
    groupId: string,
    itemId: string | null,
    fieldId: string,
    value: string | number | boolean | null
  ) => void;
  onFieldAutosave?: (params: AutosaveParams) => void;
  onAddItem: (groupId: string) => void;
  onAddComponent: (groupId: string, itemId: string) => void;
  onAddOption: (groupId: string, itemId: string) => void;
  onDeleteItem: (groupId: string, itemId: string) => void;
  onSelectOption: (groupId: string, itemId: string, optionId: string) => void;
  onOpenItemDetail?: (itemId: string, groupId: string) => void;
  onOpenGroupDetail?: (groupId: string, isSubStage: boolean) => void;
}

const SubStageSection: React.FC<SubStageSectionProps> = ({
  subGroup,
  currencySymbol,
  isExpanded,
  onToggle,
  isEditMode,
  onFieldChange,
  onFieldAutosave,
  onAddItem,
  onAddComponent,
  onAddOption,
  onDeleteItem,
  onSelectOption,
  onOpenItemDetail,
  onOpenGroupDetail,
}) => {
  const subGroupName = subGroup.name || 'Podetap';
  const totalNet = subGroup.totalNet ?? 0;
  const totalGross = subGroup.totalGross ?? 0;
  const subStageSurface = getGroupRowSurface(1);

  const handleOpenDetail = () => {
    onOpenGroupDetail?.(subGroup.id, true);
  };

  return (
    <Box
      bg={subStageSurface.bg}
      borderRadius="14px"
      p={1}
      mt={2}
      border="1px solid"
      borderColor="neutral.100"
      overflow="hidden"
    >
      <Flex
        align="center"
        gap={2.5}
        px={3}
        py={2}
        bg={subStageSurface.bg}
        transition="background 0.12s"
        _hover={{ bg: subStageSurface.hoverBg }}
      >
        <Box
          transition="transform 0.18s"
          transform={isExpanded ? 'rotate(0deg)' : 'rotate(-90deg)'}
          color="neutral.600"
          cursor="pointer"
          flexShrink={0}
          onClick={onToggle}
        >
          <ChevronDown size={15} />
        </Box>

        <Text
          flex={1}
          minW={0}
          fontSize="sm"
          fontWeight="bold"
          noOfLines={2}
          cursor="pointer"
          onClick={handleOpenDetail}
        >
          {subGroupName}
        </Text>

        <CardAmountSummary
          net={totalNet}
          gross={totalGross}
          currencySymbol={currencySymbol}
          size="md"
          layout="stacked"
        />
      </Flex>

      <Collapse in={isExpanded} animateOpacity>
        <VStack spacing={2} align="stretch" pl={7} pr={2} pb={3} mt={1}>
          {subGroup.items.length === 0 ? (
            <Text fontSize="sm" color="neutral.500" py={2}>
              Brak pozycji
            </Text>
          ) : (
            subGroup.items.map((item) => (
              <PositionCard
                key={item.id}
                item={item}
                groupId={subGroup.id}
                currencySymbol={currencySymbol}
                isEditMode={isEditMode}
                onFieldChange={onFieldChange}
                onFieldAutosave={onFieldAutosave}
                onAddComponent={onAddComponent}
                onAddOption={onAddOption}
                onDeleteItem={(itemId) => onDeleteItem(subGroup.id, itemId)}
                onSelectOption={onSelectOption}
                onOpenItemDetail={onOpenItemDetail}
              />
            ))
          )}

          {isEditMode && (
            <AddInlineButton onClick={() => onAddItem(subGroup.id)}>
              Dodaj pozycję
            </AddInlineButton>
          )}
        </VStack>
      </Collapse>
    </Box>
  );
};

interface StageCardProps {
  stage: CostEstimateGroupWeb;
  currencySymbol: string;
  isExpanded: boolean;
  expandedGroups: Set<string>;
  isEditMode: boolean;
  onToggle: () => void;
  onToggleGroup: (groupId: string) => void;
  onFieldChange: (
    groupId: string,
    itemId: string | null,
    fieldId: string,
    value: string | number | boolean | null
  ) => void;
  onFieldAutosave?: (params: AutosaveParams) => void;
  onAddItem: (groupId: string) => void;
  onAddSubGroup: (parentGroupId: string) => void;
  onAddComponent: (groupId: string, itemId: string) => void;
  onAddOption: (groupId: string, itemId: string) => void;
  onDeleteGroup: () => void;
  onDeleteItem: (groupId: string, itemId: string) => void;
  onSelectOption: (groupId: string, itemId: string, optionId: string) => void;
  onOpenItemDetail?: (itemId: string, groupId: string) => void;
  onOpenGroupDetail?: (groupId: string, isSubStage: boolean) => void;
}

export const StageCard: React.FC<StageCardProps> = ({
  stage,
  currencySymbol,
  isExpanded,
  expandedGroups,
  isEditMode,
  onToggle,
  onToggleGroup,
  onFieldChange,
  onFieldAutosave,
  onAddItem,
  onAddSubGroup,
  onAddComponent,
  onAddOption,
  onDeleteGroup,
  onDeleteItem,
  onSelectOption,
  onOpenItemDetail,
  onOpenGroupDetail,
}) => {
  const stageName = stage.name || 'Bez nazwy';
  const totalNet = stage.totalNet ?? 0;
  const totalGross = stage.totalGross ?? 0;
  const hasSubGroups = (stage.childGroups?.length ?? 0) > 0;
  const hasItems = (stage.items?.length ?? 0) > 0;
  const stageSurface = getGroupRowSurface(0);

  const handleOpenDetail = () => {
    onOpenGroupDetail?.(stage.id, false);
  };

  return (
    <Box
      bg={stageSurface.bg}
      border="1px solid"
      borderColor="neutral.100"
      borderRadius="18px"
      overflow="hidden"
    >
      <Flex
        align="center"
        gap={{ base: 2, md: 3.5 }}
        px={{ base: 3, md: 5 }}
        py={{ base: 3, md: 4 }}
        bg={stageSurface.bg}
        borderBottom={isExpanded ? '1px solid' : 'none'}
        borderColor="neutral.100"
        className="trow"
        transition="background 0.12s"
        _hover={{ bg: stageSurface.hoverBg }}
      >
        <Flex
          w="30px"
          h="30px"
          borderRadius="9px"
          align="center"
          justify="center"
          color="neutral.600"
          bg="transparent"
          transition="transform 0.18s"
          transform={isExpanded ? 'rotate(0deg)' : 'rotate(-90deg)'}
          cursor="pointer"
          flexShrink={0}
          onClick={onToggle}
        >
          <ChevronDown size={18} />
        </Flex>

        <Text
          flex={1}
          minW={0}
          fontSize="md"
          fontWeight="bold"
          noOfLines={2}
          cursor="pointer"
          onClick={handleOpenDetail}
        >
          {stageName}
        </Text>

        <CardAmountSummary
          net={totalNet}
          gross={totalGross}
          currencySymbol={currencySymbol}
          size="lg"
          layout="stacked"
        />

        {isEditMode && (
          <HStack
            spacing={0.5}
            flexShrink={0}
            pl={{ base: 0, md: 3 }}
            ml={{ base: 0, md: 1 }}
            borderLeft={{ base: 'none', md: '1px solid' }}
            borderColor="neutral.200"
          >
            <GhostActionButton
              label="Dodaj pozycję"
              icon={<Box as="span" fontSize="sm" fontWeight="bold" lineHeight="1">P+</Box>}
              variant="add"
              onClick={() => onAddItem(stage.id)}
              blendWithRow
            />
            <GhostActionButton
              label="Dodaj podetap"
              icon={<Box as="span" fontSize="sm" fontWeight="bold" lineHeight="1">E+</Box>}
              variant="add"
              onClick={() => onAddSubGroup(stage.id)}
              blendWithRow
            />
            <GhostActionButton
              label="Usuń etap"
              icon={<Trash2 size={15} />}
              variant="delete"
              onClick={onDeleteGroup}
              blendWithRow
            />
          </HStack>
        )}
      </Flex>

      <Collapse in={isExpanded} animateOpacity>
        <Box px={{ base: 3, md: 5 }} py={2} pb={{ base: 3, md: 4.5 }} bg={ADD_ROW_SURFACE.bg}>
          {!hasItems && !hasSubGroups ? (
            <Box py={4} textAlign="center">
              <Text fontSize="sm" color="neutral.500">
                Brak pozycji. Kliknij &quot;Dodaj pozycję&quot; aby rozpocząć.
              </Text>
            </Box>
          ) : (
            <VStack spacing={3} align="stretch" mt={2}>
              {hasSubGroups && stage.childGroups!.map((subGroup) => (
                <SubStageSection
                  key={subGroup.id}
                  subGroup={subGroup}
                  currencySymbol={currencySymbol}
                  isExpanded={expandedGroups.has(subGroup.id)}
                  onToggle={() => onToggleGroup(subGroup.id)}
                  isEditMode={isEditMode}
                  onFieldChange={onFieldChange}
                  onFieldAutosave={onFieldAutosave}
                  onAddItem={onAddItem}
                  onAddComponent={onAddComponent}
                  onAddOption={onAddOption}
                  onDeleteItem={onDeleteItem}
                  onSelectOption={onSelectOption}
                  onOpenItemDetail={onOpenItemDetail}
                  onOpenGroupDetail={onOpenGroupDetail}
                />
              ))}

              {hasItems && stage.items.map((item) => (
                <PositionCard
                  key={item.id}
                  item={item}
                  groupId={stage.id}
                  currencySymbol={currencySymbol}
                  isEditMode={isEditMode}
                  onFieldChange={onFieldChange}
                  onFieldAutosave={onFieldAutosave}
                  onAddComponent={onAddComponent}
                  onAddOption={onAddOption}
                  onDeleteItem={(itemId) => onDeleteItem(stage.id, itemId)}
                  onSelectOption={onSelectOption}
                  onOpenItemDetail={onOpenItemDetail}
                />
              ))}
            </VStack>
          )}

          {isEditMode && (
            <HStack mt={3} spacing={2}>
              <AddInlineButton onClick={() => onAddItem(stage.id)}>
                Dodaj pozycję
              </AddInlineButton>
              <AddInlineButton onClick={() => onAddSubGroup(stage.id)}>
                Dodaj podetap
              </AddInlineButton>
            </HStack>
          )}
        </Box>
      </Collapse>
    </Box>
  );
};
