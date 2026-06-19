import React, { useCallback } from 'react';
import {
  Box,
  Checkbox,
  Flex,
  Table,
  Tbody,
  Td,
  Text,
  Th,
  Thead,
  Tr,
} from '@chakra-ui/react';
import { Trash2 } from 'lucide-react';
import type { CostEstimateItemWeb } from '../../../types/costEstimate.types.new';
import { CostEstimateFieldType, isTemporaryId } from '../../../types/costEstimate.types.new';
import { PrototypeTextInput, PrototypeNumberInput } from '../PrototypeInputs';
import { getBaseFieldPlaceholder, getFieldLabelByKey, getInputTextAlign } from '../../../utils/costEstimateFieldSchema';
import { formatDecimalInput, formatVatPercent, parseNumericInput } from '../../../utils/numericInputUtils';
import type { ColumnDef } from '../TreeView/costEstimateColumnTypes';
import { GhostActionButton, AddInlineButton } from '../PrototypeActionButtons';
import { OptionRadioButton } from './OptionRadioButton';
import { getCostEstimateItemFieldState, areItemSourceFieldsLocked } from '../../../utils/costEstimateItemFlags';
import { deriveItemFinancialState } from '../../../utils/costEstimateItemFinancial';

interface AutosaveParams {
  entityType: 'group' | 'item';
  entityId: string;
  fieldValueId?: string | null;
  additionalFieldId?: string;
  fieldName?: string;
  fieldKind?: 'base' | 'additional';
  valueType: 'string' | 'numeric' | 'boolean' | 'date';
  value: string | undefined;
}

interface DetailModalChildrenTableProps {
  title: string;
  variant: 'options' | 'components';
  items: CostEstimateItemWeb[];
  groupId: string;
  isEditMode: boolean;
  schemaColumns: ColumnDef[];
  onFieldChange: (
    groupId: string,
    itemId: string | null,
    fieldId: string,
    value: string | number | boolean | null
  ) => void;
  onFieldAutosave?: (params: AutosaveParams) => void;
  onDeleteItem: (itemId: string) => void;
  onAdd: () => void;
  onSelectOption?: (optionId: string) => void;
}

interface ChildTableRowProps {
  child: CostEstimateItemWeb;
  variant: 'options' | 'components';
  groupId: string;
  isEditMode: boolean;
  schemaColumns: ColumnDef[];
  onFieldChange: DetailModalChildrenTableProps['onFieldChange'];
  onFieldAutosave?: DetailModalChildrenTableProps['onFieldAutosave'];
  onDeleteItem: (itemId: string) => void;
  onSelectOption?: (optionId: string) => void;
}

function fmtValue(val: number | undefined | null): string {
  if (val === undefined || val === null) {
    return '—';
  }
  return formatDecimalInput(val);
}

function ChildTableRow({
  child,
  variant,
  groupId,
  isEditMode,
  schemaColumns,
  onFieldChange,
  onFieldAutosave,
  onDeleteItem,
  onSelectOption,
}: ChildTableRowProps): React.ReactElement {
  const fieldState = getCostEstimateItemFieldState(child);
  const sourceLocked = areItemSourceFieldsLocked(fieldState);
  const { flags } = fieldState;
  const derived = deriveItemFinancialState(child);

  const label = (fieldKey: string): string => getFieldLabelByKey(schemaColumns, fieldKey);
  const placeholder = (fieldKey: string): string => getBaseFieldPlaceholder(label(fieldKey));

  const triggerBaseFieldAutosave = useCallback(
    (fieldName: string, valueType: AutosaveParams['valueType'], value: string | undefined) => {
      if (!onFieldAutosave || isTemporaryId(child.id)) {
        return;
      }
      onFieldAutosave({
        entityType: 'item',
        entityId: child.id,
        fieldKind: 'base',
        fieldName,
        valueType,
        value,
      });
    },
    [onFieldAutosave, child.id]
  );

  return (
    <Tr _hover={{ bg: 'neutral.50' }}>
      {variant === 'options' && (
        <Td px={2} py={2} w="44px">
          <Flex justify="center">
            <OptionRadioButton
              isSelected={child.isSelected}
              isDisabled={!isEditMode}
              onSelect={() => onSelectOption?.(child.id)}
              size="sm"
            />
          </Flex>
        </Td>
      )}

      <Td px={2} py={2} minW="140px">
        <PrototypeTextInput
          showBorder
          value={child.name ?? ''}
          onChange={(e) => {
            const val = e.target.value;
            onFieldChange(groupId, child.id, 'name', val);
            triggerBaseFieldAutosave('name', 'string', val);
          }}
          isDisabled={!isEditMode}
          placeholder={placeholder('name')}
        />
      </Td>

      <Td px={2} py={2} minW="80px" isNumeric>
        <PrototypeNumberInput
          showBorder
          value={child.quantity ?? ''}
          onChange={(e) => {
            const val = e.target.value;
            onFieldChange(groupId, child.id, 'quantity', val === '' ? null : val);
            triggerBaseFieldAutosave('quantity', 'numeric', val || undefined);
          }}
          isDisabled={!isEditMode || sourceLocked}
          placeholder={placeholder('quantity')}
        />
      </Td>

      <Td px={2} py={2} minW="72px" isNumeric>
        <PrototypeTextInput
          showBorder
          textAlign={getInputTextAlign('unit', CostEstimateFieldType.Unit)}
          value={child.unit ?? ''}
          onChange={(e) => {
            const val = e.target.value;
            onFieldChange(groupId, child.id, 'unit', val);
            triggerBaseFieldAutosave('unit', 'string', val);
          }}
          isDisabled={!isEditMode || sourceLocked}
          placeholder={placeholder('unit')}
        />
      </Td>

      <Td px={2} py={2} minW="96px" isNumeric>
        <PrototypeNumberInput
          showBorder
          value={child.unitPriceNet ?? ''}
          onChange={(e) => {
            const val = e.target.value;
            onFieldChange(groupId, child.id, 'unitPriceNet', val === '' ? null : val);
            triggerBaseFieldAutosave('unitPriceNet', 'numeric', val || undefined);
          }}
          isDisabled={!isEditMode || sourceLocked}
          placeholder={placeholder('unitPriceNet')}
        />
      </Td>

      <Td px={2} py={2} minW="72px" isNumeric>
        <PrototypeNumberInput
          showBorder
          value={
            child.vatRate !== undefined && child.vatRate !== null
              ? formatVatPercent(child.vatRate)
              : ''
          }
          onChange={(e) => {
            const val = e.target.value;
            onFieldChange(groupId, child.id, 'vatRate', val === '' ? null : val);
            triggerBaseFieldAutosave('vatRate', 'numeric', val || undefined);
          }}
          isDisabled={!isEditMode || sourceLocked}
          placeholder={placeholder('vatRate')}
        />
      </Td>

      <Td px={2} py={2} minW="88px" isNumeric>
        {flags.netValueComputed || !isEditMode ? (
          <Text fontSize="sm" textAlign="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
            {fmtValue(derived.netValue)}
          </Text>
        ) : (
          <PrototypeNumberInput
          showBorder
            value={child.netValue ?? ''}
            onChange={(e) => {
              const val = e.target.value;
              onFieldChange(groupId, child.id, 'netValue', val === '' ? null : parseNumericInput(val));
              triggerBaseFieldAutosave('netValue', 'numeric', val || undefined);
            }}
            placeholder={placeholder('netValue')}
          />
        )}
      </Td>

      <Td px={2} py={2} minW="88px" isNumeric>
        <Text fontSize="sm" textAlign="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
          {fmtValue(derived.vatValue)}
        </Text>
      </Td>

      <Td px={2} py={2} minW="88px" isNumeric>
        {flags.grossValueComputed || !isEditMode ? (
          <Text fontSize="sm" textAlign="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
            {fmtValue(derived.grossValue)}
          </Text>
        ) : (
          <PrototypeNumberInput
          showBorder
            value={child.grossValue ?? ''}
            onChange={(e) => {
              const val = e.target.value;
              onFieldChange(groupId, child.id, 'grossValue', val === '' ? null : parseNumericInput(val));
              triggerBaseFieldAutosave('grossValue', 'numeric', val || undefined);
            }}
            placeholder={placeholder('grossValue')}
          />
        )}
      </Td>

      {variant === 'components' && (
        <Td px={2} py={2} w="64px">
          <Flex justify="center">
            <Checkbox
              isChecked={child.isSelected}
              onChange={(e) => {
                const checked = e.target.checked;
                onFieldChange(groupId, child.id, 'isSelected', checked);
                triggerBaseFieldAutosave('isSelected', 'boolean', checked ? 'true' : 'false');
              }}
              isDisabled={!isEditMode}
              colorScheme="primary"
              size="sm"
              aria-label={label('isSelected')}
            />
          </Flex>
        </Td>
      )}

      {isEditMode && (
        <Td px={2} py={2} w="48px">
          <GhostActionButton
            label="Usuń"
            icon={<Trash2 size={14} />}
            variant="delete"
            onClick={() => onDeleteItem(child.id)}
          />
        </Td>
      )}
    </Tr>
  );
}

export function DetailModalChildrenTable({
  title,
  variant,
  items,
  groupId,
  isEditMode,
  schemaColumns,
  onFieldChange,
  onFieldAutosave,
  onDeleteItem,
  onAdd,
  onSelectOption,
}: DetailModalChildrenTableProps): React.ReactElement {
  const addLabel = variant === 'options' ? 'Dodaj opcję' : 'Dodaj komponent';
  const label = (fieldKey: string): string => getFieldLabelByKey(schemaColumns, fieldKey);

  return (
    <Box
      border="1px solid"
      borderColor="neutral.200"
      borderRadius="12px"
      bg="white"
      px={4}
      py={4}
    >
      <Text
        fontSize="md"
        fontWeight="semibold"
        color="neutral.700"
        mb={3}
        pb={2}
        borderBottom="1px solid"
        borderColor="neutral.200"
      >
        {title}
      </Text>

      {items.length === 0 ? (
        <Text fontSize="sm" color="neutral.500" py={2}>
          Brak elementów.
        </Text>
      ) : (
        <Box overflowX="auto" mx={-1}>
          <Table size="sm" variant="simple" sx={{ 'th, td': { verticalAlign: 'middle' } }}>
            <Thead>
              <Tr>
                {variant === 'options' && (
                  <Th px={2} w="44px" fontSize="xs" textTransform="uppercase" color="neutral.500">
                    Wybór
                  </Th>
                )}
                <Th px={2} minW="140px" fontSize="xs" textTransform="uppercase" color="neutral.500">
                  {label('name')}
                </Th>
                <Th px={2} isNumeric fontSize="xs" textTransform="uppercase" color="neutral.500">
                  {label('quantity')}
                </Th>
                <Th px={2} isNumeric fontSize="xs" textTransform="uppercase" color="neutral.500">
                  {label('unit')}
                </Th>
                <Th px={2} isNumeric fontSize="xs" textTransform="uppercase" color="neutral.500">
                  {label('unitPriceNet')}
                </Th>
                <Th px={2} isNumeric fontSize="xs" textTransform="uppercase" color="neutral.500">
                  {label('vatRate')}
                </Th>
                <Th px={2} isNumeric fontSize="xs" textTransform="uppercase" color="neutral.500">
                  {label('netValue')}
                </Th>
                <Th px={2} isNumeric fontSize="xs" textTransform="uppercase" color="neutral.500">
                  {label('vatValue')}
                </Th>
                <Th px={2} isNumeric fontSize="xs" textTransform="uppercase" color="neutral.500">
                  {label('grossValue')}
                </Th>
                {variant === 'components' && (
                  <Th px={2} w="64px" textAlign="center" fontSize="xs" textTransform="uppercase" color="neutral.500">
                    {label('isSelected')}
                  </Th>
                )}
                {isEditMode && (
                  <Th px={2} w="48px" fontSize="xs" textTransform="uppercase" color="neutral.500">
                    {label('actions')}
                  </Th>
                )}
              </Tr>
            </Thead>
            <Tbody>
              {items.map((child) => (
                <ChildTableRow
                  key={child.id}
                  child={child}
                  variant={variant}
                  groupId={groupId}
                  isEditMode={isEditMode}
                  schemaColumns={schemaColumns}
                  onFieldChange={onFieldChange}
                  onFieldAutosave={onFieldAutosave}
                  onDeleteItem={onDeleteItem}
                  onSelectOption={onSelectOption}
                />
              ))}
            </Tbody>
          </Table>
        </Box>
      )}

      {isEditMode && (
        <Flex mt={3}>
          <AddInlineButton onClick={onAdd}>{addLabel}</AddInlineButton>
        </Flex>
      )}
    </Box>
  );
}
