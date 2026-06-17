/**
 * Group Detail Modal — edycja etapu / podetapu w widoku kart
 */

import React, { useCallback } from 'react';
import {
  FormControl,
  FormLabel,
  Text,
  VStack,
  Button,
  HStack,
} from '@chakra-ui/react';
import AppModal from '../../ui/AppModal';
import type {
  CostEstimateGroupWeb,
  CostEstimateAdditionalFieldWeb,
} from '../../../types/costEstimate.types.new';
import { isTemporaryId } from '../../../types/costEstimate.types.new';
import { PrototypeTextInput } from '../PrototypeInputs';
import { getBaseFieldPlaceholder, getFieldLabelByKey } from '../../../utils/costEstimateFieldSchema';
import type { ColumnDef } from '../TreeView/costEstimateColumnTypes';
import {
  getAdditionalFieldValue,
  getAdditionalFieldAutosaveValueType,
  formatAdditionalFieldAutosaveValue,
} from '../../../utils/additionalFieldHelpers';
import { AdditionalFieldInput } from '../AdditionalFieldInput';
import { DetailModalSection } from './DetailModalSection';

interface AutosaveParams {
  entityType: 'group' | 'item';
  entityId: string;
  fieldValueId?: string | null;
  fieldDefinitionId?: string;
  fieldType?: number;
  additionalFieldId?: string;
  fieldName?: string;
  fieldKind?: 'base' | 'additional';
  valueType: 'string' | 'numeric' | 'boolean' | 'date';
  value: string | undefined;
}

interface GroupDetailModalProps {
  group: CostEstimateGroupWeb;
  isOpen: boolean;
  onClose: () => void;
  isEditMode: boolean;
  isSubStage: boolean;
  schemaColumns: ColumnDef[];
  additionalFields?: CostEstimateAdditionalFieldWeb[];
  onFieldChange: (
    groupId: string,
    itemId: string | null,
    fieldId: string,
    value: string | number | boolean | null
  ) => void;
  onFieldAutosave?: (params: AutosaveParams) => void;
}

function fmtNum(val: number | undefined | null): string {
  if (val === undefined || val === null) {
    return '—';
  }
  return val.toFixed(2);
}

export const GroupDetailModal: React.FC<GroupDetailModalProps> = ({
  group,
  isOpen,
  onClose,
  isEditMode,
  isSubStage,
  schemaColumns,
  additionalFields,
  onFieldChange,
  onFieldAutosave,
}) => {
  const label = (fieldKey: string): string => getFieldLabelByKey(schemaColumns, fieldKey);
  const placeholder = (fieldKey: string): string => getBaseFieldPlaceholder(label(fieldKey));

  const triggerGroupNameAutosave = useCallback(
    (value: string | undefined) => {
      if (!onFieldAutosave || isTemporaryId(group.id)) {
        return;
      }
      onFieldAutosave({
        entityType: 'group',
        entityId: group.id,
        fieldKind: 'base',
        fieldName: 'name',
        valueType: 'string',
        value,
      });
    },
    [onFieldAutosave, group.id]
  );

  const triggerAdditionalAutosave = useCallback(
    (
      additionalFieldId: string,
      fieldDef: CostEstimateAdditionalFieldWeb,
      value: string | undefined
    ) => {
      if (!onFieldAutosave || isTemporaryId(group.id)) {
        return;
      }
      const existing = getAdditionalFieldValue(group.additionalFieldValues ?? [], additionalFieldId);
      const fieldValueId = existing?.id && !isTemporaryId(existing.id) ? existing.id : null;
      const valueType = getAdditionalFieldAutosaveValueType(fieldDef.fieldType);
      onFieldAutosave({
        entityType: 'group',
        entityId: group.id,
        fieldValueId,
        additionalFieldId,
        fieldKind: 'additional',
        valueType,
        value,
      });
    },
    [onFieldAutosave, group.additionalFieldValues, group.id]
  );

  return (
    <AppModal
      isOpen={isOpen}
      onClose={onClose}
      title={isSubStage ? 'Szczegóły podetapu' : 'Szczegóły etapu'}
      desktopSize="xl"
      hideFooter={true}
    >
      <VStack spacing={5} align="stretch">
        <DetailModalSection title="Informacje podstawowe">
          <FormControl>
            <FormLabel fontSize="sm" fontWeight="medium">
              {label('name')}
            </FormLabel>
            <PrototypeTextInput
              showBorder
              value={group.name ?? ''}
              onChange={(e) => {
                const val = e.target.value;
                onFieldChange(group.id, null, 'name', val);
                triggerGroupNameAutosave(val);
              }}
              isDisabled={!isEditMode}
              isStage={!isSubStage}
              isGroup={isSubStage}
              placeholder={placeholder('name')}
            />
          </FormControl>
        </DetailModalSection>

        <DetailModalSection title="Wartości finansowe">
          <FormControl>
            <FormLabel fontSize="sm" fontWeight="medium">
              {label('netValue')}
            </FormLabel>
            <Text
              fontSize="sm"
              fontWeight="bold"
              textAlign="right"
              sx={{ fontVariantNumeric: 'tabular-nums' }}
            >
              {fmtNum(group.totalNet)} zł
            </Text>
          </FormControl>

          <FormControl>
            <FormLabel fontSize="sm" fontWeight="medium">
              {label('vatValue')}
            </FormLabel>
            <Text
              fontSize="sm"
              fontWeight="semibold"
              color="neutral.600"
              textAlign="right"
              sx={{ fontVariantNumeric: 'tabular-nums' }}
            >
              {fmtNum(group.totalVat)} zł
            </Text>
          </FormControl>

          <FormControl>
            <FormLabel fontSize="sm" fontWeight="medium">
              {label('grossValue')}
            </FormLabel>
            <Text
              fontSize="sm"
              fontWeight="semibold"
              color="neutral.600"
              textAlign="right"
              sx={{ fontVariantNumeric: 'tabular-nums' }}
            >
              {fmtNum(group.totalGross)} zł
            </Text>
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
                  showBorder
                  field={field}
                  fieldValues={group.additionalFieldValues ?? []}
                  isDisabled={!isEditMode}
                  onChange={(value) => {
                    onFieldChange(group.id, null, field.id, value);
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
