import React from 'react';
import {
  IconButton,
  Tooltip,
  Tr,
  Td,
} from '@chakra-ui/react';
import {
  Trash2,
  GripVertical,
} from 'lucide-react';
import { FieldValueInput, getFieldInputType } from './FieldValueInput';
import type {
  CostEstimateItemDto as CostEstimateWorkScopeItemDto,
  CostEstimateFieldValueDto as CostEstimateWorkScopeItemFieldValueDto,
} from '../../types/costEstimate.types.new';

/**
 * Unified field definition
 */
export interface WorkScopeFieldDefinition {
  id: string;
  label: string;
  fieldType: string;
  valueType: 'system' | 'calculated' | 'generic';
  isRequired: boolean;
  isReadOnly?: boolean;
  allowedValues?: string[];
  min?: number;
  max?: number;
  unit?: string;
  order: number;
  helpText?: string;
  fieldTypeConfig?: import('../../types/costEstimate.types.new').CostEstimateFieldTypeConfigWeb;
}

/**
 * Props for WorkScopeItemRow component
 */
export interface WorkScopeItemRowProps {
  /** Work scope item data */
  item: CostEstimateWorkScopeItemDto;
  /** Field definitions */
  fieldDefinitions: WorkScopeFieldDefinition[];
  /** Change handler */
  onChange: (updatedItem: CostEstimateWorkScopeItemDto) => void;
  /** Delete handler */
  onDelete: () => void;
  /** Whether item is readonly */
  readonly?: boolean;
  /** Item index for display */
  index?: number;
  /** Show drag handle */
  showDragHandle?: boolean;
}

/**
 * WorkScopeItemRow - Table row for work scope item with inline field editing
 */
export const WorkScopeItemRow: React.FC<WorkScopeItemRowProps> = ({
  item,
  fieldDefinitions,
  onChange,
  onDelete,
  readonly = false,
  index,
  showDragHandle = false,
}) => {
  // Sort fields by order
  const sortedFields = [...fieldDefinitions].sort((a, b) => a.order - b.order);

  // Get field value by field definition id
  const getFieldValue = (fieldDef: WorkScopeFieldDefinition): string | undefined => {
    const fieldValue = item.fieldValues.find((fv) => fv.fieldDefinitionId === fieldDef.id);
    return fieldValue?.value;
  };

  // Update field value
  const updateFieldValue = (fieldDef: WorkScopeFieldDefinition, value: string | undefined) => {
    const existingIndex = item.fieldValues.findIndex((fv) => fv.fieldDefinitionId === fieldDef.id);

    const newFieldValues = [...item.fieldValues];

    if (existingIndex >= 0) {
      if (value === undefined || value === '') {
        newFieldValues.splice(existingIndex, 1);
      } else {
        newFieldValues[existingIndex] = {
          ...newFieldValues[existingIndex],
          value,
        };
      }
    } else if (value !== undefined && value !== '') {
      const newFieldValue: CostEstimateWorkScopeItemFieldValueDto = {
        fieldDefinitionId: fieldDef.id,
        value,
      };
      newFieldValues.push(newFieldValue);
    }

    onChange({
      ...item,
      fieldValues: newFieldValues,
    });
  };

  return (
    <Tr _hover={{ bg: 'gray.50' }}>
      {/* Drag handle */}
      {showDragHandle && (
        <Td p={1} width="40px">
          <IconButton size="sm" cursor="grab" aria-label="Przeciągnij pozycję" variant="ghost">
            <GripVertical size={16} />
          </IconButton>
        </Td>
      )}

      {/* Index */}
      {index !== undefined && (
        <Td p={2} width="60px" fontWeight="medium" textAlign="center">
          {index + 1}
        </Td>
      )}

      {/* Field values */}
      {sortedFields.map((fieldDef) => {
        const value = getFieldValue(fieldDef);
        const inputType = getFieldInputType(fieldDef.fieldType, fieldDef.allowedValues);

        return (
          <Td key={fieldDef.id} p={1}>
            <FieldValueInput
              label=""
              value={value}
              type={inputType}
              onChange={(newValue) => updateFieldValue(fieldDef, newValue)}
              required={fieldDef.isRequired}
              disabled={readonly || fieldDef.isReadOnly}
              allowedValues={fieldDef.allowedValues}
              min={fieldDef.min}
              max={fieldDef.max}
              unit={fieldDef.unit}
              helpText={fieldDef.helpText}
              size="small"
            />
          </Td>
        );
      })}

      {/* Actions */}
      {!readonly && (
        <Td p={1} width="60px">
          <Tooltip label="Usuń pozycję">
            <IconButton
              size="sm"
              colorScheme="red"
              onClick={onDelete}
              aria-label="Usuń pozycję"
              icon={<Trash2 size={16} />}
              variant="ghost"
            />
          </Tooltip>
        </Td>
      )}
    </Tr>
  );
};
