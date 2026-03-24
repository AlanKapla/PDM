import React, { useState } from 'react';
import { Tr, Td, Text, IconButton, Tooltip, Badge, HStack, Checkbox } from '@chakra-ui/react';
import { GripVertical, Trash2, GitBranch, ChevronDown, ChevronRight } from 'lucide-react';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import type { CostEstimateItemWeb, CostEstimateFieldValueWeb } from '../../../types/costEstimate.types.new';
import { getAllValues } from '../../../utils/costEstimateCalculations';
import { getFieldSource } from '../../../utils/resolveFieldDefinition';
import type {
  ExpandedColumn,
  FieldSource,
  RenderFieldInputFn,
  FormatDisplayValueFn,
  GetColumnWidthFn,
} from '../costEstimateTableTypes';
import { POSITION_COL_MIN_WIDTH } from '../costEstimateTableTypes';
import { SortableOptionRow } from './SortableOptionRow';

// ---------------------------------------------------------------------------
// Props
// ---------------------------------------------------------------------------

export interface SortableComponentRowProps {
  id: string;
  component: CostEstimateItemWeb;
  compIndex: number;
  parentItem: CostEstimateItemWeb;
  groupId: string;
  indent: number;
  editable: boolean;
  /** Czy user może edytować wartości pól (false tylko dla trybu podglądu). Niezależne od canStructuralEdit. */
  canEditFields: boolean;
  templateStructure: any;
  expandedColumns: ExpandedColumn[];
  getColumnWidth: GetColumnWidthFn;
  getItemFieldValue: (item: CostEstimateItemWeb, fieldId: string) => string | undefined;
  /** Zwraca pełne CostEstimateFieldValueWeb — potrzebne dla pól z plikami */
  getItemFieldValueFull: (item: CostEstimateItemWeb, fieldId: string) => CostEstimateFieldValueWeb | undefined;
  updateComponentFieldValue: (
    groupId: string,
    itemId: string,
    componentId: string,
    fieldId: string,
    fieldSource: FieldSource,
    value: string | undefined
  ) => void;
  removeComponentFromItem: (groupId: string, itemId: string, componentId: string) => void;
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
  onAddOption?: (groupId: string, itemId: string) => void;
}

// ---------------------------------------------------------------------------
// Komponent
// ---------------------------------------------------------------------------

export const SortableComponentRow: React.FC<SortableComponentRowProps> = ({
  id,
  component,
  compIndex,
  parentItem,
  groupId,
  indent,
  editable,
  canEditFields,
  templateStructure,
  expandedColumns,
  getColumnWidth,
  getItemFieldValue,
  getItemFieldValueFull,
  updateComponentFieldValue,
  removeComponentFromItem,
  updateOptionFieldValue,
  removeOptionFromItem,
  renderFieldInput,
  formatDisplayValue,
  onAddOption,
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

  const componentAllValues = getAllValues(component, templateStructure);
  const componentOptions = component.options || [];
  const hasOptions = componentOptions.length > 0;

  // Stan zwijania opcji komponentu — domyślnie rozwinięte
  const [optionsExpanded, setOptionsExpanded] = useState(true);

  /** Dodaj opcję i automatycznie rozwiń sekcję */
  const handleAddOption = (gId: string, iId: string) => {
    setOptionsExpanded(true);
    onAddOption?.(gId, iId);
  };

  return (
    <React.Fragment>
      <Tr ref={setNodeRef} style={style} bg="green.50" _hover={{ bg: 'green.100' }}>
        {/* Akcje komponentu */}
        {editable && (
          <Td
            px={3}
            py={2}
            textAlign="center"
            position="sticky"
            left={0}
            zIndex={5}
            bg="green.50"
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
              <Tooltip label="Usuń komponent">
                <IconButton
                  aria-label="Usuń komponent"
                  icon={<Trash2 size={14} />}
                  size="xs"
                  colorScheme="red"
                  variant="ghost"
                  onClick={() => removeComponentFromItem(groupId, parentItem.id, component.id)}
                />
              </Tooltip>
              {onAddOption && (
                <Tooltip label="Dodaj opcję/wariant">
                  <IconButton
                    aria-label="Dodaj opcję"
                    icon={<GitBranch size={14} />}
                    size="xs"
                    colorScheme="purple"
                    variant="ghost"
                    onClick={() => handleAddOption(groupId, component.id)}
                  />
                </Tooltip>
              )}
            </HStack>
          </Td>
        )}

        {/* Etykieta komponentu */}
        <Td
          p={2}
          pl={`${indent + 48}px`}
          position="sticky"
          left={editable ? '120px' : 0}
          zIndex={5}
          bg="green.50"
          w={`${POSITION_COL_MIN_WIDTH}px`}
          minW={`${POSITION_COL_MIN_WIDTH}px`}
          whiteSpace="nowrap"
        >
          <HStack spacing={1}>
            {hasOptions && (
              <Tooltip label={optionsExpanded ? 'Zwiń opcje' : 'Rozwiń opcje'}>
                <IconButton
                  aria-label="Zwiń/rozwiń opcje"
                  icon={optionsExpanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
                  size="xs"
                  variant="ghost"
                  onClick={() => setOptionsExpanded((prev) => !prev)}
                  minW="auto"
                  h="auto"
                  p={0}
                />
              </Tooltip>
            )}
            <Badge colorScheme="green" size="sm">
              Komponent {compIndex + 1}
            </Badge>
          </HStack>
        </Td>

        {/* Kolumny pól komponentu */}
        {expandedColumns.map((col: any) => {
          const colWidth = getColumnWidth(col.fieldId, col.width, col.label);

          // Pola nagłówka grupy — puste
          const groupHeaderField = templateStructure.groupHeaderFields?.find(
            (f: any) => f.fieldName === col.originalColumn.fieldName
          );
          if (groupHeaderField) {
            return (
              <Td key={col.fieldId} p={2} bg="green.50" w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`}>
                <Text fontSize="xs" color="gray.300" textAlign="center">—</Text>
              </Td>
            );
          }

          // Pola opcji (childField) — pokaż liczbę opcji komponentu
          if (col.type === 'childField') {
            return (
              <Td key={col.fieldId} p={2} bg="purple.50" w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`} overflow="hidden">
                <Text fontSize="xs" color="purple.400" fontStyle="italic" textAlign="center">
                  {componentOptions.length > 0 ? `${componentOptions.length} opcji` : '—'}
                </Text>
              </Td>
            );
          }

          // Rozpoznaj definicję pola i źródło
          let fieldDef: any = col.fieldDef;
          let fieldSource: FieldSource = 'generic';

          if (!fieldDef) {
            fieldDef = templateStructure.systemFields?.find(
              (f: any) => f.fieldName === col.originalColumn.fieldName
            );
            if (fieldDef) {
              fieldSource = 'system';
            } else {
              fieldDef = templateStructure.calculatedFields?.find(
                (f: any) => f.fieldName === col.originalColumn.fieldName
              );
              if (fieldDef) {
                fieldSource = 'calculated';
              } else {
                fieldDef = templateStructure.genericFields?.find(
                  (f: any) => f.fieldName === col.originalColumn.fieldName
                );
                if (fieldDef) {
                  fieldSource = 'generic';
                }
              }
            }
          } else {
            fieldSource = getFieldSource(fieldDef.id, templateStructure);
          }

          if (fieldDef) {
            const value = getItemFieldValue(component, fieldDef.id);
            const fieldValueFull = getItemFieldValueFull(component, fieldDef.id);
            return (
              <Td key={col.fieldId} p={2} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`} overflow="hidden">
                {canEditFields ? (
                  renderFieldInput(
                    fieldDef,
                    value,
                    (newValue) =>
                      updateComponentFieldValue(
                        groupId,
                        parentItem.id,
                        component.id,
                        fieldDef.id,
                        fieldSource,
                        newValue
                      ),
                    false,
                    componentAllValues,
                    component.id,
                    fieldDef.id,
                    fieldValueFull?.files
                  )
                ) : col.isBoolean ? (
                  <Checkbox
                    isChecked={value === 'true' || value === '1'}
                    isReadOnly
                    size="sm"
                    sx={{ cursor: 'default' }}
                  />
                ) : (
                  <Text fontSize="sm" textAlign="center" isTruncated>
                    {formatDisplayValue(value, fieldDef)}
                  </Text>
                )}
              </Td>
            );
          }

          return (
            <Td key={col.fieldId} p={2} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`} overflow="hidden">
              -
            </Td>
          );
        })}
      </Tr>

      {/* Wiersze opcji komponentu — ukryte gdy zwinięte */}
      {optionsExpanded &&
        componentOptions.map((option: any, optIndex: number) => (
        <SortableOptionRow
          key={`comp-option-${component.id}-${option.id}`}
          id={`comp-option::${groupId}::${component.id}::${option.id}`}
          option={option}
          optIndex={optIndex}
          item={component}
          groupId={groupId}
          indent={indent + 24}
          editable={editable}
          canEditFields={canEditFields}
          templateStructure={templateStructure}
          expandedColumns={expandedColumns}
          getColumnWidth={getColumnWidth}
          getItemFieldValueFull={getItemFieldValueFull}
          updateOptionFieldValue={updateOptionFieldValue}
          removeOptionFromItem={removeOptionFromItem}
          renderFieldInput={renderFieldInput}
          formatDisplayValue={formatDisplayValue}
        />
      ))}
    </React.Fragment>
  );
};
