import React, { useState, useCallback } from 'react';
import {
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  Button,
  IconButton,
  HStack,
  VStack,
  Text,
  Box,
  FormControl,
  FormLabel,
  Spacer,
  Collapse,
} from '@chakra-ui/react';
import { X, Trash2, ChevronDown } from 'lucide-react';
import type { CostEstimateItemWeb } from '../../../types/costEstimate.types.new';
import type { FieldSource, RenderFieldInputFn } from '../costEstimateTableTypes';
import { getItemDisplayName, formatCurrencyValue, readItemFieldValue } from './MobileFieldInput';
import { useModalItemEdit } from '../../../hooks/useModalItemEdit';

interface ComponentEditModalProps {
  isOpen: boolean;
  onClose: () => void;
  component: CostEstimateItemWeb;
  groupId: string;
  parentItemId: string;
  currencySymbol: string;
  templateStructure: any;
  editable: boolean;
  updateComponentFieldValue: (
    groupId: string,
    itemId: string,
    componentId: string,
    fieldId: string,
    fieldSource: FieldSource,
    value: string | undefined
  ) => void;
  onDeleteItem?: (groupId: string, itemId: string) => void;
  renderFieldInput: RenderFieldInputFn;
  onUploadFiles?: (itemId: string, fieldDefinitionId: string, files: File[]) => Promise<string[]>;
  onUploadSuccess?: () => void;
}



export const ComponentEditModal: React.FC<ComponentEditModalProps> = ({
  isOpen,
  onClose,
  component,
  groupId,
  parentItemId,
  currencySymbol,
  templateStructure,
  editable,
  updateComponentFieldValue,
  onDeleteItem,
  renderFieldInput,
}) => {
  const onSaveField = useCallback(
    (fieldId: string, fieldSource: FieldSource, value: string | undefined) => {
      updateComponentFieldValue(groupId, parentItemId, component.id, fieldId, fieldSource, value);
    },
    [updateComponentFieldValue, groupId, parentItemId, component.id]
  );

  const { virtualItem, allValues, orderedFields, handleFieldChange } = useModalItemEdit({
    item: component,
    templateStructure,
    onSaveField,
  });

  // Stan zwiniętych sekcji — domyślnie wszystkie zwinięte
  const [openSections, setOpenSections] = useState<Record<string, boolean>>({});
  const toggleSection = (key: string) =>
    setOpenSections((prev) => ({ ...prev, [key]: !prev[key] }));

  const handleDelete = useCallback(() => {
    if (onDeleteItem) {
      // Komponent jest usuwany przez onDeleteItem (który w praktyce usuwa item-jako-komponent)
      onDeleteItem(groupId, component.id);
      onClose();
    }
  }, [onDeleteItem, groupId, component.id, onClose]);

  const displayName = getItemDisplayName(component, templateStructure, 1);
  const displayValue = formatCurrencyValue(component.netValue ?? component.grossValue, currencySymbol);

  return (
    <>
      <Modal
        isOpen={isOpen}
        onClose={onClose}
        scrollBehavior="inside"
        motionPreset="slideInBottom"
      >
        <ModalOverlay bg="blackAlpha.700" backdropFilter="blur(4px)" />
        <ModalContent
          borderTopRadius="2xl"
          borderBottomRadius={0}
          position="fixed"
          bottom={0}
          left={0}
          right={0}
          m={0}
          maxH="86dvh"
          display="flex"
          flexDirection="column"
        >
          {/* Drag handle */}
          <Box w="40px" h="4px" bg="gray.300" borderRadius="full" mx="auto" mt={2} mb={1} cursor="grab" />

          <ModalHeader pb={2}>
            <HStack justify="space-between" align="start">
              <VStack align="start" spacing="0">
                <Text fontSize="xs" color="gray.500">Komponent</Text>
                <Text fontWeight="bold" fontSize="lg" noOfLines={2}>{displayName}</Text>
              </VStack>
              <IconButton
                icon={<X size={18} />}
                variant="ghost"
                aria-label="Zamknij"
                onClick={onClose}
                mt={-1}
                mr={-2}
              />
            </HStack>
          </ModalHeader>

          <ModalBody pt={1} overflowY="auto" flex="1" px={4}>

            {/* Sekcja: Podsumowanie */}
            <Box borderBottomWidth="1px" borderColor="gray.100">
              <HStack
                px={0}
                py={3}
                cursor="pointer"
                onClick={() => toggleSection('summary')}
                justify="space-between"
                userSelect="none"
              >
                <Text fontSize="sm" fontWeight="semibold" color="gray.700">Podsumowanie</Text>
                <ChevronDown
                  size={16}
                  style={{
                    transform: openSections['summary'] ? 'rotate(180deg)' : 'rotate(0deg)',
                    transition: 'transform 0.2s',
                    color: 'var(--chakra-colors-gray-500)',
                  }}
                />
              </HStack>
              <Collapse in={openSections['summary'] ?? false} animateOpacity>
                <Box pb={4}>
                  <HStack bg="green.50" borderRadius="md" px={3} py={2}>
                    <Text fontSize="xs" color="gray.500">Wartość:</Text>
                    <Text fontSize="sm" fontWeight="bold" color="green.700">{displayValue}</Text>
                  </HStack>
                </Box>
              </Collapse>
            </Box>

            {/* Sekcja: Pola komponentu */}
            <Box>
              <HStack
                px={0}
                py={3}
                cursor="pointer"
                onClick={() => toggleSection('fields')}
                justify="space-between"
                userSelect="none"
              >
                <Text fontSize="sm" fontWeight="semibold" color="gray.700">Pola komponentu</Text>
                <ChevronDown
                  size={16}
                  style={{
                    transform: openSections['fields'] ? 'rotate(180deg)' : 'rotate(0deg)',
                    transition: 'transform 0.2s',
                    color: 'var(--chakra-colors-gray-500)',
                  }}
                />
              </HStack>
              <Collapse in={openSections['fields'] ?? false} animateOpacity>
                <Box pb={4}>
                  <VStack spacing={4} align="stretch">
                    {orderedFields.map((field: any) => {
                      const value = readItemFieldValue(virtualItem, field.id);
                      const fieldFv = virtualItem.fieldValues.find(
                        (fv) => fv.fieldDefinitionId === field.id
                      );
                      const filesForField = fieldFv?.files ?? null;
                      const rendered = renderFieldInput(
                        field,
                        value,
                        (newValue) => handleFieldChange(field.id, field, newValue),
                        !editable,
                        allValues,
                        component.id,
                        field.id,
                        filesForField
                      );
                      if (!rendered) return null;
                      return (
                        <FormControl key={field.id}>
                          <FormLabel fontSize="sm" color="gray.600" mb={1}>
                            {field.label || field.customLabel || field.fieldName}
                          </FormLabel>
                          {rendered}
                        </FormControl>
                      );
                    })}
                  </VStack>
                </Box>
              </Collapse>
            </Box>

          </ModalBody>

          <ModalFooter borderTopWidth="1px" borderTopColor="gray.100">
            {editable && onDeleteItem && (
              <Button
                colorScheme="red"
                variant="ghost"
                leftIcon={<Trash2 size={16} />}
                onClick={handleDelete}
                size="sm"
              >
                Usuń komponent
              </Button>
            )}
            <Spacer />
            <Button variant="outline" onClick={onClose} size="sm">
              Zamknij
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>

    </>
  );
};
