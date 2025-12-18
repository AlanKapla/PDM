import React from 'react';
import {
  Input,
  NumberInput,
  NumberInputField,
  Checkbox,
  Select,
  Textarea,
  FormControl,
  FormLabel,
  FormErrorMessage,
  FormHelperText,
  HStack,
  Text,
  Icon,
  Tooltip,
} from '@chakra-ui/react';
import { Info, AlertCircle } from 'lucide-react';
import type {
  CalculatedFieldDefinition,
  GenericFieldDefinition,
  GroupHeaderFieldDefinition,
  CostEstimateCollectionItem,
} from '../types/costEstimate.types';
import {
  validateCalculatedField,
  validateGenericField,
  validateGroupHeaderField,
  evaluateVisibilityCondition,
  evaluateRequiredCondition,
  type ValidationError,
} from '../utils/fieldValidation';
import { CollectionFieldEditor } from './CollectionFieldEditor';

interface CalculatedFieldRendererProps {
  field: CalculatedFieldDefinition;
  value: number | null | undefined;
  onChange: (value: number | null) => void;
  allValues?: Record<string, any>;
  readOnly?: boolean;
  compact?: boolean;
  canAutoCalculate?: boolean; // Informacja czy pole może być automatycznie obliczone
}

export const CalculatedFieldRenderer: React.FC<CalculatedFieldRendererProps> = ({
  field,
  value,
  onChange,
  allValues = {},
  readOnly = false,
  compact = false,
  canAutoCalculate = false,
}) => {
  const [displayValue, setDisplayValue] = React.useState<string>('');
  const [isFocused, setIsFocused] = React.useState(false);

  // Synchronizuj displayValue z value gdy nie jest w fokusie
  React.useEffect(() => {
    if (!isFocused) {
      if (value !== null && value !== undefined) {
        const precision = field.displayFormat === 'N0' ? 0 : 2;
        setDisplayValue(value.toFixed(precision));
      } else {
        setDisplayValue('');
      }
    }
  }, [value, isFocused, field.displayFormat]);

  // Evaluate visibility
  const isVisible = field.visible && evaluateVisibilityCondition(field.visibilityCondition, allValues);
  if (!isVisible) return null;

  // Evaluate required
  const isRequired = field.required || evaluateRequiredCondition(field.requiredCondition, allValues);
  
  // Pole jest read-only gdy:
  // 1. Globalnie ustawione readOnly = true
  // 2. Pole ma field.readOnly = true
  // 3. Pole jest auto-calculated I można je obliczyć (są dostępne dane źródłowe)
  const isFieldReadOnly = readOnly || field.readOnly || (field.autoCalculated && canAutoCalculate);

  // Validation
  const error = validateCalculatedField(field, value, allValues);

  if (compact) {
    // Compact mode dla komórek tabeli
    return (
      <Tooltip
        label={error?.message || field.helpText}
        hasArrow
        isDisabled={!error && !field.helpText}
      >
        <NumberInput
          value={displayValue}
          onChange={(valueString, valueAsNumber) => {
            setDisplayValue(valueString);
            if (isNaN(valueAsNumber)) {
              onChange(null);
            } else {
              const precision = field.displayFormat === 'N0' ? 0 : 2;
              const rounded = precision === 0 ? Math.round(valueAsNumber) : Math.round(valueAsNumber * 100) / 100;
              onChange(rounded);
            }
          }}
          onFocus={() => setIsFocused(true)}
          onBlur={() => {
            setIsFocused(false);
            // Formatuj wartość po wyjściu z pola
            if (value !== null && value !== undefined) {
              const precision = field.displayFormat === 'N0' ? 0 : 2;
              setDisplayValue(value.toFixed(precision));
            }
          }}
          isReadOnly={isFieldReadOnly}
          isInvalid={!!error}
          precision={field.displayFormat === 'N0' ? 0 : 2}
          step={field.displayFormat === 'N0' ? 1 : 0.01}
          size="sm"
          bg={isFieldReadOnly ? 'gray.50' : 'white'}
        >
          <NumberInputField
            placeholder={field.defaultValue || '0'}
            borderColor={error ? 'red.300' : field.color ? field.color : undefined}
          />
        </NumberInput>
      </Tooltip>
    );
  }

  return (
    <FormControl isInvalid={!!error} isRequired={isRequired} isReadOnly={isFieldReadOnly}>
      <FormLabel fontSize="sm">
        <HStack spacing={2}>
          {field.icon && <Icon as={Info} boxSize={4} />}
          <Text color={field.color || 'inherit'}>{field.label}</Text>
          {field.helpText && (
            <Tooltip label={field.helpText} hasArrow>
              <Info size={14} />
            </Tooltip>
          )}
        </HStack>
      </FormLabel>

      <HStack>
        <NumberInput
          value={displayValue}
          onChange={(valueString, valueAsNumber) => {
            setDisplayValue(valueString);
            if (isNaN(valueAsNumber)) {
              onChange(null);
            } else {
              const precision = field.displayFormat === 'N0' ? 0 : 2;
              const rounded = precision === 0 ? Math.round(valueAsNumber) : Math.round(valueAsNumber * 100) / 100;
              onChange(rounded);
            }
          }}
          onFocus={() => setIsFocused(true)}
          onBlur={() => {
            setIsFocused(false);
            // Formatuj wartość po wyjściu z pola
            if (value !== null && value !== undefined) {
              const precision = field.displayFormat === 'N0' ? 0 : 2;
              setDisplayValue(value.toFixed(precision));
            }
          }}
          isReadOnly={isFieldReadOnly}
          precision={field.displayFormat === 'N0' ? 0 : 2}
          step={field.displayFormat === 'N0' ? 1 : 0.01}
          flex={1}
        >
          <NumberInputField
            placeholder={field.defaultValue || '0'}
            bg={isFieldReadOnly ? 'gray.50' : 'white'}
          />
        </NumberInput>
        {field.unit && (
          <Text fontSize="sm" color="gray.600" minW="60px">
            {field.unit}
          </Text>
        )}
      </HStack>

      {error && <FormErrorMessage>{error.message}</FormErrorMessage>}
      {!error && field.description && (
        <FormHelperText fontSize="xs">{field.description}</FormHelperText>
      )}
    </FormControl>
  );
};

interface GenericFieldRendererProps {
  field: GenericFieldDefinition;
  value: any;
  onChange: (value: any) => void;
  onSelectionChange?: (selectedItem: any | null) => void;
  allValues?: Record<string, any>;
  readOnly?: boolean;
  compact?: boolean;
}

export const GenericFieldRenderer: React.FC<GenericFieldRendererProps> = ({
  field,
  value,
  onChange,
  onSelectionChange,
  allValues = {},
  readOnly = false,
  compact = false,
}) => {
  const [displayValue, setDisplayValue] = React.useState<string>('');
  const [isFocused, setIsFocused] = React.useState(false);

  // Synchronizuj displayValue z value gdy nie jest w fokusie (dla Decimal i Integer)
  React.useEffect(() => {
    if (!isFocused && (field.type === 0 || field.type === 1)) {
      if (value !== null && value !== undefined) {
        const precision = field.type === 0 ? 0 : 2;
        setDisplayValue(value.toFixed(precision));
      } else {
        setDisplayValue('');
      }
    }
  }, [value, isFocused, field.type]);

  // Evaluate visibility
  const isVisible = field.visible && evaluateVisibilityCondition(field.visibilityCondition, allValues);
  if (!isVisible) return null;

  // Evaluate required
  const isRequired = field.required || evaluateRequiredCondition(field.requiredCondition, allValues);

  // Validation
  const error = validateGenericField(field, value, allValues);

  const renderInput = () => {
    switch (field.type) {
      case 0: // Integer
        return (
          <NumberInput
            value={displayValue}
            onChange={(valueString, valueAsNumber) => {
              setDisplayValue(valueString);
              if (isNaN(valueAsNumber)) {
                onChange(null);
              } else {
                onChange(Math.round(valueAsNumber));
              }
            }}
            onFocus={() => setIsFocused(true)}
            onBlur={() => {
              setIsFocused(false);
              if (value !== null && value !== undefined) {
                setDisplayValue(value.toFixed(0));
              }
            }}
            isReadOnly={readOnly}
            precision={0}
            step={1}
            min={field.minValue}
            max={field.maxValue}
            size={compact ? 'sm' : 'md'}
          >
            <NumberInputField
              placeholder={field.placeholder || field.defaultValue}
              bg={readOnly ? 'gray.50' : 'white'}
            />
          </NumberInput>
        );

      case 1: // Decimal
        return (
          <NumberInput
            value={displayValue}
            onChange={(valueString, valueAsNumber) => {
              setDisplayValue(valueString);
              if (isNaN(valueAsNumber)) {
                onChange(null);
              } else {
                onChange(Math.round(valueAsNumber * 100) / 100);
              }
            }}
            onFocus={() => setIsFocused(true)}
            onBlur={() => {
              setIsFocused(false);
              if (value !== null && value !== undefined) {
                setDisplayValue(value.toFixed(2));
              }
            }}
            isReadOnly={readOnly}
            precision={2}
            step={0.01}
            min={field.minValue}
            max={field.maxValue}
            size={compact ? 'sm' : 'md'}
          >
            <NumberInputField
              placeholder={field.placeholder || field.defaultValue}
              bg={readOnly ? 'gray.50' : 'white'}
            />
          </NumberInput>
        );

      case 2: // String
        if (field.allowedValues && field.allowedValues.length > 0) {
          // Select dla allowedValues
          return (
            <Select
              value={value || ''}
              onChange={(e) => onChange(e.target.value)}
              isReadOnly={readOnly}
              placeholder={field.placeholder || 'Wybierz...'}
              size={compact ? 'sm' : 'md'}
            >
              {field.allowedValues.map((val) => (
                <option key={val} value={val}>
                  {val}
                </option>
              ))}
            </Select>
          );
        }

        if (field.maxLength && field.maxLength > 100) {
          // Textarea dla długich tekstów
          return (
            <Textarea
              value={value || ''}
              onChange={(e) => onChange(e.target.value)}
              isReadOnly={readOnly}
              placeholder={field.placeholder || field.defaultValue}
              maxLength={field.maxLength}
              rows={compact ? 2 : 4}
              size={compact ? 'sm' : 'md'}
            />
          );
        }

        return (
          <Input
            value={value || ''}
            onChange={(e) => onChange(e.target.value)}
            isReadOnly={readOnly}
            placeholder={field.placeholder || field.defaultValue}
            maxLength={field.maxLength}
            size={compact ? 'sm' : 'md'}
          />
        );

      case 3: // Boolean
        return (
          <Checkbox
            isChecked={!!value}
            onChange={(e) => onChange(e.target.checked)}
            isReadOnly={readOnly}
            size={compact ? 'sm' : 'md'}
          >
            {compact ? '' : field.label}
          </Checkbox>
        );

      case 4: // Date
        return (
          <Input
            type="date"
            value={value ? (value instanceof Date ? value.toISOString().split('T')[0] : value) : ''}
            onChange={(e) => onChange(e.target.value)}
            isReadOnly={readOnly}
            size={compact ? 'sm' : 'md'}
          />
        );

      case 5: // DateTime
        return (
          <Input
            type="datetime-local"
            value={value ? (value instanceof Date ? value.toISOString().slice(0, 16) : value) : ''}
            onChange={(e) => onChange(e.target.value)}
            isReadOnly={readOnly}
            size={compact ? 'sm' : 'md'}
          />
        );

      case 10: // Collection
        if (compact) {
          return (
            <Text fontSize="sm" color="gray.500">
              Kolekcja ({Array.isArray(value) ? value.length : 0} elementów)
            </Text>
          );
        }
        return (
          <CollectionFieldEditor
            field={field}
            value={(value as CostEstimateCollectionItem[]) || []}
            onChange={onChange}
            onSelectionChange={onSelectionChange}
            readOnly={readOnly}
          />
        );

      default:
        return (
          <Input
            value={value || ''}
            onChange={(e) => onChange(e.target.value)}
            isReadOnly={readOnly}
            placeholder={field.placeholder || field.defaultValue}
            size={compact ? 'sm' : 'md'}
          />
        );
    }
  };

  if (compact) {
    return (
      <Tooltip
        label={error?.message || field.helpText}
        hasArrow
        isDisabled={!error && !field.helpText}
      >
        <FormControl isInvalid={!!error} size="sm">
          {renderInput()}
        </FormControl>
      </Tooltip>
    );
  }

  return (
    <FormControl isInvalid={!!error} isRequired={isRequired} isReadOnly={readOnly}>
      <FormLabel fontSize="sm">
        <HStack spacing={2}>
          {field.icon && <Icon as={Info} boxSize={4} />}
          <Text color={field.color || 'inherit'}>{field.label}</Text>
          {field.helpText && (
            <Tooltip label={field.helpText} hasArrow>
              <Info size={14} />
            </Tooltip>
          )}
        </HStack>
      </FormLabel>

      {renderInput()}

      {error && <FormErrorMessage>{error.message}</FormErrorMessage>}
      {!error && field.description && (
        <FormHelperText fontSize="xs">{field.description}</FormHelperText>
      )}
    </FormControl>
  );
};

interface GroupHeaderFieldRendererProps {
  field: GroupHeaderFieldDefinition;
  value: any;
  onChange: (value: any) => void;
  readOnly?: boolean;
  compact?: boolean;
}

export const GroupHeaderFieldRenderer: React.FC<GroupHeaderFieldRendererProps> = ({
  field,
  value,
  onChange,
  readOnly = false,
  compact = false,
}) => {
  const [displayValue, setDisplayValue] = React.useState<string>('');
  const [isFocused, setIsFocused] = React.useState(false);

  // Synchronizuj displayValue z value dla Budget (type 8)
  React.useEffect(() => {
    if (!isFocused && field.type === 8) {
      if (value !== null && value !== undefined) {
        setDisplayValue(value.toFixed(2));
      } else {
        setDisplayValue('');
      }
    }
  }, [value, isFocused, field.type]);

  if (!field.visible) return null;

  const isReadOnly = readOnly || field.readOnly;
  const error = validateGroupHeaderField(field, value);
  const label = compact ? '' : (field.customLabel || getDefaultGroupHeaderLabel(field.type));
  
  // GroupName (type 0) jest zawsze wymagane niezależnie od ustawienia w szablonie
  const isRequired = field.required || field.type === 0;

  const renderInput = () => {
    switch (field.type) {
      case 0: // GroupName
      case 1: // GroupDescription
      case 6: // Notes
      case 7: // Responsible
        return field.type === 1 || field.type === 6 ? (
          <Textarea
            value={value || ''}
            onChange={(e) => onChange(e.target.value)}
            isReadOnly={isReadOnly}
            placeholder={field.placeholder || field.defaultValue}
            rows={compact ? 2 : 3}
            size={compact ? 'sm' : 'md'}
          />
        ) : (
          <Input
            value={value || ''}
            onChange={(e) => onChange(e.target.value)}
            isReadOnly={isReadOnly}
            placeholder={field.placeholder || field.defaultValue}
            size={compact ? 'sm' : 'md'}
          />
        );

      case 2: // GroupNumber
        return (
          <Input
            value={value || ''}
            onChange={(e) => onChange(e.target.value)}
            isReadOnly={isReadOnly}
            placeholder={field.placeholder || field.defaultValue}
            size={compact ? 'sm' : 'md'}
          />
        );

      case 3: // StartDate
      case 4: // EndDate
        return (
          <Input
            type="date"
            value={value ? (value instanceof Date ? value.toISOString().split('T')[0] : value) : ''}
            onChange={(e) => onChange(e.target.value)}
            isReadOnly={isReadOnly}
            size={compact ? 'sm' : 'md'}
          />
        );

      case 5: // Status
        if (field.allowedValues && field.allowedValues.length > 0) {
          return (
            <Select
              value={value || ''}
              onChange={(e) => onChange(e.target.value)}
              isReadOnly={isReadOnly}
              placeholder={field.placeholder || 'Wybierz status...'}
              size={compact ? 'sm' : 'md'}
            >
              {field.allowedValues.map((val) => (
                <option key={val} value={val}>
                  {val}
                </option>
              ))}
            </Select>
          );
        }
        return (
          <Input
            value={value || ''}
            onChange={(e) => onChange(e.target.value)}
            isReadOnly={isReadOnly}
            placeholder={field.placeholder || field.defaultValue}
          />
        );

      case 8: // Budget
        return (
          <NumberInput
            value={displayValue}
            onChange={(valueString, valueAsNumber) => {
              setDisplayValue(valueString);
              if (isNaN(valueAsNumber)) {
                onChange(null);
              } else {
                onChange(Math.round(valueAsNumber * 100) / 100);
              }
            }}
            onFocus={() => setIsFocused(true)}
            onBlur={() => {
              setIsFocused(false);
              if (value !== null && value !== undefined) {
                setDisplayValue(value.toFixed(2));
              }
            }}
            isReadOnly={isReadOnly}
            precision={2}
            step={0.01}
            min={0}
            size={compact ? 'sm' : 'md'}
          >
            <NumberInputField placeholder={field.placeholder || field.defaultValue || '0.00'} />
          </NumberInput>
        );

      case 9: // Priority
        if (field.allowedValues && field.allowedValues.length > 0) {
          return (
            <Select
              value={value || ''}
              onChange={(e) => onChange(e.target.value)}
              isReadOnly={isReadOnly}
              placeholder={field.placeholder || 'Wybierz priorytet...'}
              size={compact ? 'sm' : 'md'}
            >
              {field.allowedValues.map((val) => (
                <option key={val} value={val}>
                  {val}
                </option>
              ))}
            </Select>
          );
        }
        return (
          <NumberInput
            value={value ?? ''}
            onChange={(_, valueAsNumber) => onChange(isNaN(valueAsNumber) ? null : valueAsNumber)}
            isReadOnly={isReadOnly}
            precision={0}
            step={1}
            min={1}
            size={compact ? 'sm' : 'md'}
          >
            <NumberInputField placeholder={field.placeholder || field.defaultValue || '1'} />
          </NumberInput>
        );

      default:
        return (
          <Input
            value={value || ''}
            onChange={(e) => onChange(e.target.value)}
            isReadOnly={isReadOnly}
            placeholder={field.placeholder || field.defaultValue}
            size={compact ? 'sm' : 'md'}
          />
        );
    }
  };

  if (compact) {
    // Compact mode dla komórek tabeli - tylko input bez labela
    return renderInput();
  }

  return (
    <FormControl isInvalid={!!error} isRequired={isRequired} isReadOnly={isReadOnly}>
      <FormLabel fontSize="sm" fontWeight="medium">
        <HStack spacing={2}>
          {field.icon && <Icon as={Info} boxSize={4} />}
          <Text color={field.color || 'inherit'}>{label}</Text>
          {field.helpText && (
            <Tooltip label={field.helpText} hasArrow>
              <Info size={14} />
            </Tooltip>
          )}
        </HStack>
      </FormLabel>

      {renderInput()}

      {error && <FormErrorMessage>{error.message}</FormErrorMessage>}
      {field.helpUrl && (
        <FormHelperText fontSize="xs">
          <a href={field.helpUrl} target="_blank" rel="noopener noreferrer" style={{ color: 'blue' }}>
            Więcej informacji
          </a>
        </FormHelperText>
      )}
    </FormControl>
  );
};

export function getDefaultGroupHeaderLabel(type: number): string {
  const labels: Record<number, string> = {
    0: 'Nazwa grupy',
    1: 'Opis grupy',
    2: 'Numer grupy',
    3: 'Data rozpoczęcia',
    4: 'Data zakończenia',
    5: 'Status',
    6: 'Uwagi',
    7: 'Odpowiedzialny',
    8: 'Budżet',
    9: 'Priorytet',
  };
  return labels[type] || 'Pole';
}
