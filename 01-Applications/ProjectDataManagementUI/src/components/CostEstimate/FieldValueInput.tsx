import React from 'react';
import { Input, Select, Checkbox, FormControl, FormLabel, FormHelperText, InputGroup, InputRightAddon } from '@chakra-ui/react';

// Note: For date/datetime pickers, you'll need to install react-datepicker or similar
// For now, using basic HTML5 input types

/**
 * Field type for input component
 */
export type FieldInputType = 
  | 'text'
  | 'number'
  | 'decimal'
  | 'integer'
  | 'boolean'
  | 'date'
  | 'datetime'
  | 'select';

/**
 * Props for FieldValueInput component
 */
export interface FieldValueInputProps {
  /** Field label */
  label: string;
  /** Current value */
  value: string | undefined;
  /** Input type */
  type: FieldInputType;
  /** Change handler */
  onChange: (value: string | undefined) => void;
  /** Whether field is required */
  required?: boolean;
  /** Whether field is disabled/readonly */
  disabled?: boolean;
  /** Placeholder text */
  placeholder?: string;
  /** Allowed values for select type */
  allowedValues?: string[];
  /** Min value for number types */
  min?: number;
  /** Max value for number types */
  max?: number;
  /** Min length for text */
  minLength?: number;
  /** Max length for text */
  maxLength?: number;
  /** Help text */
  helpText?: string;
  /** Unit label (e.g., "PLN", "m²", "szt.") */
  unit?: string;
  /** Error message */
  error?: string;
  /** Variant */
  variant?: 'outlined' | 'filled' | 'standard';
  /** Size */
  size?: 'small' | 'medium';
  /** Full width */
  fullWidth?: boolean;
}

/**
 * FieldValueInput - Universal input component for different field types
 * Supports: text, number, decimal, integer, boolean, date, datetime, select
 */
export const FieldValueInput: React.FC<FieldValueInputProps> = ({
  label,
  value,
  type,
  onChange,
  required = false,
  disabled = false,
  placeholder,
  allowedValues,
  min,
  max,
  minLength,
  maxLength,
  helpText,
  unit,
  error,
  variant = 'outlined',
  size = 'small',
  fullWidth = true,
}) => {
  // Boolean type
  if (type === 'boolean') {
    const checked = value === 'true' || value === '1';
    
    return (
      <FormControl>
        <Checkbox
          isChecked={checked}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => onChange(e.target.checked ? 'true' : 'false')}
          isDisabled={disabled}
          size={size}
        >
          {label}{required ? ' *' : ''}
        </Checkbox>
        {(error || helpText) && <FormHelperText color={error ? 'red.500' : undefined}>{error || helpText}</FormHelperText>}
      </FormControl>
    );
  }

  // Date type
  if (type === 'date') {
    return (
      <FormControl isInvalid={!!error} isRequired={required} isDisabled={disabled}>
        <FormLabel>{label}</FormLabel>
        <Input
          type="date"
          value={value || ''}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => onChange(e.target.value || undefined)}
          placeholder={placeholder}
        />
        {(error || helpText) && <FormHelperText color={error ? 'red.500' : undefined}>{error || helpText}</FormHelperText>}
      </FormControl>
    );
  }

  // DateTime type
  if (type === 'datetime') {
    return (
      <FormControl isInvalid={!!error} isRequired={required} isDisabled={disabled}>
        <FormLabel>{label}</FormLabel>
        <Input
          type="datetime-local"
          value={value ? value.slice(0, 16) : ''}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
            const val = e.target.value;
            onChange(val ? new Date(val).toISOString() : undefined);
          }}
          placeholder={placeholder}
        />
        {(error || helpText) && <FormHelperText color={error ? 'red.500' : undefined}>{error || helpText}</FormHelperText>}
      </FormControl>
    );
  }

  // Select type
  if (type === 'select' && allowedValues) {
    return (
      <FormControl isInvalid={!!error} isRequired={required} isDisabled={disabled}>
        <FormLabel>{label}</FormLabel>
        <Select
          value={value || ''}
          onChange={(e: React.ChangeEvent<HTMLSelectElement>) => onChange(e.target.value || undefined)}
          placeholder={placeholder || (required ? undefined : 'Brak')}
        >
          {!required && <option value="">Brak</option>}
          {allowedValues.map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </Select>
        {(error || helpText) && <FormHelperText color={error ? 'red.500' : undefined}>{error || helpText}</FormHelperText>}
      </FormControl>
    );
  }

  // Number types (integer, decimal, number)
  if (type === 'integer' || type === 'decimal' || type === 'number') {
    const step = type === 'integer' ? 1 : 0.01;
    
    return (
      <FormControl isInvalid={!!error} isRequired={required} isDisabled={disabled}>
        <FormLabel>{label}</FormLabel>
        <InputGroup>
          <Input
            type="number"
            value={value || ''}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
              const val = e.target.value;
              if (val === '') {
                onChange(undefined);
                return;
              }
              
              // Validate number
              const num = parseFloat(val);
              if (isNaN(num)) {
                return;
              }
              
              // Check min/max
              if (min !== undefined && num < min) {
                return;
              }
              if (max !== undefined && num > max) {
                return;
              }
              
              onChange(val);
            }}
            placeholder={placeholder}
            step={step}
            min={min}
            max={max}
          />
          {unit && <InputRightAddon>{unit}</InputRightAddon>}
        </InputGroup>
        {(error || helpText) && <FormHelperText color={error ? 'red.500' : undefined}>{error || helpText}</FormHelperText>}
      </FormControl>
    );
  }

  // Text type (default)
  return (
    <FormControl isInvalid={!!error} isRequired={required} isDisabled={disabled}>
      <FormLabel>{label}</FormLabel>
      <InputGroup>
        <Input
          type="text"
          value={value || ''}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
            const val = e.target.value;
            
            // Check length
            if (minLength !== undefined && val.length < minLength && val.length > 0) {
              // Show error but allow typing
            }
            if (maxLength !== undefined && val.length > maxLength) {
              return; // Don't allow exceeding max length
            }
            
            onChange(val || undefined);
          }}
          placeholder={placeholder}
          minLength={minLength}
          maxLength={maxLength}
        />
        {unit && <InputRightAddon>{unit}</InputRightAddon>}
      </InputGroup>
      {(error || helpText) && <FormHelperText color={error ? 'red.500' : undefined}>{error || helpText}</FormHelperText>}
    </FormControl>
  );
};

/**
 * Helper to determine field input type from backend field type
 * Supports legacy string type and new FieldTypeConfig flags
 */
export function getFieldInputType(
  fieldTypeOrConfig: string | { isNumeric: boolean; isText: boolean; isDate: boolean; isBoolean: boolean; isCollection: boolean; valueTypeName?: string },
  allowedValues?: string[]
): FieldInputType {
  if (allowedValues && allowedValues.length > 0) {
    return 'select';
  }

  if (typeof fieldTypeOrConfig !== 'string') {
    const cfg = fieldTypeOrConfig;
    if (cfg.isCollection) return 'select';
    if (cfg.isBoolean) return 'boolean';
    if (cfg.isDate) return 'date';
    if (cfg.isNumeric) {
      const name = (cfg.valueTypeName || '').toLowerCase();
      if (name.includes('integer') || name === 'int') return 'integer';
      return 'decimal';
    }
    if (cfg.isText) return 'text';
    // Fallback
    return 'text';
  }

  const lowerType = fieldTypeOrConfig.toLowerCase();

  if (lowerType.includes('integer') || lowerType.includes('int')) {
    return 'integer';
  }
  if (lowerType.includes('decimal') || lowerType.includes('float') || lowerType.includes('double')) {
    return 'decimal';
  }
  if (lowerType.includes('number') || lowerType.includes('quantity') || lowerType.includes('price') || lowerType.includes('vat') || lowerType.includes('value')) {
    return 'decimal';
  }
  if (lowerType.includes('bool')) {
    return 'boolean';
  }
  if (lowerType === 'datetime') {
    return 'datetime';
  }
  if (lowerType === 'date') {
    return 'date';
  }

  return 'text';
}
