import React, { useState } from 'react';
import {
  Box,
  Button,
  IconButton,
  VStack,
  HStack,
  Text,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  Badge,
  Collapse,
  useDisclosure,
  Alert,
  AlertIcon,
  Tooltip,
  Radio,
} from '@chakra-ui/react';
import { Plus, Trash2, ChevronDown, ChevronRight } from 'lucide-react';
import type {
  CostEstimateCollectionItem,
  GenericFieldDefinition,
  CalculatedFieldDefinition,
} from '../types/costEstimate.types';
import { CalculatedFieldRenderer, GenericFieldRenderer } from './FieldRenderer';
import { canAutoCalculate } from '../utils/calculationEngine';

interface CollectionFieldEditorProps {
  field: GenericFieldDefinition;
  value: CostEstimateCollectionItem[];
  onChange: (value: CostEstimateCollectionItem[]) => void;
  onSelectionChange?: (selectedItem: CostEstimateCollectionItem | null) => void;
  readOnly?: boolean;
}

export const CollectionFieldEditor: React.FC<CollectionFieldEditorProps> = ({
  field,
  value = [],
  onChange,
  onSelectionChange,
  readOnly = false,
}) => {
  const { isOpen, onToggle } = useDisclosure({ defaultIsOpen: true });

  if (!field.nestedFields) {
    return (
      <Alert status="warning" size="sm">
        <AlertIcon />
        Pole kolekcji nie ma zdefiniowanych zagnieżdżonych pól
      </Alert>
    );
  }

  const { calculatedFields, genericFields, minItems, maxItems, isSelectableCollection } = field.nestedFields;

  // Sortowanie pól według kolejności
  const visibleCalculatedFields = (calculatedFields || [])
    .filter((f) => f.visible)
    .sort((a, b) => a.order - b.order);

  const visibleGenericFields = (genericFields || [])
    .filter((f) => f.visible)
    .sort((a, b) => a.order - b.order);

  const canAddItem = !maxItems || value.length < maxItems;
  const canRemoveItem = !minItems || value.length > minItems;

  // Dodaj nowy element do kolekcji
  const handleAddItem = () => {
    if (!canAddItem) return;

    const newItem: CostEstimateCollectionItem = {
      id: `item-${Date.now()}-${Math.random()}`,
      calculatedFieldValues: {},
      genericFieldValues: {},
    };

    onChange([...value, newItem]);
  };

  // Usuń element z kolekcji
  const handleRemoveItem = (itemId: string) => {
    if (!canRemoveItem) return;
    onChange(value.filter((item) => item.id !== itemId));
  };

  // Obsługa zaznaczania opcji (tylko dla isSelectableCollection)
  const handleItemSelect = (itemId: string) => {
    if (!isSelectableCollection) return;

    // Sprawdź czy klikamy na już zaznaczony element
    const currentItem = value.find((item) => item.id === itemId);
    const wasSelected = currentItem?.isSelected || false;

    const updatedValue = value.map((item) => ({
      ...item,
      // Jeśli klikamy już zaznaczony element - odznaczamy (null)
      // W przeciwnym wypadku - zaznaczamy tylko kliknięty element
      isSelected: wasSelected ? false : item.id === itemId,
    }));

    onChange(updatedValue);

    // Powiadom rodzica o wybranej opcji (null jeśli odznaczono)
    const selectedItem = wasSelected ? null : updatedValue.find((item) => item.isSelected);
    onSelectionChange?.(selectedItem || null);
  };

  // Zaktualizuj wartość pola w elemencie
  const handleItemChange = (
    itemId: string,
    fieldName: string,
    fieldValue: any,
    isCalculated: boolean
  ) => {
    const updatedValue = value.map((item) => {
      if (item.id === itemId) {
        if (isCalculated) {
          return {
            ...item,
            calculatedFieldValues: {
              ...item.calculatedFieldValues,
              [fieldName]: fieldValue,
            },
          };
        } else {
          return {
            ...item,
            genericFieldValues: {
              ...item.genericFieldValues,
              [fieldName]: fieldValue,
            },
          };
        }
      }
      return item;
    });

    onChange(updatedValue);
  };

  // Oblicz sumy dla pól summable (jeśli włączone)
  const calculateSummaries = () => {
    if (!field.nestedFields?.enableCalculatedFieldsSummation) return null;

    const summableFields = field.nestedFields.summableCalculatedFields || [];
    const totals: Record<string, number> = {};

    summableFields.forEach((fieldName) => {
      const sum = value.reduce((acc, item) => {
        const fieldValue = item.calculatedFieldValues?.[fieldName];
        return acc + (typeof fieldValue === 'number' ? fieldValue : 0);
      }, 0);
      totals[fieldName] = sum;
    });

    return totals;
  };

  const summaries = calculateSummaries();

  return (
    <Box borderWidth="1px" borderRadius="md" overflow="hidden" bg="white">
      {/* Collection Header */}
      <Box bg="level2.50" p={3} borderBottomWidth={isOpen ? '1px' : '0'}>
        <HStack justify="space-between">
          <HStack spacing={3}>
            <IconButton
              aria-label="Toggle collection"
              icon={isOpen ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
              size="xs"
              variant="ghost"
              onClick={onToggle}
            />
            <Text fontWeight="medium" fontSize="sm">
              {field.label}
            </Text>
            <Badge colorScheme="level2" fontSize="xs">
              {value.length} {value.length === 1 ? 'element' : 'elementów'}
            </Badge>
            {minItems && (
              <Text fontSize="xs" color="gray.600">
                min: {minItems}
              </Text>
            )}
            {maxItems && (
              <Text fontSize="xs" color="gray.600">
                max: {maxItems}
              </Text>
            )}
          </HStack>

          {!readOnly && canAddItem && (
            <Button
              leftIcon={<Plus size={14} />}
              size="xs"
              colorScheme="level2"
              variant="ghost"
              onClick={handleAddItem}
            >
              Dodaj element
            </Button>
          )}
        </HStack>

        {field.description && (
          <Text fontSize="xs" color="gray.600" mt={1}>
            {field.description}
          </Text>
        )}
      </Box>

      {/* Collection Items Table */}
      <Collapse in={isOpen} animateOpacity>
        {value.length === 0 ? (
          <Box p={6} textAlign="center">
            <Text color="gray.500" fontSize="sm" mb={3}>
              Brak elementów w kolekcji
            </Text>
            {!readOnly && canAddItem && (
              <Button
                leftIcon={<Plus size={16} />}
                size="sm"
                colorScheme="level2"
                variant="outline"
                onClick={handleAddItem}
              >
                Dodaj pierwszy element
              </Button>
            )}
            {minItems && minItems > 0 && (
              <Alert status="warning" mt={3} size="sm">
                <AlertIcon />
                Wymagane minimum {minItems} {minItems === 1 ? 'element' : 'elementów'}
              </Alert>
            )}
          </Box>
        ) : (
          <Box overflowX="auto">
            <Table size="sm" variant="simple">
              <Thead bg="level2.50">
                <Tr>
                  {isSelectableCollection && <Th w="50px">Wybór</Th>}
                  <Th w="40px">#</Th>
                  {visibleCalculatedFields.map((f) => (
                    <Th key={f.name} minW="150px">
                      <VStack align="start" spacing={0}>
                        <HStack>
                          <Text>{f.label}</Text>
                          {f.required && <Text color="red.500">*</Text>}
                        </HStack>
                        {f.unit && (
                          <Text fontSize="xs" color="gray.500" fontWeight="normal">
                            [{f.unit}]
                          </Text>
                        )}
                      </VStack>
                    </Th>
                  ))}
                  {visibleGenericFields.map((f) => (
                    <Th key={f.name} minW="150px">
                      <HStack>
                        <Text>{f.label}</Text>
                        {f.required && <Text color="red.500">*</Text>}
                      </HStack>
                    </Th>
                  ))}
                  {!readOnly && <Th w="60px">Akcje</Th>}
                </Tr>
              </Thead>
              <Tbody>
                {value.map((item, index) => (
                  <Tr 
                    key={item.id} 
                    _hover={{ bg: 'level2.25' }}
                    bg={item.isSelected ? 'green.50' : undefined}
                    borderLeftWidth={item.isSelected ? '3px' : undefined}
                    borderLeftColor={item.isSelected ? 'green.500' : undefined}
                  >
                    {isSelectableCollection && (
                      <Td>
                        <Radio
                          isChecked={item.isSelected || false}
                          onChange={() => handleItemSelect(item.id)}
                          isDisabled={readOnly}
                          colorScheme="green"
                          size="lg"
                        />
                      </Td>
                    )}
                    <Td>
                      <Text fontSize="sm" color="gray.600" fontWeight={item.isSelected ? 'bold' : 'normal'}>
                        {index + 1}
                      </Text>
                    </Td>

                    {/* Calculated Fields */}
                    {visibleCalculatedFields.map((f) => {
                      // Przygotuj mapę valuesByType dla canAutoCalculate
                      const valuesByType: Record<number, any> = {};
                      (calculatedFields || []).forEach(field => {
                        if (field.name in (item.calculatedFieldValues || {})) {
                          valuesByType[field.type] = item.calculatedFieldValues![field.name];
                        }
                      });
                      const canAutoCalc = canAutoCalculate(f.type, valuesByType);
                      
                      return (
                        <Td key={f.name}>
                          <CalculatedFieldRenderer
                            field={f}
                            value={item.calculatedFieldValues?.[f.name]}
                            onChange={(val) => handleItemChange(item.id, f.name, val, true)}
                            allValues={{
                              ...item.calculatedFieldValues,
                              ...item.genericFieldValues,
                            }}
                            readOnly={readOnly}
                            canAutoCalculate={canAutoCalc}
                            compact
                          />
                        </Td>
                      );
                    })}

                    {/* Generic Fields */}
                    {visibleGenericFields.map((f) => (
                      <Td key={f.name}>
                        <GenericFieldRenderer
                          field={f}
                          value={item.genericFieldValues?.[f.name]}
                          onChange={(val) => handleItemChange(item.id, f.name, val, false)}
                          allValues={{
                            ...item.calculatedFieldValues,
                            ...item.genericFieldValues,
                          }}
                          readOnly={readOnly}
                          compact
                        />
                      </Td>
                    ))}

                    {/* Actions */}
                    {!readOnly && (
                      <Td>
                        <Tooltip
                          label={
                            canRemoveItem
                              ? 'Usuń element'
                              : `Wymagane minimum ${minItems} elementów`
                          }
                        >
                          <IconButton
                            aria-label="Delete item"
                            icon={<Trash2 size={14} />}
                            size="xs"
                            colorScheme="red"
                            variant="ghost"
                            onClick={() => handleRemoveItem(item.id)}
                            isDisabled={!canRemoveItem}
                          />
                        </Tooltip>
                      </Td>
                    )}
                  </Tr>
                ))}

                {/* Summary Row */}
                {summaries && Object.keys(summaries).length > 0 && (
                  <Tr bg="level2.100" fontWeight="bold">
                    <Td>
                      <Text fontSize="sm">Suma:</Text>
                    </Td>
                    {visibleCalculatedFields.map((f) => (
                      <Td key={f.name}>
                        {summaries[f.name] !== undefined ? (
                          <Text fontSize="sm" fontWeight="bold" color="level2.700">
                            {summaries[f.name].toFixed(2)}
                            {f.unit && ` ${f.unit}`}
                          </Text>
                        ) : (
                          <Text fontSize="sm" color="gray.400">
                            -
                          </Text>
                        )}
                      </Td>
                    ))}
                    {visibleGenericFields.map(() => (
                      <Td key={Math.random()}>
                        <Text fontSize="sm" color="gray.400">
                          -
                        </Text>
                      </Td>
                    ))}
                    {!readOnly && <Td />}
                  </Tr>
                )}
              </Tbody>
            </Table>
          </Box>
        )}
      </Collapse>

      {/* Validation Messages */}
      {minItems && value.length < minItems && (
        <Box p={2} bg="orange.50" borderTopWidth="1px">
          <Text fontSize="xs" color="orange.700">
            ⚠️ Wymagane minimum {minItems} {minItems === 1 ? 'element' : 'elementów'} (obecnie:{' '}
            {value.length})
          </Text>
        </Box>
      )}
      {maxItems && value.length >= maxItems && (
        <Box p={2} bg="primary.50" borderTopWidth="1px">
          <Text fontSize="xs" color="primary.700">
            ℹ️ Osiągnięto maksymalną liczbę elementów ({maxItems})
          </Text>
        </Box>
      )}
    </Box>
  );
};
