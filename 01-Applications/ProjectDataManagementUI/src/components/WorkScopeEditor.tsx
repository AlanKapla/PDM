import React, { useState, useEffect } from 'react';
import {
  Box,
  FormControl,
  FormLabel,
  Input,
  NumberInput,
  NumberInputField,
  VStack,
  HStack,
  Text,
  Badge,
  Divider,
} from '@chakra-ui/react';
import type {
  CostEstimateWorkScope,
  CalculatedFieldDefinition,
  GenericFieldDefinition,
} from '../types/costEstimate.types';
import { useCalculations } from '../hooks/useCalculations';
import { formatCalculatedValue } from '../utils/calculationEngine';

export interface WorkScopeEditorProps {
  workScope: CostEstimateWorkScope;
  calculatedFields: CalculatedFieldDefinition[];
  genericFields: GenericFieldDefinition[];
  onChange: (workScope: CostEstimateWorkScope) => void;
  readOnly?: boolean;
}

/**
 * Component for editing a single work scope with automatic calculations
 */
export const WorkScopeEditor: React.FC<WorkScopeEditorProps> = ({
  workScope,
  calculatedFields,
  genericFields,
  onChange,
  readOnly = false,
}) => {
  const [localWorkScope, setLocalWorkScope] = useState<CostEstimateWorkScope>(workScope);

  const { recalculateWorkScope, isAutoCalculated } = useCalculations({
    calculatedFields,
    genericFields,
  });

  // Update local state when prop changes
  useEffect(() => {
    setLocalWorkScope(workScope);
  }, [workScope]);

  // Handle calculated field change
  const handleCalculatedFieldChange = (fieldName: string, value: number | null) => {
    const updated = {
      ...localWorkScope,
      calculatedFieldValues: {
        ...localWorkScope.calculatedFieldValues,
        [fieldName]: value,
      },
    };

    // Recalculate dependent fields
    const recalculated = recalculateWorkScope(updated);
    setLocalWorkScope(recalculated);
    onChange(recalculated);
  };

  // Handle generic field change
  const handleGenericFieldChange = (fieldName: string, value: any) => {
    const updated = {
      ...localWorkScope,
      genericFieldValues: {
        ...localWorkScope.genericFieldValues,
        [fieldName]: value,
      },
    };

    setLocalWorkScope(updated);
    onChange(updated);
  };

  // Render calculated field
  const renderCalculatedField = (field: CalculatedFieldDefinition) => {
    const value = localWorkScope.calculatedFieldValues[field.name];
    const isReadOnly = readOnly || field.readOnly || isAutoCalculated(field.name);

    return (
      <FormControl key={field.name}>
        <FormLabel>
          <HStack spacing={2}>
            <Text>{field.label}</Text>
            {isAutoCalculated(field.name) && (
              <Badge colorScheme="primary" fontSize="xs">
                Auto
              </Badge>
            )}
            {field.required && (
              <Text color="red.500" fontSize="sm">
                *
              </Text>
            )}
          </HStack>
          {field.helpText && (
            <Text fontSize="xs" color="neutral.500" fontWeight="normal">
              {field.helpText}
            </Text>
          )}
        </FormLabel>

        <HStack>
          <NumberInput
            value={value ?? ''}
            onChange={(_, valueAsNumber) =>
              handleCalculatedFieldChange(field.name, isNaN(valueAsNumber) ? null : valueAsNumber)
            }
            isReadOnly={isReadOnly}
            isDisabled={readOnly}
            precision={2}
            step={0.01}
            flex={1}
          >
            <NumberInputField
              placeholder={field.defaultValue || '0.00'}
              bg={isAutoCalculated(field.name) ? 'gray.50' : 'white'}
            />
          </NumberInput>
          {field.unit && (
            <Text fontSize="sm" color="neutral.600" minW="40px">
              {field.unit}
            </Text>
          )}
        </HStack>

        {/* Display formatted value for auto-calculated fields */}
        {isAutoCalculated(field.name) && value !== null && value !== undefined && (
          <Text fontSize="sm" color="primary.600" mt={1}>
            = {formatCalculatedValue(value, field.displayFormat, field.unit)}
          </Text>
        )}
      </FormControl>
    );
  };

  // Render generic field
  const renderGenericField = (field: GenericFieldDefinition) => {
    const value = localWorkScope.genericFieldValues[field.name];

    return (
      <FormControl key={field.name}>
        <FormLabel>
          <HStack spacing={2}>
            <Text>{field.label}</Text>
            {field.required && (
              <Text color="red.500" fontSize="sm">
                *
              </Text>
            )}
          </HStack>
          {field.helpText && (
            <Text fontSize="xs" color="neutral.500" fontWeight="normal">
              {field.helpText}
            </Text>
          )}
        </FormLabel>

        {/* Simple text input for generic fields - can be expanded based on type */}
        <Input
          value={value ?? ''}
          onChange={(e) => handleGenericFieldChange(field.name, e.target.value)}
          placeholder={field.placeholder || field.defaultValue}
          isReadOnly={readOnly}
        />
      </FormControl>
    );
  };

  return (
    <Box p={4} borderWidth="1px" borderRadius="md" bg="white">
      <VStack spacing={4} align="stretch">
        {/* Calculated Fields Section */}
        {calculatedFields.filter(f => f.visible).length > 0 && (
          <>
            <Text fontWeight="bold" fontSize="lg">
              Pola kalkulowane
            </Text>
            {calculatedFields
              .filter(f => f.visible)
              .sort((a, b) => a.order - b.order)
              .map(field => renderCalculatedField(field))}
          </>
        )}

        {/* Divider */}
        {calculatedFields.filter(f => f.visible).length > 0 &&
          genericFields.filter(f => f.visible).length > 0 && <Divider />}

        {/* Generic Fields Section */}
        {genericFields.filter(f => f.visible).length > 0 && (
          <>
            <Text fontWeight="bold" fontSize="lg">
              Pola dodatkowe
            </Text>
            {genericFields
              .filter(f => f.visible)
              .sort((a, b) => a.order - b.order)
              .map(field => renderGenericField(field))}
          </>
        )}

        {/* Summary Display */}
        {calculatedFields.some(f => f.summable) && (
          <>
            <Divider />
            <Box bg="primary.50" p={3} borderRadius="md">
              <Text fontWeight="bold" fontSize="sm" mb={2}>
                Podsumowanie
              </Text>
              <VStack spacing={1} align="stretch">
                {calculatedFields
                  .filter(f => f.summable && f.visible)
                  .map(field => {
                    const value = localWorkScope.calculatedFieldValues[field.name];
                    return (
                      <HStack key={field.name} justify="space-between">
                        <Text fontSize="sm">{field.label}:</Text>
                        <Text fontSize="sm" fontWeight="medium">
                          {formatCalculatedValue(value, field.displayFormat, field.unit)}
                        </Text>
                      </HStack>
                    );
                  })}
              </VStack>
            </Box>
          </>
        )}
      </VStack>
    </Box>
  );
};
