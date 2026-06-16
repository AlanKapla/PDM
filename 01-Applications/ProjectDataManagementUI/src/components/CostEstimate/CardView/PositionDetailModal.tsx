/**
 * Position Detail Modal - Modal for showing full item details
 * Sections: Informacje podstawowe, Wartości finansowe, Pola dodatkowe
 */

import React, { useCallback } from 'react';
import {
  VStack,
  HStack,
  Text,
  Button,
  FormControl,
  FormLabel,
  Checkbox,
  IconButton,
} from '@chakra-ui/react';
import { Upload, FileText } from 'lucide-react';
import AppModal from '../../ui/AppModal';
import type {
  CostEstimateItemWeb,
  CostEstimateAdditionalFieldWeb,
} from '../../../types/costEstimate.types.new';
import { isTemporaryId } from '../../../types/costEstimate.types.new';
import { PrototypeTextInput, PrototypeNumberInput } from '../PrototypeInputs';
import { useCostEstimateItemFieldState } from '../../../hooks/useCostEstimateItemFieldState';
import {
  areItemAdditionalFieldsLocked,
  areItemSourceFieldsLocked,
  isItemNameLocked,
} from '../../../utils/costEstimateItemFlags';
import {
  getAdditionalFieldValue,
  getAdditionalFieldAutosaveValueType,
  formatAdditionalFieldAutosaveValue,
} from '../../../utils/additionalFieldHelpers';
import { AdditionalFieldInput } from '../AdditionalFieldInput';
import { DetailModalSection } from './DetailModalSection';
import { DetailModalChildrenTable } from './DetailModalChildrenTable';

interface PositionDetailModalProps {
  item: CostEstimateItemWeb;
  groupId: string;
  isOpen: boolean;
  onClose: () => void;
  isEditMode: boolean;
  additionalFields?: CostEstimateAdditionalFieldWeb[];
  onFieldChange: (
    groupId: string,
    itemId: string | null,
    fieldId: string,
    value: string | number | boolean | null
  ) => void;
  onFieldAutosave?: (params: AutosaveParams) => void;
  onAddComponent: (groupId: string, itemId: string) => void;
  onAddOption: (groupId: string, itemId: string) => void;
  onDeleteItem: (itemId: string) => void;
  onSelectOption: (groupId: string, itemId: string, optionId: string) => void;
  onUploadFiles: (itemId: string) => void;
}

interface AutosaveParams {
  entityType: 'group' | 'item';
  entityId: string;
  fieldValueId?: string | null;
  /** @deprecated Używaj additionalFieldId */
  fieldDefinitionId?: string;
  /** @deprecated Nie używany w nowym API */
  fieldType?: number;
  additionalFieldId?: string;
  fieldName?: string;
  fieldKind?: 'base' | 'additional';
  valueType: 'string' | 'numeric' | 'boolean' | 'date';
  value: string | undefined;
}

function getModalTitle(isComponent: boolean, isOption: boolean): string {
  if (isComponent) {
    return 'Szczegóły komponentu';
  }
  if (isOption) {
    return 'Szczegóły opcji';
  }
  return 'Szczegóły pozycji';
}

export const PositionDetailModal: React.FC<PositionDetailModalProps> = ({
  item,
  groupId,
  isOpen,
  onClose,
  isEditMode,
  additionalFields,
  onFieldChange,
  onFieldAutosave,
  onAddComponent,
  onAddOption,
  onDeleteItem,
  onSelectOption,
  onUploadFiles,
}) => {
  const fieldState = useCostEstimateItemFieldState(item);
  const { isComponent, isOption, flags, hasComponents, hasOptions } = fieldState;
  const sourceFieldsLocked = areItemSourceFieldsLocked(fieldState);
  const additionalFieldsLocked = areItemAdditionalFieldsLocked(fieldState);
  const nameLocked = isItemNameLocked(fieldState);
  const hasFiles = (item.files?.length ?? 0) > 0;

  const positionName = item.name || 'Bez nazwy';
  const quantity = item.quantity;
  const unit = item.unit ?? '';
  const isSelected = item.isSelected;
  const isStageWork = item.isStageWork;
  const unitPriceNet = item.unitPriceNet;
  const vatRate = item.vatRate;
  const unitPriceGross = item.unitPriceGross;
  const canHaveOptions = isComponent || (!isComponent && !isOption && !hasComponents);
  const canHaveComponents = !isOption && !isComponent && !hasOptions;
  const showOptionsTable = hasOptions || (isEditMode && canHaveOptions);
  const showComponentsTable = !isOption && (hasComponents || (isEditMode && canHaveComponents));
  const modalSize = showOptionsTable || showComponentsTable ? '2xl' : 'xl';

  const triggerBaseFieldAutosave = useCallback(
    (fieldName: string, valueType: AutosaveParams['valueType'], value: string | undefined) => {
      if (!onFieldAutosave || isTemporaryId(item.id)) {
        return;
      }
      onFieldAutosave({
        entityType: 'item',
        entityId: item.id,
        fieldKind: 'base',
        fieldName,
        valueType,
        value,
      });
    },
    [onFieldAutosave, item.id]
  );

  const triggerAdditionalAutosave = useCallback(
    (
      additionalFieldId: string,
      fieldDef: CostEstimateAdditionalFieldWeb,
      value: string | undefined
    ) => {
      if (!onFieldAutosave || isTemporaryId(item.id)) {
        return;
      }
      const existing = getAdditionalFieldValue(item.additionalFieldValues ?? [], additionalFieldId);
      const fieldValueId = existing?.id && !isTemporaryId(existing.id) ? existing.id : null;
      const valueType = getAdditionalFieldAutosaveValueType(fieldDef.fieldType);
      onFieldAutosave({
        entityType: 'item',
        entityId: item.id,
        fieldValueId,
        additionalFieldId,
        fieldKind: 'additional',
        valueType,
        value,
      });
    },
    [onFieldAutosave, item.additionalFieldValues, item.id]
  );

  return (
    <AppModal
      isOpen={isOpen}
      onClose={onClose}
      title={getModalTitle(isComponent, isOption)}
      desktopSize={modalSize}
      hideFooter={true}
    >
      <VStack spacing={5} align="stretch">
        <DetailModalSection title="Informacje podstawowe">
          <FormControl>
            <FormLabel fontSize="sm" fontWeight="medium">
              Nazwa
            </FormLabel>
            <PrototypeTextInput
              value={positionName}
              onChange={(e) => {
                const val = e.target.value;
                onFieldChange(groupId, item.id, 'name', val);
                triggerBaseFieldAutosave('name', 'string', val);
              }}
              isDisabled={!isEditMode || nameLocked}
              placeholder={
                isComponent
                  ? 'Nazwa komponentu...'
                  : isOption
                  ? 'Nazwa opcji...'
                  : 'Nazwa pozycji...'
              }
            />
          </FormControl>

          <FormControl>
            <FormLabel fontSize="sm" fontWeight="medium">
              Ilość
            </FormLabel>
            <PrototypeNumberInput
              value={quantity !== undefined && quantity !== null ? String(quantity) : ''}
              onChange={(e) => {
                const val = e.target.value;
                onFieldChange(groupId, item.id, 'quantity', val === '' ? null : parseFloat(val));
                triggerBaseFieldAutosave('quantity', 'numeric', val || undefined);
              }}
              isDisabled={!isEditMode || sourceFieldsLocked}
              placeholder="Ilość"
            />
          </FormControl>

          <FormControl>
            <FormLabel fontSize="sm" fontWeight="medium">
              Jednostka
            </FormLabel>
            <PrototypeTextInput
              value={unit}
              onChange={(e) => {
                const val = e.target.value;
                onFieldChange(groupId, item.id, 'unit', val);
                triggerBaseFieldAutosave('unit', 'string', val);
              }}
              isDisabled={!isEditMode || sourceFieldsLocked}
              placeholder="Jednostka"
            />
          </FormControl>

          {!isComponent && !isOption && (
            <FormControl>
              <HStack justify="space-between" align="center">
                <FormLabel fontSize="sm" fontWeight="medium" mb={0}>
                  Zakres pracy harmonogramu
                </FormLabel>
                <Checkbox
                  isChecked={isStageWork}
                  onChange={(e) => {
                    const checked = e.target.checked;
                    onFieldChange(groupId, item.id, 'isStageWork', checked);
                    triggerBaseFieldAutosave('isStageWork', 'boolean', checked ? 'true' : 'false');
                  }}
                  isDisabled={!isEditMode}
                  colorScheme="orange"
                  size="sm"
                />
              </HStack>
            </FormControl>
          )}

          {!isOption && (
            <FormControl>
              <HStack justify="space-between" align="center">
                <FormLabel fontSize="sm" fontWeight="medium" mb={0}>
                  Sumuj
                </FormLabel>
                <Checkbox
                  isChecked={isSelected}
                  onChange={(e) => {
                    const checked = e.target.checked;
                    onFieldChange(groupId, item.id, 'isSelected', checked);
                    triggerBaseFieldAutosave('isSelected', 'boolean', checked ? 'true' : 'false');
                  }}
                  isDisabled={!isEditMode}
                  colorScheme="primary"
                  size="sm"
                />
              </HStack>
            </FormControl>
          )}

          <FormControl>
            <FormLabel fontSize="sm" fontWeight="medium">
              Załączone pliki
            </FormLabel>
            <HStack spacing={2}>
              <IconButton
                aria-label="Zarządzaj plikami"
                icon={hasFiles ? <FileText size={16} /> : <Upload size={16} />}
                colorScheme={hasFiles ? 'primary' : 'gray'}
                variant="outline"
                onClick={() => onUploadFiles(item.id)}
                isDisabled={!isEditMode}
              />
              {hasFiles && (
                <Text fontSize="sm" color="neutral.600">
                  {item.files?.length} plik(ów)
                </Text>
              )}
            </HStack>
          </FormControl>
        </DetailModalSection>

        <DetailModalSection title="Wartości finansowe">
          <FormControl>
            <FormLabel fontSize="sm" fontWeight="medium">
              Cena jednostkowa netto
            </FormLabel>
            <PrototypeNumberInput
              value={unitPriceNet !== undefined && unitPriceNet !== null ? String(unitPriceNet) : ''}
              onChange={(e) => {
                const val = e.target.value;
                onFieldChange(groupId, item.id, 'unitPriceNet', val === '' ? null : parseFloat(val));
                triggerBaseFieldAutosave('unitPriceNet', 'numeric', val || undefined);
              }}
              isDisabled={!isEditMode || sourceFieldsLocked}
              placeholder="Cena netto"
            />
          </FormControl>

          <FormControl>
            <FormLabel fontSize="sm" fontWeight="medium">
              Stawka VAT (%)
            </FormLabel>
            <PrototypeNumberInput
              value={vatRate !== undefined && vatRate !== null ? String(Math.round(vatRate * 100)) : ''}
              onChange={(e) => {
                const val = e.target.value;
                const raw = parseFloat(val.replace(',', '.'));
                const decimal = isNaN(raw) ? val : String(raw / 100);
                onFieldChange(groupId, item.id, 'vatRate', decimal);
                triggerBaseFieldAutosave('vatRate', 'numeric', decimal);
              }}
              isDisabled={!isEditMode || sourceFieldsLocked}
              placeholder="23"
            />
          </FormControl>

          <FormControl>
            <FormLabel fontSize="sm" fontWeight="medium">
              Cena jednostkowa brutto
            </FormLabel>
            <PrototypeNumberInput
              value={unitPriceGross !== undefined && unitPriceGross !== null ? String(unitPriceGross) : ''}
              onChange={(e) => {
                const val = e.target.value;
                onFieldChange(groupId, item.id, 'unitPriceGross', val === '' ? null : parseFloat(val));
                triggerBaseFieldAutosave('unitPriceGross', 'numeric', val || undefined);
              }}
              isDisabled={!isEditMode || flags.unitPriceGrossComputed}
              placeholder="Cena brutto"
            />
          </FormControl>

          <FormControl>
            <FormLabel fontSize="sm" fontWeight="medium">
              Wartość netto
            </FormLabel>
            <PrototypeNumberInput
              value={item.netValue !== undefined && item.netValue !== null ? String(item.netValue) : ''}
              onChange={(e) => {
                const val = e.target.value;
                onFieldChange(groupId, item.id, 'netValue', val === '' ? null : parseFloat(val));
                triggerBaseFieldAutosave('netValue', 'numeric', val || undefined);
              }}
              isDisabled={!isEditMode || flags.netValueComputed}
              placeholder="Wartość netto"
            />
          </FormControl>

          <FormControl>
            <FormLabel fontSize="sm" fontWeight="medium">
              Wartość VAT
            </FormLabel>
            <PrototypeNumberInput
              value={item.vatValue !== undefined && item.vatValue !== null ? String(item.vatValue) : ''}
              onChange={(e) => {
                const val = e.target.value;
                onFieldChange(groupId, item.id, 'vatValue', val === '' ? null : parseFloat(val));
                triggerBaseFieldAutosave('vatValue', 'numeric', val || undefined);
              }}
              isDisabled={!isEditMode || flags.vatValueComputed}
              placeholder="Wartość VAT"
            />
          </FormControl>

          <FormControl>
            <FormLabel fontSize="sm" fontWeight="medium">
              Wartość brutto
            </FormLabel>
            <PrototypeNumberInput
              value={item.grossValue !== undefined && item.grossValue !== null ? String(item.grossValue) : ''}
              onChange={(e) => {
                const val = e.target.value;
                onFieldChange(groupId, item.id, 'grossValue', val === '' ? null : parseFloat(val));
                triggerBaseFieldAutosave('grossValue', 'numeric', val || undefined);
              }}
              isDisabled={!isEditMode || flags.grossValueComputed}
              placeholder="Wartość brutto"
            />
          </FormControl>
        </DetailModalSection>

        {additionalFields && additionalFields.length > 0 && (
          <DetailModalSection title="Pola dodatkowe">
            {additionalFields.map((field) => (
              <FormControl key={field.id}>
                <FormLabel fontSize="sm" fontWeight="medium">
                  {field.name}
                </FormLabel>
                <AdditionalFieldInput
                  field={field}
                  fieldValues={item.additionalFieldValues ?? []}
                  isDisabled={!isEditMode || additionalFieldsLocked}
                  onChange={(value) => {
                    onFieldChange(groupId, item.id, field.id, value);
                    triggerAdditionalAutosave(
                      field.id,
                      field,
                      formatAdditionalFieldAutosaveValue(value)
                    );
                  }}
                />
              </FormControl>
            ))}
          </DetailModalSection>
        )}

        {showOptionsTable && (
          <DetailModalChildrenTable
            title="Opcje"
            variant="options"
            items={item.options ?? []}
            groupId={groupId}
            isEditMode={isEditMode}
            onFieldChange={onFieldChange}
            onFieldAutosave={onFieldAutosave}
            onDeleteItem={onDeleteItem}
            onAdd={() => onAddOption(groupId, item.id)}
            onSelectOption={(optionId) => onSelectOption(groupId, item.id, optionId)}
          />
        )}

        {showComponentsTable && (
          <DetailModalChildrenTable
            title="Komponenty"
            variant="components"
            items={item.components ?? []}
            groupId={groupId}
            isEditMode={isEditMode}
            onFieldChange={onFieldChange}
            onFieldAutosave={onFieldAutosave}
            onDeleteItem={onDeleteItem}
            onAdd={() => onAddComponent(groupId, item.id)}
          />
        )}

        {isEditMode && (
          <HStack justify="flex-end" spacing={3} pt={2}>
            <Button variant="outline" onClick={onClose}>
              Zamknij
            </Button>
          </HStack>
        )}
      </VStack>
    </AppModal>
  );
};
