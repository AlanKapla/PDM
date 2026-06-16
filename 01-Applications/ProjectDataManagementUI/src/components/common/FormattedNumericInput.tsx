/**
 * Stub komponentu FormattedNumericInput.
 * @deprecated Używany przez deprecated CostEstimateTableView.
 */

import React from 'react';
import { Input } from '@chakra-ui/react';

interface FormattedNumericInputProps {
  value?: number | string | null;
  onChange?: (value: number | string | undefined) => void;
  onBlur?: () => void;
  placeholder?: string;
  isDisabled?: boolean;
  isReadOnly?: boolean;
  min?: number;
  max?: number;
  decimalPlaces?: number;
  [key: string]: unknown;
}

export const FormattedNumericInput: React.FC<FormattedNumericInputProps> = ({
  value,
  onChange,
  onBlur,
  placeholder,
  isDisabled,
  isReadOnly,
  ...rest
}) => {
  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const parsed = parseFloat(e.target.value.replace(',', '.'));
    onChange?.(isNaN(parsed) ? undefined : parsed);
  };

  return (
    <Input
      type="number"
      value={value !== null && value !== undefined ? String(value) : ''}
      onChange={handleChange}
      onBlur={onBlur}
      placeholder={placeholder}
      isDisabled={isDisabled}
      isReadOnly={isReadOnly}
      {...rest}
    />
  );
};

export default FormattedNumericInput;
