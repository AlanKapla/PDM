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
import { CardNameText } from './CardNameText';
import type { ColumnDef } from '../TreeView/costEstimateColumnTypes';

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
  schemaColumns: ColumnDef[];
  depth: number;
  isExpanded: boolean;
  expandedGroups: Set<string>;
  onToggle: () => void;
  onToggleGroup: (groupId: string) => void;
  isEditMode: boolean;
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
  onDeleteGroup: (groupId: string) => void;
  onDeleteItem: (groupId: string, itemId: string) => void;
  onSelectOption: (groupId: string, itemId: string, optionId: string) => void;
  onOpenItemDetail?: (itemId: string, groupId: string) => void;
  onOpenGroupDetail?: (groupId: string, isSubStage: boolean) => void;
}

const SubStageSection: React.FC<SubStageSectionProps> = ({
  subGroup,
  currencySymbol,
  schemaColumns,
  depth,
  isExpanded,
  expandedGroups,
  onToggle,
  onToggleGroup,
  isEditMode,
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
  const totalNet = subGroup.totalNet ?? 0;
  const totalGross = subGroup.totalGross ?? 0;
  const hasSubGroups = (subGroup.childGroups?.length ?? 0) > 0;
  const hasItems = (subGroup.items?.length ?? 0) > 0;
  const subStageSurface = getGroupRowSurface(depth);

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

        <CardNameText
          name={subGroup.name}
          schemaColumns={schemaColumns}
          flex={1}
          minW={0}
          fontSize="sm"
          fontWeight="bold"
          noOfLines={2}
          cursor="pointer"
          onClick={handleOpenDetail}
        />

        <CardAmountSummary
          net={totalNet}
          gross={totalGross}
          currencySymbol={currencySymbol}
          size="md"
          layout="stacked"
        />

        {isEditMode && (
          <HStack spacing={0.5} flexShrink={0} pl={2} ml={1} borderLeft="1px solid" borderColor="neutral.200">
            <GhostActionButton
              label="Dodaj pozycję"
              icon={<Box as="span" fontSize="sm" fontWeight="bold" lineHeight="1">P+</Box>}
              variant="add"
              onClick={() => onAddItem(subGroup.id)}
              blendWithRow
            />
            <GhostActionButton
              label="Dodaj podetap"
              icon={<Box as="span" fontSize="sm" fontWeight="bold" lineHeight="1">E+</Box>}
              variant="add"
              onClick={() => onAddSubGroup(subGroup.id)}
              blendWithRow
            />
            <GhostActionButton
              label="Usuń podetap"
              icon={<Trash2 size={14} />}
              variant="delete"
              onClick={() => onDeleteGroup(subGroup.id)}
              blendWithRow
            />
          </HStack>
        )}
      </Flex>

      <Collapse in={isExpanded} animateOpacity>
        <VStack spacing={2} align="stretch" pl={7} pr={2} pb={3} mt={1}>
          {!hasItems && !hasSubGroups ? (
            <Text fontSize="sm" color="neutral.500" py={2}>
              Brak pozycji
            </Text>
          ) : (
            <>
              {hasSubGroups &&
                subGroup.childGroups!.map((childGroup) => (
                  <SubStageSection
                    key={childGroup.id}
                    subGroup={childGroup}
                    currencySymbol={currencySymbol}
                    schemaColumns={schemaColumns}
                    depth={depth + 1}
                    isExpanded={expandedGroups.has(childGroup.id)}
                    expandedGroups={expandedGroups}
                    onToggle={() => onToggleGroup(childGroup.id)}
                    onToggleGroup={onToggleGroup}
                    isEditMode={isEditMode}
                    onFieldChange={onFieldChange}
                    onFieldAutosave={onFieldAutosave}
                    onAddItem={onAddItem}
                    onAddSubGroup={onAddSubGroup}
                    onAddComponent={onAddComponent}
                    onAddOption={onAddOption}
                    onDeleteGroup={onDeleteGroup}
                    onDeleteItem={onDeleteItem}
                    onSelectOption={onSelectOption}
                    onOpenItemDetail={onOpenItemDetail}
                    onOpenGroupDetail={onOpenGroupDetail}
                  />
                ))}

              {hasItems &&
                subGroup.items.map((item) => (
                  <PositionCard
                    key={item.id}
                    item={item}
                    groupId={subGroup.id}
                    currencySymbol={currencySymbol}
                    schemaColumns={schemaColumns}
                    isEditMode={isEditMode}
                    onFieldChange={onFieldChange}
                    onFieldAutosave={onFieldAutosave}
                    onAddComponent={onAddComponent}
                    onAddOption={onAddOption}
                    onDeleteItem={(itemId) => onDeleteItem(subGroup.id, itemId)}
                    onSelectOption={onSelectOption}
                    onOpenItemDetail={onOpenItemDetail}
                  />
                ))}
            </>
          )}

          {isEditMode && (
            <HStack spacing={2}>
              <AddInlineButton onClick={() => onAddItem(subGroup.id)}>
                Dodaj pozycję
              </AddInlineButton>
              <AddInlineButton onClick={() => onAddSubGroup(subGroup.id)}>
                Dodaj podetap
              </AddInlineButton>
            </HStack>
          )}
        </VStack>
      </Collapse>
    </Box>
  );
};

interface StageCardProps {
  stage: CostEstimateGroupWeb;
  currencySymbol: string;
  schemaColumns: ColumnDef[];
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
  onDeleteGroup: (groupId: string) => void;
  onDeleteItem: (groupId: string, itemId: string) => void;
  onSelectOption: (groupId: string, itemId: string, optionId: string) => void;
  onOpenItemDetail?: (itemId: string, groupId: string) => void;
  onOpenGroupDetail?: (groupId: string, isSubStage: boolean) => void;
}

export const StageCard: React.FC<StageCardProps> = ({
  stage,
  currencySymbol,
  schemaColumns,
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
      data-ce-group-id={stage.id}
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

        <CardNameText
          name={stage.name}
          schemaColumns={schemaColumns}
          flex={1}
          minW={0}
          fontSize="md"
          fontWeight="bold"
          noOfLines={2}
          cursor="pointer"
          onClick={handleOpenDetail}
        />

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
              onClick={() => onDeleteGroup(stage.id)}
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
                  schemaColumns={schemaColumns}
                  depth={1}
                  isExpanded={expandedGroups.has(subGroup.id)}
                  expandedGroups={expandedGroups}
                  onToggle={() => onToggleGroup(subGroup.id)}
                  onToggleGroup={onToggleGroup}
                  isEditMode={isEditMode}
                  onFieldChange={onFieldChange}
                  onFieldAutosave={onFieldAutosave}
                  onAddItem={onAddItem}
                  onAddSubGroup={onAddSubGroup}
                  onAddComponent={onAddComponent}
                  onAddOption={onAddOption}
                  onDeleteGroup={onDeleteGroup}
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
                  schemaColumns={schemaColumns}
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
