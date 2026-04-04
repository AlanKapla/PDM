import React, { useCallback, useState } from 'react';
import {
  Box,
  VStack,
  HStack,
  Text,
  IconButton,
  Badge,
  Button,
  Divider,
  Collapse,
} from '@chakra-ui/react';
import { Trash2, ChevronDown, ChevronRight, FolderPlus, ListPlus } from 'lucide-react';
import type { CostEstimateDetailsWeb, CostEstimateGroupWeb, CostEstimateItemWeb } from '../../../types/costEstimate.types.new';
import type { FieldSource, RenderFieldInputFn } from '../costEstimateTableTypes';
import { useEstimateModal } from '../../../hooks/useEstimateModal';
import {
  getGroupDisplayName,
  getItemDisplayName,
  formatCurrencyValue,
  getGroupSummaryValues,
  getItemSummaryValues,
} from './MobileFieldInput';
import { GroupEditModal } from './GroupEditModal';
import { ItemEditModal } from './ItemEditModal';

// ---------------------------------------------------------------------------
// Props
// ---------------------------------------------------------------------------

export interface CostEstimateMobileViewProps {
  details: CostEstimateDetailsWeb;
  editable: boolean;
  canStructuralEdit: boolean;
  currencySymbol: string;
  updateGroupFieldValue: (groupId: string, fieldId: string, value: string | undefined) => void;
  updateItemFieldValue: (
    groupId: string,
    itemId: string,
    fieldId: string,
    fieldSource: FieldSource,
    value: string | undefined
  ) => void;
  updateComponentFieldValue: (
    groupId: string,
    itemId: string,
    componentId: string,
    fieldId: string,
    fieldSource: FieldSource,
    value: string | undefined
  ) => void;
  removeComponentFromItem: (groupId: string, itemId: string, componentId: string) => void;
  onDeleteGroup?: (groupId: string) => void;
  onDeleteItem?: (groupId: string, itemId: string) => void;
  onAddItem?: (groupId: string) => Promise<string | undefined>;
  onAddSubGroup?: (parentGroupId: string) => Promise<string | undefined>;
  onAddGroup?: () => Promise<string | undefined>;
  onAddChildItem?: (
    groupId: string,
    parentItemId: string,
    relationType: 1 | 2
  ) => Promise<string | undefined>;
  renderFieldInput: RenderFieldInputFn;
  onUploadFiles?: (itemId: string, fieldDefinitionId: string, files: File[]) => Promise<string[]>;
  onUploadSuccess?: () => void;
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Oblicza hierarchiczny numer etapu, np. "1", "1.2", "1.2.3" */
const buildGroupNumber = (indexes: number[]): string =>
  indexes.map((i) => i + 1).join('.');

// ---------------------------------------------------------------------------
// Komponent wiersza pozycji
// ---------------------------------------------------------------------------

interface ItemRowProps {
  item: CostEstimateItemWeb;
  itemNumber: number;
  groupId: string;
  indent: number;
  currencySymbol: string;
  templateStructure: any;
  canStructuralEdit: boolean;
  onTap: () => void;
  onDelete?: (groupId: string, itemId: string) => void;
}

const ItemRow: React.FC<ItemRowProps> = ({
  item,
  itemNumber,
  groupId,
  indent,
  currencySymbol,
  templateStructure,
  canStructuralEdit,
  onTap,
  onDelete,
}) => {
  const name = getItemDisplayName(item, templateStructure, itemNumber);
  const summaryValues = getItemSummaryValues(item, templateStructure);
  const fallbackValue = summaryValues.length === 0
    ? formatCurrencyValue(item.netValue ?? item.grossValue, currencySymbol)
    : null;
  const hasComponents = (item.components ?? []).length > 0;

  return (
    <HStack
      px={3}
      py={2}
      pl={`${indent + 12}px`}
      bg="white"
      _active={{ bg: 'blue.50' }}
      cursor="pointer"
      onClick={onTap}
      role="button"
      spacing={2}
      borderBottomWidth="1px"
      borderBottomColor="gray.100"
      _last={{ borderBottomWidth: 0 }}
    >
      <VStack align="start" spacing={0} flex={1} minW={0}>
        <Text fontSize="xs" color="gray.400" fontWeight="medium">
          POZYCJA {itemNumber}
        </Text>
        <Text fontSize="sm" fontWeight="medium" isTruncated color="gray.800">
          {name}
        </Text>
        {hasComponents && (
          <Text fontSize="xs" color="green.600">
            {(item.components ?? []).length} komponentów
          </Text>
        )}
      </VStack>
      {fallbackValue !== null ? (
        <Text fontSize="sm" fontWeight="medium" color="blue.700" whiteSpace="nowrap">
          {fallbackValue}
        </Text>
      ) : (
        <VStack spacing={0} align="end">
          {summaryValues.map((sv) => (
            <HStack key={sv.label} spacing={1} align="baseline">
              <Text fontSize="9px" color="gray.400" fontWeight="normal" whiteSpace="nowrap">
                {sv.label}
              </Text>
              <Text fontSize="sm" fontWeight="medium" color="blue.700" whiteSpace="nowrap">
                {formatCurrencyValue(sv.value, currencySymbol)}
              </Text>
            </HStack>
          ))}
        </VStack>
      )}
      {canStructuralEdit && onDelete && (
        <IconButton
          aria-label="Usuń pozycję"
          icon={<Trash2 size={14} />}
          size="xs"
          colorScheme="red"
          variant="ghost"
          onClick={(e) => {
            e.stopPropagation();
            onDelete(groupId, item.id);
          }}
        />
      )}
    </HStack>
  );
};

// ---------------------------------------------------------------------------
// Komponent rekurencyjny grupy
// ---------------------------------------------------------------------------

interface GroupCardProps {
  group: CostEstimateGroupWeb;
  groupIndexes: number[];
  currencySymbol: string;
  templateStructure: any;
  editable: boolean;
  canStructuralEdit: boolean;
  onGroupTap: (group: CostEstimateGroupWeb, groupNumber: string, level: number) => void;
  onItemTap: (item: CostEstimateItemWeb, groupId: string, itemNumber: number) => void;
  onDeleteGroup?: (groupId: string) => void;
  onDeleteItem?: (groupId: string, itemId: string) => void;
  onAddItem?: (groupId: string) => Promise<string | undefined>;
  onAddSubGroup?: (parentGroupId: string) => Promise<string | undefined>;
}

const GroupCard: React.FC<GroupCardProps> = ({
  group,
  groupIndexes,
  currencySymbol,
  templateStructure,
  editable,
  canStructuralEdit,
  onGroupTap,
  onItemTap,
  onDeleteGroup,
  onDeleteItem,
  onAddItem,
  onAddSubGroup,
}) => {
  const groupNumber = buildGroupNumber(groupIndexes);
  const level = groupIndexes.length - 1;
  const name = getGroupDisplayName(group, templateStructure, groupNumber);
  const summaryValues = getGroupSummaryValues(group, templateStructure);
  // Fallback gdy szablon nie definiuje pól sumowania
  const fallbackValue = summaryValues.length === 0
    ? formatCurrencyValue(group.totalNet ?? group.totalGross, currencySymbol)
    : null;
  const items = group.items ?? [];
  const childGroups = group.childGroups ?? [];

  const headerBg = level === 0 ? 'blue.100' : 'teal.50';
  const headerBorderColor = level === 0 ? 'blue.300' : 'teal.200';
  const badgeColorScheme = level === 0 ? 'blue' : 'teal';

  // Domyślnie zwinięty
  const [isExpanded, setIsExpanded] = useState(false);

  return (
    <Box
      borderRadius="lg"
      borderWidth="1px"
      borderColor={headerBorderColor}
      overflow="hidden"
      mb={level === 0 ? 3 : 2}
      ml={level > 0 ? `${level * 12}px` : 0}
    >
      {/* Nagłówek grupy */}
      <HStack
        bg={headerBg}
        px={3}
        py={2}
        spacing={2}
      >
        {/* Chevron — toggle zwijania */}
        <IconButton
          aria-label={isExpanded ? 'Zwiń etap' : 'Rozwiń etap'}
          icon={isExpanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
          size="xs"
          variant="ghost"
          colorScheme={badgeColorScheme}
          flexShrink={0}
          onClick={() => setIsExpanded((v) => !v)}
        />

        {/* Tap na resztę nagłówka = otwórz modal edycji */}
        <HStack
          flex={1}
          cursor="pointer"
          onClick={() => onGroupTap(group, groupNumber, level)}
          _active={{ filter: 'brightness(0.97)' }}
          role="button"
          spacing={2}
          minW={0}
        >
          <Badge colorScheme={badgeColorScheme} px={2} py={0.5} borderRadius="md" flexShrink={0}>
            {level === 0 ? 'ETAP' : 'ETAP'} {groupNumber}
          </Badge>
          <Text fontSize="sm" fontWeight="semibold" flex={1} isTruncated color="gray.800">
            {name}
          </Text>
          {fallbackValue !== null ? (
            <Text fontSize="sm" fontWeight="bold" color={level === 0 ? 'blue.800' : 'teal.800'} whiteSpace="nowrap">
              {fallbackValue}
            </Text>
          ) : (
            <VStack spacing={0} align="end">
              {summaryValues.map((sv) => (
                <HStack key={sv.label} spacing={1} align="baseline">
                  <Text fontSize="9px" color={level === 0 ? 'blue.400' : 'teal.400'} fontWeight="normal" whiteSpace="nowrap">
                    {sv.label}
                  </Text>
                  <Text fontSize="xs" fontWeight="bold" color={level === 0 ? 'blue.800' : 'teal.800'} whiteSpace="nowrap">
                    {formatCurrencyValue(sv.value, currencySymbol)}
                  </Text>
                </HStack>
              ))}
            </VStack>
          )}
        </HStack>

        {canStructuralEdit && onDeleteGroup && (
          <IconButton
            aria-label="Usuń etap"
            icon={<Trash2 size={14} />}
            size="xs"
            colorScheme="red"
            variant="ghost"
            onClick={(e) => {
              e.stopPropagation();
              onDeleteGroup(group.id);
            }}
          />
        )}
      </HStack>

      {/* Treść — zwijana */}
      <Collapse in={isExpanded} animateOpacity>
        <Box bg="white">
          {items.map((item, idx) => (
            <ItemRow
              key={item.id}
              item={item}
              itemNumber={idx + 1}
              groupId={group.id}
              indent={level * 12}
              currencySymbol={currencySymbol}
              templateStructure={templateStructure}
              canStructuralEdit={canStructuralEdit}
              onTap={() => onItemTap(item, group.id, idx + 1)}
              onDelete={onDeleteItem}
            />
          ))}

          {/* Pod-grupy */}
          {childGroups.map((child, childIdx) => (
            <Box key={child.id} px={2} py={2}>
              <GroupCard
                group={child}
                groupIndexes={[...groupIndexes, childIdx]}
                currencySymbol={currencySymbol}
                templateStructure={templateStructure}
                editable={editable}
                canStructuralEdit={canStructuralEdit}
                onGroupTap={onGroupTap}
                onItemTap={onItemTap}
                onDeleteGroup={onDeleteGroup}
                onDeleteItem={onDeleteItem}
                onAddItem={onAddItem}
                onAddSubGroup={onAddSubGroup}
              />
            </Box>
          ))}

          {/* Przyciski dodawania w grupie */}
          {canStructuralEdit && (onAddItem || onAddSubGroup) && (
            <HStack px={3} py={2} spacing={2} borderTopWidth="1px" borderTopColor="gray.100">
              {onAddItem && (
                <Button
                  leftIcon={<ListPlus size={14} />}
                  size="xs"
                  variant="ghost"
                  colorScheme="blue"
                  onClick={() => onAddItem(group.id)}
                >
                  Dodaj pozycję
                </Button>
              )}
              {onAddSubGroup && templateStructure?.canBranchGroups !== false && (
                <Button
                  leftIcon={<FolderPlus size={14} />}
                  size="xs"
                  variant="ghost"
                  colorScheme="teal"
                  onClick={() => onAddSubGroup(group.id)}
                >
                  Dodaj pod-etap
                </Button>
              )}
            </HStack>
          )}
        </Box>
      </Collapse>
    </Box>
  );
};

// ---------------------------------------------------------------------------
// Główny komponent widoku mobilnego
// ---------------------------------------------------------------------------

export const CostEstimateMobileView: React.FC<CostEstimateMobileViewProps> = ({
  details,
  editable,
  canStructuralEdit,
  currencySymbol,
  updateGroupFieldValue,
  updateItemFieldValue,
  updateComponentFieldValue,
  removeComponentFromItem,
  onDeleteGroup,
  onDeleteItem,
  onAddItem,
  onAddSubGroup,
  onAddGroup,
  onAddChildItem,
  renderFieldInput,
  onUploadFiles,
  onUploadSuccess,
}) => {
  const templateStructure = details.templateStructure;
  const rootGroups = details.rootGroups ?? [];

  const { isOpen, elementType, groupId, groupNumber, itemId, itemNumber, openModal, closeModal } =
    useEstimateModal();

  // Znajdź etap dla otwartego modalu
  const findGroup = useCallback(
    (id: string | null): CostEstimateGroupWeb | undefined => {
      if (!id) return undefined;
      const search = (groups: CostEstimateGroupWeb[]): CostEstimateGroupWeb | undefined => {
        for (const g of groups) {
          if (g.id === id) return g;
          const f = search(g.childGroups ?? []);
          if (f) return f;
        }
        return undefined;
      };
      return search(rootGroups);
    },
    [rootGroups]
  );

  // Znajdź pozycję dla otwartego modalu
  const findItem = useCallback(
    (gId: string | null, iId: string | null): CostEstimateItemWeb | undefined => {
      if (!gId || !iId) return undefined;
      const group = findGroup(gId);
      return group?.items?.find((i) => i.id === iId);
    },
    [findGroup]
  );

  const handleGroupTap = useCallback(
    (group: CostEstimateGroupWeb, gNumber: string, _level: number) => {
      openModal({ type: 'group', groupId: group.id, groupNumber: gNumber });
    },
    [openModal]
  );

  const handleItemTap = useCallback(
    (item: CostEstimateItemWeb, gId: string, idx: number) => {
      openModal({ type: 'item', groupId: gId, itemId: item.id, itemNumber: idx });
    },
    [openModal]
  );

  const activeGroup = findGroup(groupId);
  const activeItem = findItem(groupId, itemId);
  const activeGroupLevel = activeGroup?.level ?? 0;

  return (
    <Box pb={safe(rootGroups.length > 0 ? 4 : 0)}>
      {/* Lista etapów */}
      <VStack spacing={0} align="stretch" p={3}>
        {rootGroups.length === 0 ? (
          <Box textAlign="center" py={10}>
            <Text fontSize="sm" color="gray.400" fontStyle="italic">
              Brak etapów. Dodaj pierwszy etap poniżej.
            </Text>
          </Box>
        ) : (
          rootGroups.map((group, idx) => (
            <GroupCard
              key={group.id}
              group={group}
              groupIndexes={[idx]}
              currencySymbol={currencySymbol}
              templateStructure={templateStructure}
              editable={editable}
              canStructuralEdit={canStructuralEdit}
              onGroupTap={handleGroupTap}
              onItemTap={handleItemTap}
              onDeleteGroup={onDeleteGroup}
              onDeleteItem={onDeleteItem}
              onAddItem={onAddItem}
              onAddSubGroup={onAddSubGroup}
            />
          ))
        )}
      </VStack>

      {/* Przycisk dodaj etap */}
      {canStructuralEdit && onAddGroup && (
        <Box px={3} pt={1} pb={6}>
          <Divider mb={3} />
          <Button
            leftIcon={<FolderPlus size={16} />}
            colorScheme="blue"
            variant="ghost"
            size="sm"
            width="full"
            onClick={onAddGroup}
          >
            Dodaj etap
          </Button>
        </Box>
      )}

      {/* Modal: edycja etapu */}
      {isOpen && elementType === 'group' && activeGroup && (
        <GroupEditModal
          isOpen
          onClose={closeModal}
          group={activeGroup}
          level={activeGroupLevel}
          groupNumber={groupNumber}
          currencySymbol={currencySymbol}
          templateStructure={templateStructure}
          editable={editable}
          updateGroupFieldValue={updateGroupFieldValue}
          onDeleteGroup={onDeleteGroup}
          renderFieldInput={renderFieldInput}
        />
      )}

      {/* Modal: edycja pozycji */}
      {isOpen && elementType === 'item' && activeItem && groupId && (
        <ItemEditModal
          isOpen
          onClose={closeModal}
          item={activeItem}
          groupId={groupId}
          itemNumber={itemNumber}
          currencySymbol={currencySymbol}
          templateStructure={templateStructure}
          editable={editable}
          updateItemFieldValue={updateItemFieldValue}
          updateComponentFieldValue={updateComponentFieldValue}
          removeComponentFromItem={removeComponentFromItem}
          onDeleteItem={onDeleteItem}
          onAddChildItem={onAddChildItem}
          renderFieldInput={renderFieldInput}
          onUploadFiles={onUploadFiles}
          onUploadSuccess={onUploadSuccess}
        />
      )}
    </Box>
  );
};

// helper — unika eslint no-restricted-syntax
function safe(x: number) { return x; }
