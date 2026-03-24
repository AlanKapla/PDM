import React, { useState } from 'react';
import {
  Tr,
  Td,
  Text,
  IconButton,
  Tooltip,
  Badge,
  HStack,
  Checkbox,
} from '@chakra-ui/react';
import {
  GripVertical,
  Trash2,
  GitBranch,
  Layers,
  ChevronDown,
  ChevronRight,
} from 'lucide-react';
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
import { SortableComponentRow } from './SortableComponentRow';

// ---------------------------------------------------------------------------
// Props
// ---------------------------------------------------------------------------

export interface SortableItemRowProps {
  id: string;
  item: CostEstimateItemWeb;
  groupId: string;
  level: number;
  indent: number;
  itemNumber: number;
  editable: boolean;
  /** Czy user może edytować wartości pól (false tylko dla trybu podglądu). Niezależne od canStructuralEdit. */
  canEditFields: boolean;
  templateStructure: any;
  expandedColumns: ExpandedColumn[];
  getColumnWidth: GetColumnWidthFn;
  getItemFieldValue: (item: CostEstimateItemWeb, fieldId: string) => string | undefined;
  /** Zwraca pełne CostEstimateFieldValueWeb — potrzebne dla pól z plikami */
  getItemFieldValueFull: (item: CostEstimateItemWeb, fieldId: string) => CostEstimateFieldValueWeb | undefined;
  updateItemFieldValue: (
    groupId: string,
    itemId: string,
    fieldId: string,
    fieldSource: FieldSource,
    value: string | undefined
  ) => void;
  updateOptionFieldValue: (
    groupId: string,
    itemId: string,
    optionId: string,
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
  removeOptionFromItem: (groupId: string, itemId: string, optionId: string) => void;
  removeComponentFromItem: (groupId: string, itemId: string, componentId: string) => void;
  renderFieldInput: RenderFieldInputFn;
  formatDisplayValue: FormatDisplayValueFn;
  onDeleteItem?: (groupId: string, itemId: string) => void;
  onAddOption?: (groupId: string, itemId: string) => void;
  onAddComponent?: (groupId: string, itemId: string) => void;
}

// ---------------------------------------------------------------------------
// Komponent
// ---------------------------------------------------------------------------

export const SortableItemRow: React.FC<SortableItemRowProps> = ({
  id,
  item,
  groupId,
  level,
  indent,
  itemNumber,
  editable,
  canEditFields,
  templateStructure,
  expandedColumns,
  getColumnWidth,
  getItemFieldValue,
  getItemFieldValueFull,
  updateItemFieldValue,
  updateOptionFieldValue,
  updateComponentFieldValue,
  removeOptionFromItem,
  removeComponentFromItem,
  renderFieldInput,
  formatDisplayValue,
  onDeleteItem,
  onAddOption,
  onAddComponent,
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

  const itemOptions = item.options || [];
  const itemComponents = item.components || [];
  const hasComponents = itemComponents.length > 0;
  const hasOptions = itemOptions.length > 0;
  const hasChildren = hasComponents || hasOptions;

  // Stan zwijania — domyślnie rozwinięte
  const [componentsExpanded, setComponentsExpanded] = useState(true);
  const [optionsExpanded, setOptionsExpanded] = useState(true);

  /** Dodaj opcję i automatycznie rozwiń sekcję opcji */
  const handleAddOption = (gId: string, iId: string) => {
    setOptionsExpanded(true);
    onAddOption?.(gId, iId);
  };

  /** Dodaj komponent i automatycznie rozwiń sekcję komponentów */
  const handleAddComponent = (gId: string, iId: string) => {
    setComponentsExpanded(true);
    onAddComponent?.(gId, iId);
  };

  return (
    <React.Fragment>
      {/* Główny wiersz pozycji */}
      <Tr ref={setNodeRef} style={style} bg="gray.50" _hover={{ bg: 'gray.100' }}>
        {/* Akcje pozycji - zamrożona kolumna */}
        {editable && (
          <Td
            px={3}
            py={2}
            textAlign="center"
            position="sticky"
            left={0}
            zIndex={5}
            bg="gray.50"
            minW="120px"
            maxW="120px"
            _groupHover={{ bg: 'gray.100' }}
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
              {onDeleteItem && (
                <Tooltip label="Usuń pozycję">
                  <IconButton
                    aria-label="Usuń pozycję"
                    icon={<Trash2 size={14} />}
                    size="xs"
                    colorScheme="red"
                    variant="ghost"
                    onClick={() => onDeleteItem(groupId, item.id)}
                  />
                </Tooltip>
              )}
              {onAddOption && (
                <Tooltip 
                  label={hasComponents 
                    ? "Nie można dodać opcji do pozycji z komponentami" 
                    : "Dodaj opcję/wariant"
                  }
                >
                  <IconButton
                    aria-label="Dodaj opcję"
                    icon={<GitBranch size={14} />}
                    size="xs"
                    colorScheme="purple"
                    variant="ghost"
                    onClick={() => handleAddOption(groupId, item.id)}
                    isDisabled={hasComponents}
                    opacity={hasComponents ? 0.4 : 1}
                  />
                </Tooltip>
              )}
              {onAddComponent && (
                <Tooltip 
                  label={hasOptions 
                    ? "Nie można dodać komponentu do pozycji z opcjami" 
                    : "Dodaj komponent (składnik pozycji)"
                  }
                >
                  <IconButton
                    aria-label="Dodaj komponent"
                    icon={<Layers size={14} />}
                    size="xs"
                    colorScheme="green"
                    variant="ghost"
                    onClick={() => handleAddComponent(groupId, item.id)}
                    isDisabled={hasOptions}
                    opacity={hasOptions ? 0.4 : 1}
                  />
                </Tooltip>
              )}
            </HStack>
          </Td>
        )}

        {/* Pozycja - zamrożona kolumna */}
        <Td
          p={3}
          pl={`${indent + 24}px`}
          position="sticky"
          left={editable ? '120px' : 0}
          zIndex={5}
          bg="gray.50"
          w={`${POSITION_COL_MIN_WIDTH}px`}
          minW={`${POSITION_COL_MIN_WIDTH}px`}
          whiteSpace="nowrap"
          _groupHover={{ bg: 'gray.100' }}
        >
          <HStack spacing={1}>
            {hasChildren && (
              <Tooltip label={
                componentsExpanded && optionsExpanded
                  ? 'Zwiń opcje i komponenty'
                  : 'Rozwiń opcje i komponenty'
              }>
                <IconButton
                  aria-label="Zwiń/rozwiń"
                  icon={
                    componentsExpanded && optionsExpanded
                      ? <ChevronDown size={14} />
                      : <ChevronRight size={14} />
                  }
                  size="xs"
                  variant="ghost"
                  onClick={() => {
                    const allExpanded = componentsExpanded && optionsExpanded;
                    setComponentsExpanded(!allExpanded);
                    setOptionsExpanded(!allExpanded);
                  }}
                  minW="auto"
                  h="auto"
                  p={0}
                />
              </Tooltip>
            )}
            <Text fontSize="sm" color="gray.600" fontWeight="medium">
              POZYCJA {itemNumber}
            </Text>
          </HStack>
        </Td>

        {/* Kolumny pól pozycji */}
        {expandedColumns.map((col: any) => {
          const colWidth = getColumnWidth(col.fieldId, col.width, col.label);

          const groupHeaderField = templateStructure.groupHeaderFields?.find(
            (f: any) => f.fieldName === col.originalColumn.fieldName
          );
          if (groupHeaderField) {
            return (
              <Td key={col.fieldId} p={2} bg="gray.50" w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`}>
                <Text fontSize="xs" color="gray.400" fontStyle="italic" textAlign="center">—</Text>
              </Td>
            );
          }

          if (col.type === 'childField') {
            return (
              <Td key={col.fieldId} p={2} bg="purple.50" w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`}>
                <Text fontSize="xs" color="purple.400" fontStyle="italic" textAlign="center">
                  {itemOptions.length > 0 ? `${itemOptions.length} opcji` : '—'}
                </Text>
              </Td>
            );
          }

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
            const value = getItemFieldValue(item, fieldDef.id);
            const fieldValueFull = getItemFieldValueFull(item, fieldDef.id);
            const itemAllValues = getAllValues(item, templateStructure);
            // Gdy pozycja ma komponenty — blokuj TYLKO pola kalkulowane (sumy z komponentów)
            const isCalcFieldForDisable = fieldSource === 'calculated';
            const disabledByComponents = hasComponents && isCalcFieldForDisable;

            return (
              <Td
                key={col.fieldId}
                p={2}
                w={`${colWidth}px`}
                minW={`${colWidth}px`}
                maxW={`${colWidth}px`}
                overflow="hidden"
                bg={hasComponents && isCalcFieldForDisable ? 'green.50' : undefined}
              >
                {canEditFields ? (
                  renderFieldInput(
                    fieldDef,
                    value,
                    (newValue) => updateItemFieldValue(groupId, item.id, fieldDef.id, fieldSource, newValue),
                    disabledByComponents,
                    itemAllValues,
                    item.id,
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

      {/* Wiersze komponentów — ukryte gdy zwinięte */}
      {componentsExpanded &&
        itemComponents.map((comp: CostEstimateItemWeb, compIndex: number) => (
          <SortableComponentRow
            key={`comp-${item.id}-${comp.id}`}
            id={`comp::${groupId}::${item.id}::${comp.id}`}
            component={comp}
            compIndex={compIndex}
            parentItem={item}
            groupId={groupId}
            indent={indent}
            editable={editable}
            canEditFields={canEditFields}
            templateStructure={templateStructure}
            expandedColumns={expandedColumns}
            getColumnWidth={getColumnWidth}
            getItemFieldValue={getItemFieldValue}
            getItemFieldValueFull={getItemFieldValueFull}
            updateComponentFieldValue={updateComponentFieldValue}
            removeComponentFromItem={removeComponentFromItem}
            updateOptionFieldValue={updateOptionFieldValue}
            removeOptionFromItem={removeOptionFromItem}
            onAddOption={onAddOption}
            renderFieldInput={renderFieldInput}
            formatDisplayValue={formatDisplayValue}
          />
        ))}

      {/* Wiersze opcji — ukryte gdy zwinięte */}
      {optionsExpanded &&
        itemOptions.map((option: any, optIndex: number) => (
          <SortableOptionRow
            key={`option-${item.id}-${option.id}`}
            id={`option::${groupId}::${item.id}::${option.id}`}
            option={option}
            optIndex={optIndex}
            item={item}
            groupId={groupId}
            indent={indent}
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
