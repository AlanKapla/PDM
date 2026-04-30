import React from 'react';
import { Tr, Td, Text, IconButton, Tooltip, Badge, HStack, Checkbox, Box } from '@chakra-ui/react';
import { GripVertical, Trash2 } from 'lucide-react';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { getFieldValueAsString } from '../../../types/costEstimate.types.new';
import type { CostEstimateItemWeb, CostEstimateFieldValueWeb } from '../../../types/costEstimate.types.new';
import { getAllOptionValues } from '../../../utils/costEstimateCalculations';
import type {
  ExpandedColumn,
  FieldSource,
  RenderFieldInputFn,
  FormatDisplayValueFn,
  GetColumnWidthFn,
} from '../costEstimateTableTypes';
import { POSITION_COL_MIN_WIDTH } from '../costEstimateTableTypes';

// ---------------------------------------------------------------------------
// Props
// ---------------------------------------------------------------------------

export interface SortableOptionRowProps {
  id: string;
  option: any;
  optIndex: number;
  item: CostEstimateItemWeb;
  groupId: string;
  indent: number;
  editable: boolean;
  /** Czy user może edytować wartości pól (false tylko dla trybu podglądu). Niezależne od canStructuralEdit. */
  canEditFields: boolean;
  templateStructure: any;
  expandedColumns: ExpandedColumn[];
  getColumnWidth: GetColumnWidthFn;
  /** Zwraca pełne CostEstimateFieldValueWeb — potrzebne dla pól z plikami */
  getItemFieldValueFull: (item: CostEstimateItemWeb, fieldId: string) => CostEstimateFieldValueWeb | undefined;
  updateOptionFieldValue: (
    groupId: string,
    itemId: string,
    optionId: string,
    fieldId: string,
    fieldSource: FieldSource,
    value: string | undefined
  ) => void;
  removeOptionFromItem: (groupId: string, itemId: string, optionId: string) => void;
  renderFieldInput: RenderFieldInputFn;
  formatDisplayValue: FormatDisplayValueFn;
}

// ---------------------------------------------------------------------------
// Komponent
// ---------------------------------------------------------------------------

export const SortableOptionRow: React.FC<SortableOptionRowProps> = ({
  id,
  option,
  optIndex,
  item,
  groupId,
  indent,
  editable,
  canEditFields,
  templateStructure,
  expandedColumns,
  getColumnWidth,
  getItemFieldValueFull,
  updateOptionFieldValue,
  removeOptionFromItem,
  renderFieldInput,
  formatDisplayValue,
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
      bg="neutral.25"
      borderBottomWidth="0.5px"
      borderBottomColor="neutral.100"
      _hover={{ bg: 'neutral.50', cursor: 'pointer' }}
    >
      {editable && (
        <Td
          px={3}
          py={2}
          textAlign="center"
          position="sticky"
          left={0}
          zIndex={5}
          bg="neutral.25"
          borderLeftWidth="2px"
          borderLeftColor="neutral.200"
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
            <Box display="inline-flex" alignItems="center">
            <Tooltip label="Usuń opcję">
              <IconButton
                aria-label="Usuń opcję"
                icon={<Trash2 size={14} />}
                size="xs"
                colorScheme="red"
                variant="ghost"
                onClick={() => removeOptionFromItem(groupId, item.id, option.id)}
              />
            </Tooltip>
            </Box>
          </HStack>
        </Td>
      )}

      <Td
        p={2}
        pl={`${indent + 48}px`}
        position="sticky"
        left={editable ? '120px' : 0}
        zIndex={5}
        bg="neutral.25"
        borderLeftWidth={!editable ? '2px' : undefined}
        borderLeftColor={!editable ? 'neutral.200' : undefined}
        w={`${POSITION_COL_MIN_WIDTH}px`}
        minW={`${POSITION_COL_MIN_WIDTH}px`}
        whiteSpace="nowrap"
      >
        <Badge
          bg="neutral.100"
          color="neutral.400"
          px={2}
          py={0.5}
          borderRadius="md"
          fontSize="xs"
          fontWeight="medium"
        >
          OPCJA {optIndex + 1}
        </Badge>
      </Td>

      {expandedColumns.map((col: any) => {
        const colWidth = getColumnWidth(col.fieldId, col.width, col.label);

        // Dla opcji renderujemy tylko kolumny childField, reszta to puste komórki
        if (col.type !== 'childField' || !col.childField) {
          return (
            <Td key={col.fieldId} p={2} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`} overflow="hidden">
              <Text fontSize="xs" color="neutral.400" textAlign="center">—</Text>
            </Td>
          );
        }

        const optionFieldValue = option.fieldValues?.find(
          (fv: any) => fv.fieldDefinitionId === col.childField.id
        );
        const childValue = getFieldValueAsString(optionFieldValue) ?? '';
        const fieldValueFull = getItemFieldValueFull(option, col.childField.id);

        let fieldSource: FieldSource = 'system';
        if (templateStructure.calculatedFields?.find((f: any) => f.id === col.childField.id)) {
          fieldSource = 'calculated';
        } else if (templateStructure.genericFields?.find((f: any) => f.id === col.childField.id)) {
          fieldSource = 'generic';
        }

        // Oblicz wartości opcji do sprawdzenia readonly pól kalkulowanych
        // Ilość (quantity) pochodzi z pozycji nadrzędnej (item)
        const optionAllValues = getAllOptionValues(option.fieldValues || [], templateStructure, item);

        return (
          <Td key={col.fieldId} p={2} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`} overflow="hidden">
            {canEditFields ? (
              renderFieldInput(
                col.childField,
                childValue || undefined,
                (newValue) =>
                  updateOptionFieldValue(
                    groupId,
                    item.id,
                    option.id,
                    col.childField.id,
                    fieldSource,
                    newValue
                  ),
                false,
                optionAllValues,
                option.id,
                col.childField.id,
                fieldValueFull?.files
              )
            ) : col.isBoolean ? (
              <Checkbox
                isChecked={childValue === 'true' || childValue === '1'}
                isReadOnly
                size="sm"
                sx={{ cursor: 'default' }}
              />
            ) : (
              <Text fontSize="sm" textAlign="center" isTruncated>
                {formatDisplayValue(childValue, col.childField)}
              </Text>
            )}
          </Td>
        );
      })}
    </Tr>
  );
};
