import React from 'react';
import { Tr, Td, Text, IconButton, Tooltip, Badge, HStack, Checkbox, Box } from '@chakra-ui/react';
import { GripVertical, Trash2 } from 'lucide-react';
import { useSortable } from '@dnd-kit/sortable';
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
  columns: ExpandedColumn[];
  groupColumnCount: number;
  expandColWidth: number;
  /** Sticky left offset dla kolumny # (expand) — freeze podczas scrolla */
  expandStickyLeft?: number;
  /** Sticky left offset dla nazwy pozycji (ItemSystemName) — freeze podczas scrolla */
  stickyLeftForName?: number;
  /** Sticky left offset dla nazwy etapu (GroupName) — freeze dla pustego Td w tej kolumnie */
  groupNameStickyLeft?: number;
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
  columns: columnsProp,
  groupColumnCount,
  expandColWidth,
  expandStickyLeft,
  stickyLeftForName,
  groupNameStickyLeft,
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

  // Używamy CSS `translate` zamiast `transform` aby nie tworzyć stacking contextu
  const translateX: number = transform?.x ?? 0;
  const translateY: number = transform?.y ?? 0;
  const transitionStyle: string | undefined = transition?.replace('transform', 'translate');

  const style = {
    translate: `${translateX}px ${translateY}px`,
    transition: transitionStyle,
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <Tr
      ref={setNodeRef}
      style={style}
      bg="level2.50"
      borderBottomWidth="0.5px"
      borderBottomColor="neutral.100"
      _hover={{ bg: 'level2.100', cursor: 'pointer' }}
    >
      {editable && (
        <Td
          px={3}
          py={2}
          textAlign="center"
          position="sticky"
          left={0}
          zIndex={5}
          bg="level2.50"
          borderLeftWidth="2px"
          borderLeftColor="level2.300"
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

      {/* Pozycja column removed — minimal expand cell */}
      <Td
        p={2}
        pl={`${indent + 48}px`}
        w={`${expandColWidth}px`}
        minW={`${expandColWidth}px`}
        bg="level2.50"
        position={expandStickyLeft !== undefined ? 'sticky' : undefined}
        left={expandStickyLeft !== undefined ? `${expandStickyLeft}px` : undefined}
        zIndex={expandStickyLeft !== undefined ? 5 : undefined}
        boxShadow={expandStickyLeft !== undefined ? 'inset 0 0 0 9999px var(--chakra-colors-level2-50)' : undefined}
      />

      {/* Puste Td dla kolumn etapów — wyrównanie liczby komórek z nagłówkiem */}
      {Array.from({ length: groupColumnCount }).map((_, idx) => {
        const isGroupNameCol: boolean = idx === 0 && groupNameStickyLeft !== undefined;
        const groupBg: string = 'level2.50';
        return (
          <Td
            key={`empty-group-${idx}`}
            p={2}
            bg={groupBg}
            position={isGroupNameCol ? 'sticky' : undefined}
            left={isGroupNameCol ? `${groupNameStickyLeft}px` : undefined}
            zIndex={isGroupNameCol ? 5 : undefined}
            boxShadow={isGroupNameCol ? `inset 0 0 0 9999px var(--chakra-colors-${groupBg.replace('.', '-')})` : undefined}
          />
        );
      })}

      {columnsProp.map((col: any) => {
        const colWidth = getColumnWidth(col.fieldId, col.width, col.label);

        // Freeze ItemSystemName podczas scrolla
        const isNameCol: boolean = stickyLeftForName !== undefined &&
          col.originalColumn?.fieldType === 100; // FieldType.ItemSystemName
        const stickyShadowColor: string = 'var(--chakra-colors-level2-50)';
        const stickyProps: Record<string, any> | undefined = isNameCol
          ? { position: 'sticky', left: `${stickyLeftForName}px`, zIndex: 5, bg: 'level2.50', boxShadow: `inset 0 0 0 9999px ${stickyShadowColor}` }
          : undefined;

        // Dla opcji renderujemy tylko kolumny childField, reszta to puste komórki
        if (col.type !== 'childField' || !col.childField) {
          return (
            <Td key={col.fieldId} p={2} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`} overflow="hidden" {...(stickyProps ?? {})}>
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
          <Td key={col.fieldId} p={2} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`} overflow="hidden" {...(stickyProps ?? {})}>
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
