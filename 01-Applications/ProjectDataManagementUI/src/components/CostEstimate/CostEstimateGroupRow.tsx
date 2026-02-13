import React from 'react';
import {
  Box,
  IconButton,
  Text,
  Button,
  Tooltip,
  Badge,
  Divider,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
} from '@chakra-ui/react';
import {
  Trash2,
  Plus,
  PlusCircle,
  GripVertical,
} from 'lucide-react';
import { WorkScopeItemRow, type WorkScopeFieldDefinition } from './WorkScopeItemRow';
import type {
  CostEstimateGroupDto,
  CostEstimateGroupFieldValueDto,
  CostEstimateItemDto,
} from '../../types/costEstimate.types.new';
import { createEmptyGroup, createEmptyItem } from '../../types/costEstimate.types.new';

/**
 * Field definition for group header
 */
export interface GroupHeaderFieldDefinition {
  id: string;
  label: string;
  fieldType: string;
  isRequired: boolean;
  allowedValues?: string[];
  order: number;
  helpText?: string;
}

/**
 * Template configuration for groups
 */
export interface GroupTemplateConfig {
  canAddGroups: boolean;
  canBranchGroups: boolean;
  maxGroupLevel?: number;
}

/**
 * Props for CostEstimateGroupRow component
 */
export interface CostEstimateGroupRowProps {
  /** Group data */
  group: CostEstimateGroupDto;
  /** Header field definitions */
  headerFieldDefinitions: GroupHeaderFieldDefinition[];
  /** Work scope field definitions */
  workScopeFieldDefinitions: WorkScopeFieldDefinition[];
  /** Template configuration */
  templateConfig: GroupTemplateConfig;
  /** Change handler */
  onChange: (updatedGroup: CostEstimateGroupDto) => void;
  /** Delete handler */
  onDelete: () => void;
  /** Whether group is readonly */
  readonly?: boolean;
  /** Group index for display */
  index?: number;
  /** Show drag handle */
  showDragHandle?: boolean;
}

/**
 * CostEstimateGroupRow - Recursive component for cost estimate group
 * Displays groups and work scope items in table format
 */
export const CostEstimateGroupRow: React.FC<CostEstimateGroupRowProps> = ({
  group,
  headerFieldDefinitions,
  workScopeFieldDefinitions,
  templateConfig,
  onChange,
  onDelete,
  readonly = false,
  index,
  showDragHandle = false,
}) => {
  // Sort header fields and work scope fields by order
  const sortedHeaderFields = [...headerFieldDefinitions].sort((a, b) => a.order - b.order);
  const sortedWorkScopeFields = [...workScopeFieldDefinitions].sort((a, b) => a.order - b.order);

  // Calculate indentation based on group level
  const indentLeft = group.level * 16;

  // Check if child groups can be added
  const canAddChildGroups =
    templateConfig.canBranchGroups &&
    (templateConfig.maxGroupLevel === undefined || group.level < templateConfig.maxGroupLevel);

  // Get group name for display
  const getGroupName = (): string => {
    const nameField = sortedHeaderFields.find(f => 
      f.label.toLowerCase().includes('name') || 
      f.label.toLowerCase().includes('nazwa')
    );
    
    if (nameField) {
      const fieldValue = group.fieldValues.find((fv) => fv.fieldDefinitionId === nameField.id);
      if (fieldValue?.value) return fieldValue.value;
    }

    return `Grupa ${(index ?? 0) + 1}`;
  };

  // Add work scope item
  const handleAddWorkScopeItem = () => {
    const newItem = createEmptyItem((group.items || []).length);
    
    onChange({
      ...group,
      items: [...(group.items || []), newItem],
    });
  };

  // Update work scope item
  const handleUpdateWorkScopeItem = (itemIndex: number, updatedItem: CostEstimateItemDto) => {
    const newItems = [...(group.items || [])];
    newItems[itemIndex] = updatedItem;
    
    onChange({
      ...group,
      items: newItems,
    });
  };

  // Delete work scope item
  const handleDeleteWorkScopeItem = (itemIndex: number) => {
    const newItems = (group.items || []).filter((_: CostEstimateItemDto, idx: number) => idx !== itemIndex);
    
    onChange({
      ...group,
      items: newItems,
    });
  };

  // Add child group
  const handleAddChildGroup = () => {
    const newChildGroup = createEmptyGroup(
      group.level + 1,
      group.childGroups.length,
      group.id
    );
    
    onChange({
      ...group,
      childGroups: [...group.childGroups, newChildGroup],
    });
  };

  // Update child group
  const handleUpdateChildGroup = (childIndex: number, updatedChild: CostEstimateGroupDto) => {
    const newChildren = [...group.childGroups];
    newChildren[childIndex] = updatedChild;
    
    onChange({
      ...group,
      childGroups: newChildren,
    });
  };

  // Delete child group
  const handleDeleteChildGroup = (childIndex: number) => {
    const newChildren = group.childGroups.filter((_, idx) => idx !== childIndex);
    
    onChange({
      ...group,
      childGroups: newChildren,
    });
  };

  return (
    <Box
      mb={3}
      ml={`${indentLeft}px`}
      borderWidth={group.level === 0 ? '2px' : '1px'}
      borderColor={group.level === 0 ? 'blue.500' : 'gray.200'}
      borderRadius="md"
      shadow={group.level === 0 ? 'md' : 'sm'}
      bg="white"
    >
      {/* Group Header */}
      <Box
        display="flex"
        alignItems="center"
        p={2}
        bg={group.level === 0 ? 'blue.50' : 'gray.50'}
        borderBottomWidth="1px"
        borderColor="gray.200"
      >
        {showDragHandle && (
          <IconButton size="sm" mr={2} cursor="grab" aria-label="Przeciągnij grupę" variant="ghost">
            <GripVertical size={16} />
          </IconButton>
        )}

        <Badge
          colorScheme={group.level === 0 ? 'blue' : 'gray'}
          mr={2}
        >
          L{group.level}
        </Badge>

        {index !== undefined && (
          <Badge mr={2} minW={8}>
            {index + 1}
          </Badge>
        )}

        <Box flex={1}>
          <Text fontSize={group.level === 0 ? 'lg' : 'md'} fontWeight="semibold">
            {getGroupName()}
          </Text>
          <Text fontSize="xs" color="gray.600">
            {(group.items || []).length} pozycji • {group.childGroups.length} podgrup
          </Text>
        </Box>

        {!readonly && (
          <Tooltip label="Usuń grupę">
            <IconButton
              size="sm"
              colorScheme="red"
              onClick={onDelete}
              aria-label="Usuń grupę"
              icon={<Trash2 size={16} />}
              variant="ghost"
            />
          </Tooltip>
        )}
      </Box>

      {/* Work Scope Items Table */}
      <Box p={2}>
        <Box display="flex" alignItems="center" mb={2}>
          <Text fontSize="sm" fontWeight="semibold" flex={1}>
            Pozycje kosztorysu
          </Text>
          {!readonly && (
            <Button
              size="sm"
              leftIcon={<Plus size={16} />}
              onClick={handleAddWorkScopeItem}
              variant="outline"
            >
              Dodaj pozycję
            </Button>
          )}
        </Box>

        {(group.items || []).length === 0 ? (
          <Text fontSize="sm" color="gray.600" fontStyle="italic" py={2}>
            Brak pozycji. Kliknij "Dodaj pozycję" aby utworzyć pierwszą pozycję.
          </Text>
        ) : (
          <Box overflowX="auto">
            <Table size="sm" variant="simple">
              <Thead>
                <Tr>
                  {showDragHandle && <Th width="40px"></Th>}
                  <Th width="60px">#</Th>
                  {sortedWorkScopeFields.map((fieldDef) => (
                    <Th key={fieldDef.id}>
                      {fieldDef.label}
                      {fieldDef.isRequired && <Text as="span" color="red.500"> *</Text>}
                      {fieldDef.unit && <Text as="span" fontSize="xs" color="gray.500"> ({fieldDef.unit})</Text>}
                    </Th>
                  ))}
                  {!readonly && <Th width="60px">Akcje</Th>}
                </Tr>
              </Thead>
              <Tbody>
                {(group.items || []).map((item: CostEstimateItemDto, itemIndex: number) => (
                  <WorkScopeItemRow
                    key={itemIndex}
                    item={item}
                    fieldDefinitions={sortedWorkScopeFields}
                    onChange={(updated) => handleUpdateWorkScopeItem(itemIndex, updated)}
                    onDelete={() => handleDeleteWorkScopeItem(itemIndex)}
                    readonly={readonly}
                    index={itemIndex}
                    showDragHandle={showDragHandle}
                  />
                ))}
              </Tbody>
            </Table>
          </Box>
        )}
      </Box>

      {/* Child Groups */}
      {(canAddChildGroups || group.childGroups.length > 0) && (
        <Box p={2} pt={0}>
          <Divider my={2} />
          
          <Box display="flex" alignItems="center" mb={2}>
            <Text fontSize="sm" fontWeight="semibold" flex={1}>
              Podgrupy
            </Text>
            {!readonly && canAddChildGroups && (
              <Button
                size="sm"
                leftIcon={<PlusCircle size={16} />}
                onClick={handleAddChildGroup}
                variant="outline"
                colorScheme="blue"
              >
                Dodaj podgrupę
              </Button>
            )}
          </Box>

          {group.childGroups.length === 0 ? (
            <Text fontSize="sm" color="gray.600" fontStyle="italic" py={2}>
              {canAddChildGroups
                ? 'Brak podgrup. Kliknij "Dodaj podgrupę" aby utworzyć zagnieżdżoną grupę.'
                : 'Podgrupy nie są dozwolone w tym szablonie.'}
            </Text>
          ) : (
            <Box>
              {group.childGroups.map((childGroup, childIndex) => (
                <CostEstimateGroupRow
                  key={childIndex}
                  group={childGroup}
                  headerFieldDefinitions={headerFieldDefinitions}
                  workScopeFieldDefinitions={sortedWorkScopeFields}
                  templateConfig={templateConfig}
                  onChange={(updated) => handleUpdateChildGroup(childIndex, updated)}
                  onDelete={() => handleDeleteChildGroup(childIndex)}
                  readonly={readonly}
                  index={childIndex}
                  showDragHandle={showDragHandle}
                />
              ))}
            </Box>
          )}
        </Box>
      )}
    </Box>
  );
};
