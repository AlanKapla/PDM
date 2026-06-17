import React from 'react';
import { Checkbox, Flex } from '@chakra-ui/react';
import type { CostEstimateAdditionalFieldWeb, CostEstimateAdditionalFieldValueWeb } from '../../types/costEstimate.types.new';
import {
  getAdditionalFieldInputKind,
  getAdditionalFieldValue,
  getAdditionalFieldValueAsString,
  getAdditionalFieldDateInputValue,
  sanitizeDecimalInput,
} from '../../utils/additionalFieldHelpers';
import { PrototypeTextInput, PrototypeNumberInput, PrototypeDateInput } from './PrototypeInputs';
import { getInputTextAlign } from '../../utils/costEstimateFieldSchema';

interface AdditionalFieldInputProps {
  field: Pick<CostEstimateAdditionalFieldWeb, 'id' | 'name' | 'fieldType'>;
  fieldValues: CostEstimateAdditionalFieldValueWeb[];
  isDisabled?: boolean;
  onChange: (value: string | number | boolean | null) => void;
  w?: string;
  /** W TreeView — input bez białego tła przy hover/focus */
  blendWithRow?: boolean;
  /** W modalach formularza — widoczna ramka od razu */
  showBorder?: boolean;
  valueAlign?: 'left' | 'right';
}

export const AdditionalFieldInput: React.FC<AdditionalFieldInputProps> = ({
  field,
  fieldValues,
  isDisabled = false,
  onChange,
  w = 'full',
  blendWithRow = false,
  showBorder = false,
  valueAlign,
}) => {
  const inputKind = getAdditionalFieldInputKind(field.fieldType);
  const resolvedValueAlign = valueAlign ?? getInputTextAlign(field.id, field.fieldType);

  if (inputKind === 'number') {
    const currentValue = getAdditionalFieldValueAsString(fieldValues, field.id) ?? '';
    return (
      <PrototypeNumberInput
        value={currentValue}
        onChange={(e) => {
          const val = sanitizeDecimalInput(e.target.value);
          onChange(val === '' ? null : val);
        }}
        onPointerDown={(e) => e.stopPropagation()}
        onClick={(e) => e.stopPropagation()}
        isDisabled={isDisabled}
        placeholder={field.name}
        w={w}
        blendWithRow={blendWithRow}
        showBorder={showBorder}
      />
    );
  }

  if (inputKind === 'boolean') {
    const checked = getAdditionalFieldValue(fieldValues, field.id)?.boolValue ?? false;
    return (
      <Flex justify="center" align="center" w={w}>
        <Checkbox
          isChecked={checked}
          onChange={(e) => onChange(e.target.checked)}
          onPointerDown={(e) => e.stopPropagation()}
          onClick={(e) => e.stopPropagation()}
          isDisabled={isDisabled}
          colorScheme="primary"
          size="sm"
          aria-label={field.name}
        />
      </Flex>
    );
  }

  if (inputKind === 'date') {
    const currentValue = getAdditionalFieldDateInputValue(fieldValues, field.id);
    return (
      <PrototypeDateInput
        value={currentValue}
        onChange={(e) => onChange(e.target.value || null)}
        onPointerDown={(e) => e.stopPropagation()}
        onClick={(e) => e.stopPropagation()}
        isDisabled={isDisabled}
        w={w}
        blendWithRow={blendWithRow}
        showBorder={showBorder}
        textAlign={resolvedValueAlign}
      />
    );
  }

  const currentValue = getAdditionalFieldValueAsString(fieldValues, field.id) ?? '';
  return (
    <PrototypeTextInput
      value={currentValue}
      onChange={(e) => onChange(e.target.value)}
      onPointerDown={(e) => e.stopPropagation()}
      onClick={(e) => e.stopPropagation()}
      isDisabled={isDisabled}
      placeholder={field.name}
      w={w}
      blendWithRow={blendWithRow}
      showBorder={showBorder}
      textAlign={resolvedValueAlign}
    />
  );
};
