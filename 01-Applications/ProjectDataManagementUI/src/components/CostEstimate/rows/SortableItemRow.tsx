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
  Box,
} from '@chakra-ui/react';
import {
  GripVertical,
  Trash2,
  ChevronDown,
  ChevronRight,
} from 'lucide-react';
import { useSortable } from '@dnd-kit/sortable';
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
  columns: ExpandedColumn[];
  /** Liczba kolumn etapów — tyle samo pustych Td trzeba wyrenderować przed kolumnami pozycji */
  groupColumnCount: number;
  /** Szerokość kolumny expand (z uwzględnieniem poziomu zagnieżdżenia) */
  expandColWidth: number;
  /** Sticky left offset dla kolumny # (expand) — freeze podczas scrolla */
  expandStickyLeft?: number;
  /** Sticky left offset dla nazwy pozycji (ItemSystemName) — freeze podczas scrolla */
  stickyLeftForName?: number;
  /** Sticky left offset dla nazwy etapu (GroupName) — freeze dla pustego Td w tej kolumnie */
  groupNameStickyLeft?: number;
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
  onRequestDeleteItem?: (groupId: string, itemId: string) => void;
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
  columns: columnsProp,
  groupColumnCount,
  expandColWidth,
  expandStickyLeft,
  stickyLeftForName,
  groupNameStickyLeft,
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
  onRequestDeleteItem,
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

  // Używamy CSS `translate` zamiast `transform` aby nie tworzyć stacking contextu
  // (transform na Tr blokuje poprawne renderowanie tła position:sticky na Td)
  const translateX: number = transform?.x ?? 0;
  const translateY: number = transform?.y ?? 0;
  const transitionStyle: string | undefined = transition?.replace('transform', 'translate');

  const style = {
    translate: `${translateX}px ${translateY}px`,
    transition: transitionStyle,
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
      <Tr
        ref={setNodeRef}
        style={style}
        bg="level1.100"
        borderBottomWidth="0.5px"
        borderBottomColor="neutral.100"
        _hover={{ bg: 'level1.200', cursor: 'pointer' }}
      >
        {/* Akcje pozycji - zamrożona kolumna */}
        {editable && (
          <Td
            px={3}
            py={2}
            textAlign="center"
            position="sticky"
            left={0}
            zIndex={5}
            bg="level1.100"
            borderLeftWidth="2px"
            borderLeftColor="level1.300"
            minW="120px"
            maxW="120px"
            _groupHover={{ bg: 'level1.200' }}
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
              {onDeleteItem && (
                <Tooltip label="Usuń pozycję">
                  <IconButton
                    aria-label="Usuń pozycję"
                    icon={<Trash2 size={14} />}
                    size="xs"
                    colorScheme="red"
                    variant="ghost"
                    onClick={() => onRequestDeleteItem?.(groupId, item.id)}
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
                    icon={<Text fontWeight="bold" fontSize="xs" lineHeight="1">O+</Text>}
                    size="xs"
                    colorScheme="level2"
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
                    icon={<Text fontWeight="bold" fontSize="xs" lineHeight="1">K+</Text>}
                    size="xs"
                    colorScheme="green"
                    variant="ghost"
                    onClick={() => handleAddComponent(groupId, item.id)}
                    isDisabled={hasOptions}
                    opacity={hasOptions ? 0.4 : 1}
                  />
                </Tooltip>
              )}
            </Box>
          </HStack>
          </Td>
        )}

        {/* Expand/collapse button */}
        <Td
          p={2}
          pl={`${indent + 24}px`}
          w={`${expandColWidth}px`}
          minW={`${expandColWidth}px`}
          bg="level1.100"
          position={expandStickyLeft !== undefined ? 'sticky' : undefined}
          left={expandStickyLeft !== undefined ? `${expandStickyLeft}px` : undefined}
          zIndex={expandStickyLeft !== undefined ? 5 : undefined}
          boxShadow={expandStickyLeft !== undefined ? 'inset 0 0 0 9999px var(--chakra-colors-level1-100)' : undefined}
        >
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
        </Td>

        {/* Puste Td dla kolumn etapów — wyrównanie liczby komórek z nagłówkiem */}
        {Array.from({ length: groupColumnCount }).map((_, idx) => {
          // Pierwsze puste Td (GroupName) musi być sticky aby zakrywać treść pozycji
          const isGroupNameCol: boolean = idx === 0 && groupNameStickyLeft !== undefined;
          const groupBg: string = 'level1.100';
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

        {/* Kolumny pól pozycji */}
        {columnsProp.map((col: any) => {
          const colWidth = getColumnWidth(col.fieldId, col.width, col.label);

          // Freeze ItemSystemName podczas scrolla
          const isNameCol: boolean = stickyLeftForName !== undefined &&
            col.originalColumn?.fieldType === 100; // FieldType.ItemSystemName
          const stickyShadowColor: string = 'var(--chakra-colors-level1-100)';
          const stickyProps: Record<string, any> | undefined = isNameCol
            ? { position: 'sticky', left: `${stickyLeftForName}px`, zIndex: 5, bg: 'level1.100', boxShadow: `inset 0 0 0 9999px ${stickyShadowColor}` }
            : undefined;

          const groupHeaderField = templateStructure.groupHeaderFields?.find(
            (f: any) => f.fieldName === col.originalColumn.fieldName
          );
          if (groupHeaderField) {
            return (
              <Td key={col.fieldId} p={2} bg="level1.100" w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`} {...(stickyProps ?? {})}>
                <Text fontSize="xs" color="neutral.300" fontStyle="italic" textAlign="center">—</Text>
              </Td>
            );
          }

          if (col.type === 'childField') {
            return (
              <Td key={col.fieldId} p={2} bg="level1.100" w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`} {...(stickyProps ?? {})}>
                <Text fontSize="xs" color="neutral.400" fontStyle="italic" textAlign="center">
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
                bg={hasComponents && isCalcFieldForDisable ? 'level1.100' : undefined}
                {...(stickyProps ?? {})}
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
            <Td key={col.fieldId} p={2} w={`${colWidth}px`} minW={`${colWidth}px`} maxW={`${colWidth}px`} overflow="hidden" {...(stickyProps ?? {})}>
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
            columns={columnsProp}
            groupColumnCount={groupColumnCount}
            expandColWidth={expandColWidth}
            expandStickyLeft={expandStickyLeft}
            stickyLeftForName={stickyLeftForName}
            groupNameStickyLeft={groupNameStickyLeft}
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
            columns={columnsProp}
            groupColumnCount={groupColumnCount}
            expandColWidth={expandColWidth}
            expandStickyLeft={expandStickyLeft}
            stickyLeftForName={stickyLeftForName}
            groupNameStickyLeft={groupNameStickyLeft}
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
