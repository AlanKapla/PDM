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
import { isTemporaryId } from '../../../types/costEstimate.types.new';
import { PrototypeTextInput, PrototypeNumberInput } from '../PrototypeInputs';
import { GhostActionButton, AddInlineButton } from '../PrototypeActionButtons';
import { OptionRadioButton } from './OptionRadioButton';
import { getCostEstimateItemFieldState, areItemSourceFieldsLocked } from '../../../utils/costEstimateItemFlags';

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
  onFieldChange: DetailModalChildrenTableProps['onFieldChange'];
  onFieldAutosave?: DetailModalChildrenTableProps['onFieldAutosave'];
  onDeleteItem: (itemId: string) => void;
  onSelectOption?: (optionId: string) => void;
}

function fmtValue(val: number | undefined | null): string {
  if (val === undefined || val === null) {
    return '—';
  }
  return val.toFixed(2);
}

function ChildTableRow({
  child,
  variant,
  groupId,
  isEditMode,
  onFieldChange,
  onFieldAutosave,
  onDeleteItem,
  onSelectOption,
}: ChildTableRowProps): React.ReactElement {
  const fieldState = getCostEstimateItemFieldState(child);
  const sourceLocked = areItemSourceFieldsLocked(fieldState);
  const { flags } = fieldState;

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
          value={child.name ?? ''}
          onChange={(e) => {
            const val = e.target.value;
            onFieldChange(groupId, child.id, 'name', val);
            triggerBaseFieldAutosave('name', 'string', val);
          }}
          isDisabled={!isEditMode}
          placeholder="Nazwa"
        />
      </Td>

      <Td px={2} py={2} minW="80px">
        <PrototypeNumberInput
          value={child.quantity !== undefined && child.quantity !== null ? String(child.quantity) : ''}
          onChange={(e) => {
            const val = e.target.value;
            onFieldChange(groupId, child.id, 'quantity', val === '' ? null : parseFloat(val));
            triggerBaseFieldAutosave('quantity', 'numeric', val || undefined);
          }}
          isDisabled={!isEditMode || sourceLocked}
          placeholder="Ilość"
        />
      </Td>

      <Td px={2} py={2} minW="72px">
        <PrototypeTextInput
          value={child.unit ?? ''}
          onChange={(e) => {
            const val = e.target.value;
            onFieldChange(groupId, child.id, 'unit', val);
            triggerBaseFieldAutosave('unit', 'string', val);
          }}
          isDisabled={!isEditMode || sourceLocked}
          placeholder="J.m."
        />
      </Td>

      <Td px={2} py={2} minW="96px">
        <PrototypeNumberInput
          value={
            child.unitPriceNet !== undefined && child.unitPriceNet !== null
              ? String(child.unitPriceNet)
              : ''
          }
          onChange={(e) => {
            const val = e.target.value;
            onFieldChange(groupId, child.id, 'unitPriceNet', val === '' ? null : parseFloat(val));
            triggerBaseFieldAutosave('unitPriceNet', 'numeric', val || undefined);
          }}
          isDisabled={!isEditMode || sourceLocked}
          placeholder="Cena netto"
        />
      </Td>

      <Td px={2} py={2} minW="72px">
        <PrototypeNumberInput
          value={
            child.vatRate !== undefined && child.vatRate !== null
              ? String(Math.round(child.vatRate * 100))
              : ''
          }
          onChange={(e) => {
            const val = e.target.value;
            const raw = parseFloat(val.replace(',', '.'));
            const decimal = isNaN(raw) ? val : String(raw / 100);
            onFieldChange(groupId, child.id, 'vatRate', decimal);
            triggerBaseFieldAutosave('vatRate', 'numeric', decimal);
          }}
          isDisabled={!isEditMode || sourceLocked}
          placeholder="23"
        />
      </Td>

      <Td px={2} py={2} minW="88px" isNumeric>
        {flags.netValueComputed || !isEditMode ? (
          <Text fontSize="sm" sx={{ fontVariantNumeric: 'tabular-nums' }}>
            {fmtValue(child.netValue)}
          </Text>
        ) : (
          <PrototypeNumberInput
            value={child.netValue !== undefined && child.netValue !== null ? String(child.netValue) : ''}
            onChange={(e) => {
              const val = e.target.value;
              onFieldChange(groupId, child.id, 'netValue', val === '' ? null : parseFloat(val));
              triggerBaseFieldAutosave('netValue', 'numeric', val || undefined);
            }}
            placeholder="Netto"
          />
        )}
      </Td>

      <Td px={2} py={2} minW="88px" isNumeric>
        {flags.grossValueComputed || !isEditMode ? (
          <Text fontSize="sm" sx={{ fontVariantNumeric: 'tabular-nums' }}>
            {fmtValue(child.grossValue)}
          </Text>
        ) : (
          <PrototypeNumberInput
            value={
              child.grossValue !== undefined && child.grossValue !== null ? String(child.grossValue) : ''
            }
            onChange={(e) => {
              const val = e.target.value;
              onFieldChange(groupId, child.id, 'grossValue', val === '' ? null : parseFloat(val));
              triggerBaseFieldAutosave('grossValue', 'numeric', val || undefined);
            }}
            placeholder="Brutto"
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
              aria-label="Sumuj"
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
  onFieldChange,
  onFieldAutosave,
  onDeleteItem,
  onAdd,
  onSelectOption,
}: DetailModalChildrenTableProps): React.ReactElement {
  const addLabel = variant === 'options' ? 'Dodaj opcję' : 'Dodaj komponent';

  return (
    <Box
      border="1px solid"
      borderColor="neutral.200"
      borderRadius="12px"
      bg="neutral.25"
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
                  Nazwa
                </Th>
                <Th px={2} isNumeric fontSize="xs" textTransform="uppercase" color="neutral.500">
                  Ilość
                </Th>
                <Th px={2} fontSize="xs" textTransform="uppercase" color="neutral.500">
                  J.m.
                </Th>
                <Th px={2} isNumeric fontSize="xs" textTransform="uppercase" color="neutral.500">
                  Cena netto
                </Th>
                <Th px={2} isNumeric fontSize="xs" textTransform="uppercase" color="neutral.500">
                  VAT %
                </Th>
                <Th px={2} isNumeric fontSize="xs" textTransform="uppercase" color="neutral.500">
                  Netto
                </Th>
                <Th px={2} isNumeric fontSize="xs" textTransform="uppercase" color="neutral.500">
                  Brutto
                </Th>
                {variant === 'components' && (
                  <Th px={2} w="64px" textAlign="center" fontSize="xs" textTransform="uppercase" color="neutral.500">
                    Sumuj
                  </Th>
                )}
                {isEditMode && (
                  <Th px={2} w="48px" fontSize="xs" textTransform="uppercase" color="neutral.500">
                    Akcje
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
