import React from 'react';
import {
  Tr,
  Td,
  Text,
  IconButton,
  Tooltip,
  Badge,
  HStack,
  Box,
} from '@chakra-ui/react';
import {
  GripVertical,
  Trash2,
  ChevronDown,
  ChevronRight,
} from 'lucide-react';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import type {
  CostEstimateGroupWeb,
  CostEstimateItemWeb,
} from '../../../types/costEstimate.types.new';
import type { AllItemValues } from '../../../utils/costEstimateCalculations';
import type {
  ExpandedColumn,
  RenderFieldInputFn,
  FormatDisplayValueFn,
  GetColumnWidthFn,
} from '../costEstimateTableTypes';


// ---------------------------------------------------------------------------
// Props
// ---------------------------------------------------------------------------

export interface SortableGroupRowProps {
  id: string;
  group: CostEstimateGroupWeb;
  level: number;
  indent: number;
  groupNumber: string;
  isCollapsed: boolean;
  editable: boolean;
  /** Czy user może edytować wartości pól (false tylko dla trybu podglądu). Niezależne od canStructuralEdit. */
  canEditFields: boolean;
  templateStructure: any;
  showGroupSummary: boolean;
  groupSummaryFields: any[];
  currencySymbol: string;
  columns: ExpandedColumn[];
  /** Liczba kolumn pozycji — tyle samo pustych Td trzeba wyrenderować dla wyrównania */
  itemColumnCount: number;
  /** Szerokość kolumny expand (z uwzględnieniem poziomu zagnieżdżenia) */
  expandColWidth: number;
  getColumnWidth: GetColumnWidthFn;
  getGroupFieldValue: (group: CostEstimateGroupWeb, fieldId: string) => string | undefined;
  updateGroupFieldValue: (groupId: string, fieldId: string, value: string | undefined) => void;
  renderFieldInput: RenderFieldInputFn;
  formatDisplayValue: FormatDisplayValueFn;
  toggleGroupCollapse: (groupId: string) => void;
  onAddItem?: (groupId: string) => void;
  onAddSubGroup?: (parentGroupId: string) => void;
  onDeleteGroup?: (groupId: string) => void;
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Zbiera wszystkie pozycje z grupy i jej podgrup */
const collectGroupItems = (g: CostEstimateGroupWeb): CostEstimateItemWeb[] => {
  let items: CostEstimateItemWeb[] = [];
  if (g.items) items = items.concat(g.items);
  if (g.childGroups) {
    for (const childGroup of g.childGroups) {
      items = items.concat(collectGroupItems(childGroup));
    }
  }
  return items;
};

// ---------------------------------------------------------------------------
// Komponent
// ---------------------------------------------------------------------------

export const SortableGroupRow: React.FC<SortableGroupRowProps> = ({
  id,
  group,
  level,
  indent,
  groupNumber,
  isCollapsed,
  editable,
  canEditFields,
  templateStructure,
  showGroupSummary,
  groupSummaryFields,
  currencySymbol,
  columns: columnsProp,
  itemColumnCount,
  expandColWidth,
  getColumnWidth,
  getGroupFieldValue,
  updateGroupFieldValue,
  renderFieldInput,
  formatDisplayValue,
  toggleGroupCollapse,
  onAddItem,
  onAddSubGroup,
  onDeleteGroup,
}) => {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <Tr
      ref={setNodeRef}
      style={style}
      bg={level === 0 ? 'primary.100' : 'primary.50'}
      borderTopWidth="1px"
      borderTopColor="neutral.200"
      _hover={{ bg: level === 0 ? 'primary.200' : 'primary.100', cursor: 'pointer' }}
    >
      {/* Akcje grupy - zamrożona kolumna */}
      {editable && (
        <Td
          px={3}
          py={2}
          textAlign="center"
          position="sticky"
          left={0}
          zIndex={5}
          bg={level === 0 ? 'primary.100' : 'primary.50'}
          boxShadow={level === 0
            ? 'inset 3px 0 0 var(--chakra-colors-primary-400)'
            : 'inset 2px 0 0 var(--chakra-colors-primary-300)'}
          minW="120px"
          maxW="120px"
        >
          <HStack spacing={1} justify="center">
            <Tooltip label="Przeciągnij aby zmienić kolejność">
              <IconButton
                aria-label="Przeciągnij"
                icon={<GripVertical size={14} />}
                size="xs"
                variant="ghost"
                cursor="grab"
                {...attributes}
                {...listeners}
              />
            </Tooltip>
            <Box display="inline-flex" alignItems="center" gap={1}>
            {onAddItem && (
              <Tooltip label="Dodaj pozycję">
                <IconButton
                  aria-label="Dodaj pozycję"
                  icon={<Text fontWeight="bold" fontSize="xs" lineHeight="1">P+</Text>}
                  size="xs"
                  colorScheme="green"
                  variant="ghost"
                  onClick={() => onAddItem(group.id)}
                />
              </Tooltip>
            )}
            {onAddSubGroup && templateStructure?.canBranchGroups !== false && (() => {
              const maxLevel = templateStructure?.maxGroupLevel;
              const isMaxLevelReached = maxLevel != null && level >= maxLevel;
              return isMaxLevelReached ? (
                <Tooltip label={`Osiągnięto maksymalny poziom zagnieżdżenia (${maxLevel})`}>
                  <IconButton
                    aria-label="Dodaj podetap"
                    icon={<Text fontWeight="bold" fontSize="xs" lineHeight="1">E+</Text>}
                    size="xs"
                    colorScheme="gray"
                    variant="ghost"
                    isDisabled
                  />
                </Tooltip>
              ) : (
                <Tooltip label="Dodaj podetap">
                  <IconButton
                    aria-label="Dodaj podetap"
                    icon={<Text fontWeight="bold" fontSize="xs" lineHeight="1">E+</Text>}
                    size="xs"
                  colorScheme="primary"
                    variant="ghost"
                    onClick={() => onAddSubGroup(group.id)}
                  />
                </Tooltip>
              );
            })()}
            {onDeleteGroup && (
              <Tooltip label="Usuń etap">
                <IconButton
                  aria-label="Usuń etap"
                  icon={<Trash2 size={14} />}
                  size="xs"
                  colorScheme="red"
                  variant="ghost"
                  onClick={() => onDeleteGroup(group.id)}
                />
              </Tooltip>
            )}
            </Box>
          </HStack>
        </Td>
      )}

      {/* Expand/collapse button dla grupy */}
      <Td
        p={2}
        pl={`${indent + 12}px`}
        w={`${expandColWidth}px`}
        minW={`${expandColWidth}px`}
        bg={level === 0 ? 'primary.100' : 'primary.50'}
      >
        <Tooltip label={isCollapsed ? 'Rozwiń etap' : 'Zwiń etap'}>
          <IconButton
            aria-label={isCollapsed ? 'Rozwiń' : 'Zwiń'}
            icon={isCollapsed ? <ChevronRight size={16} /> : <ChevronDown size={16} />}
            size="xs"
            variant="ghost"
            onClick={() => toggleGroupCollapse(group.id)}
          />
        </Tooltip>
      </Td>

      {/* Kolumny pól grup */}
      {columnsProp.map((col: any) => {
        const colWidth = getColumnWidth(col.fieldId, col.width, col.label);

        if (col.type === 'childField') {
          return (
              <Td key={col.fieldId} p={2} bg={level === 0 ? 'primary.100' : 'primary.50'} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`}>
              <Text fontSize="xs" color="neutral.400" fontStyle="italic" textAlign="center">—</Text>
            </Td>
          );
        }

        const groupHeaderField = templateStructure.groupHeaderFields?.find(
          (f: any) => f.fieldName === col.originalColumn.fieldName
        );

        if (groupHeaderField) {
          const value = getGroupFieldValue(group, groupHeaderField.id);
          return (
            <Td key={col.fieldId} p={2} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`}>
              {canEditFields ? (
                renderFieldInput(groupHeaderField, value, (newValue) =>
                  updateGroupFieldValue(group.id, groupHeaderField.id, newValue)
                )
              ) : (
                <Text fontSize="sm" fontWeight="medium">
                  {formatDisplayValue(value, groupHeaderField)}
                </Text>
              )}
            </Td>
          );
        }

        // Sprawdź czy pole ma sumowanie w grupie
        const systemField = templateStructure.systemFields?.find(
          (f: any) => f.id === col.fieldId || f.fieldName === col.originalColumn.fieldName
        );
        const calcField = templateStructure.calculatedFields?.find(
          (f: any) => f.id === col.fieldId || f.fieldName === col.originalColumn.fieldName
        );
        const genericField = templateStructure.genericFields?.find(
          (f: any) => f.id === col.fieldId || f.fieldName === col.originalColumn.fieldName
        );
        const fieldDef = systemField || calcField || genericField;

        if (fieldDef) {
          const hasSumInGroupFlag = fieldDef.sumInGroup === true;
          const isInSummaryFields =
            groupSummaryFields.length > 0 &&
            groupSummaryFields.some(
              (sf: any) => sf.fieldId === col.fieldId || sf.fieldId === fieldDef.id
            );
          const isDefaultSumField =
            showGroupSummary &&
            (fieldDef.fieldName === 'valueNet' ||
              fieldDef.fieldName === 'valueGross' ||
              fieldDef.fieldName === 'totalVat');

          const shouldSum = hasSumInGroupFlag || isInSummaryFields || isDefaultSumField;

          if (shouldSum) {
            const summaryValues = (group as any).summaryValues || {};
            let sumValue: number | undefined;

            if (summaryValues[fieldDef.id] !== undefined) {
              sumValue = summaryValues[fieldDef.id];
            } else if (fieldDef.fieldName === 'valueNet' && group.totalNet !== undefined) {
              sumValue = group.totalNet;
            } else if (fieldDef.fieldName === 'valueGross' && group.totalGross !== undefined) {
              sumValue = group.totalGross;
            } else if (fieldDef.fieldName === 'totalVat' && group.totalVat !== undefined) {
              sumValue = group.totalVat;
            } else {
              // Fallback: oblicz sumę z pozycji grupy
              const groupItems = collectGroupItems(group);
              sumValue = 0;
              for (const item of groupItems) {
                const fieldValue = item.fieldValues?.find(
                  (fv: any) => fv.fieldDefinitionId === fieldDef.id
                );
                if (fieldValue?.decimalValue !== undefined && fieldValue?.decimalValue !== null) {
                  sumValue += fieldValue.decimalValue;
                } else if (fieldValue?.stringValue) {
                  const parsed = parseFloat(fieldValue.stringValue);
                  if (!isNaN(parsed)) sumValue += parsed;
                }
              }
            }

            return (
              <Td
                key={col.fieldId}
                p={2}
                textAlign="center"
                bg={level === 0 ? 'primary.100' : 'primary.50'}
                w={`${colWidth}px`}
                minW={`${colWidth}px`}
                maxW={`${colWidth}px`}
              >
                <Text fontSize="sm" fontWeight="bold" color="neutral.700">
                  {sumValue !== undefined
                    ? `Σ ${sumValue.toLocaleString('pl-PL', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ${currencySymbol}`
                    : '—'}
                </Text>
              </Td>
            );
          }
        }

        return (
          <Td key={col.fieldId} p={2} bg={level === 0 ? 'primary.100' : 'primary.50'} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`}>
            <Text fontSize="xs" color="neutral.400" fontStyle="italic" textAlign="center">—</Text>
          </Td>
        );
      })}

      {/* Puste Td dla kolumn pozycji — wyrównanie liczby komórek z nagłówkiem */}
      {Array.from({ length: itemColumnCount }).map((_, idx) => (
        <Td key={`empty-item-${idx}`} p={2} bg={level === 0 ? 'primary.100' : 'primary.50'} />
      ))}
    </Tr>
  );
};
