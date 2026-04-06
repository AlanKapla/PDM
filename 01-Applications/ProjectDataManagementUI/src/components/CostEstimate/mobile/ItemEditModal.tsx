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
  Badge,
  Spacer,
  Collapse,
  useDisclosure,
} from '@chakra-ui/react';
import { X, Trash2, Edit2, Plus, ChevronDown } from 'lucide-react';
import type { CostEstimateItemWeb } from '../../../types/costEstimate.types.new';
import type { FieldSource, RenderFieldInputFn } from '../costEstimateTableTypes';
import { getItemDisplayName, formatCurrencyValue, readItemFieldValue } from './MobileFieldInput';
import { useModalItemEdit } from '../../../hooks/useModalItemEdit';
import { ComponentEditModal } from './ComponentEditModal';

interface ItemEditModalProps {
  isOpen: boolean;
  onClose: () => void;
  item: CostEstimateItemWeb;
  groupId: string;
  itemNumber: number;
  currencySymbol: string;
  templateStructure: any;
  editable: boolean;
  updateItemFieldValue: (
    groupId: string,
    itemId: string,
    fieldId: string,
    fieldSource: FieldSource,
    value: string | undefined
  ) => void;
  updateComponentFieldValue: (
    groupId: string,
    itemId: string,
    componentId: string,
    fieldId: string,
    fieldSource: FieldSource,
    value: string | undefined
  ) => void;
  removeComponentFromItem: (groupId: string, itemId: string, componentId: string) => void;
  onDeleteItem?: (groupId: string, itemId: string) => void;
  onAddChildItem?: (
    groupId: string,
    parentItemId: string,
    relationType: 1 | 2
  ) => Promise<string | undefined>;
  renderFieldInput: RenderFieldInputFn;
  onUploadFiles?: (itemId: string, fieldDefinitionId: string, files: File[]) => Promise<string[]>;
  onUploadSuccess?: () => void;
}



export const ItemEditModal: React.FC<ItemEditModalProps> = ({
  isOpen,
  onClose,
  item,
  groupId,
  itemNumber,
  currencySymbol,
  templateStructure,
  editable,
  updateItemFieldValue,
  updateComponentFieldValue,
  removeComponentFromItem,
  onDeleteItem,
  onAddChildItem,
  renderFieldInput,
}) => {
  const onSaveField = useCallback(
    (fieldId: string, fieldSource: FieldSource, value: string | undefined) => {
      updateItemFieldValue(groupId, item.id, fieldId, fieldSource, value);
    },
    [updateItemFieldValue, groupId, item.id]
  );

  const { virtualItem, allValues, orderedFields, handleFieldChange } = useModalItemEdit({
    item,
    templateStructure,
    onSaveField,
  });

  // Stan zwiniętych sekcji — domyślnie wszystkie zwinięte
  const [openSections, setOpenSections] = useState<Record<string, boolean>>({});
  const toggleSection = (key: string) =>
    setOpenSections((prev) => ({ ...prev, [key]: !prev[key] }));

  const handleDelete = useCallback(() => {
    if (onDeleteItem) {
      onDeleteItem(groupId, item.id);
      onClose();
    }
  }, [onDeleteItem, groupId, item.id, onClose]);

  // --- Stan modalu komponentu ---
  const [selectedComponent, setSelectedComponent] = useState<CostEstimateItemWeb | null>(null);
  const { isOpen: isCompOpen, onOpen: openComp, onClose: closeComp } = useDisclosure();

  const openComponentModal = useCallback((comp: CostEstimateItemWeb) => {
    setSelectedComponent(comp);
    openComp();
  }, [openComp]);

  const handleAddComponent = useCallback(async () => {
    if (onAddChildItem) {
      await onAddChildItem(groupId, item.id, 2);
    }
  }, [onAddChildItem, groupId, item.id]);

  const components = item.components ?? [];
  const displayName = getItemDisplayName(item, templateStructure, itemNumber);
  const displayValue = formatCurrencyValue(item.netValue ?? item.grossValue, currencySymbol);

  return (
    <>
      <Modal
        isOpen={isOpen}
        onClose={onClose}
        scrollBehavior="inside"
        motionPreset="slideInBottom"
      >
        <ModalOverlay bg="blackAlpha.600" backdropFilter="blur(2px)" />
        <ModalContent
          borderTopRadius="2xl"
          borderBottomRadius={0}
          position="fixed"
          bottom={0}
          left={0}
          right={0}
          m={0}
          maxH="90dvh"
          display="flex"
          flexDirection="column"
        >
          {/* Drag handle */}
          <Box w="40px" h="4px" bg="gray.300" borderRadius="full" mx="auto" mt={2} mb={1} cursor="grab" />

          <ModalHeader pb={2}>
            <HStack justify="space-between" align="start">
              <VStack align="start" spacing="0">
                <Text fontSize="xs" color="gray.500">Pozycja</Text>
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
                  <HStack bg="primary.50" borderRadius="md" px={3} py={2}>
                    <Text fontSize="xs" color="gray.500">Wartość:</Text>
                    <Text fontSize="sm" fontWeight="bold" color="primary.700">{displayValue}</Text>
                  </HStack>
                </Box>
              </Collapse>
            </Box>

            {/* Sekcja: Pola pozycji */}
            <Box borderBottomWidth="1px" borderColor="gray.100">
              <HStack
                px={0}
                py={3}
                cursor="pointer"
                onClick={() => toggleSection('fields')}
                justify="space-between"
                userSelect="none"
              >
                <Text fontSize="sm" fontWeight="semibold" color="gray.700">Pola pozycji</Text>
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
                        item.id,
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

            {/* Sekcja: Komponenty */}
            {(components.length > 0 || (editable && onAddChildItem)) && (
              <Box>
                <HStack
                  px={0}
                  py={3}
                  cursor="pointer"
                  onClick={() => toggleSection('components')}
                  justify="space-between"
                  userSelect="none"
                >
                  <HStack spacing={2}>
                    <Text fontSize="sm" fontWeight="semibold" color="gray.700">Komponenty</Text>
                    {components.length > 0 && (
                      <Badge colorScheme="green">{components.length}</Badge>
                    )}
                  </HStack>
                  <ChevronDown
                    size={16}
                    style={{
                      transform: openSections['components'] ? 'rotate(180deg)' : 'rotate(0deg)',
                      transition: 'transform 0.2s',
                      color: 'var(--chakra-colors-gray-500)',
                    }}
                  />
                </HStack>
                <Collapse in={openSections['components'] ?? false} animateOpacity>
                  <Box pb={4}>
                    <VStack spacing={2} align="stretch" mb={2}>
                      {components.map((comp, idx) => (
                        <HStack
                          key={comp.id}
                          bg="green.50"
                          borderRadius="md"
                          px={3}
                          py={2}
                          justify="space-between"
                        >
                          <VStack align="start" spacing={0} flex={1} minW={0}>
                            <Text fontSize="xs" color="gray.500">Komponent {idx + 1}</Text>
                            <Text fontSize="sm" fontWeight="medium" isTruncated>
                              {getItemDisplayName(comp, templateStructure, idx + 1)}
                            </Text>
                          </VStack>
                          <HStack spacing={1} flexShrink={0}>
                            <Text fontSize="sm" color="green.700" fontWeight="medium">
                              {formatCurrencyValue(comp.netValue ?? comp.grossValue, currencySymbol)}
                            </Text>
                            {editable && (
                              <>
                                <IconButton
                                  aria-label="Edytuj komponent"
                                  icon={<Edit2 size={14} />}
                                  size="xs"
                                  colorScheme="green"
                                  variant="ghost"
                                  onClick={() => openComponentModal(comp)}
                                />
                                <IconButton
                                  aria-label="Usuń komponent"
                                  icon={<Trash2 size={14} />}
                                  size="xs"
                                  colorScheme="red"
                                  variant="ghost"
                                  onClick={() => removeComponentFromItem(groupId, item.id, comp.id)}
                                />
                              </>
                            )}
                          </HStack>
                        </HStack>
                      ))}
                    </VStack>
                    {editable && onAddChildItem && (
                      <Button
                        leftIcon={<Plus size={14} />}
                        variant="ghost"
                        colorScheme="green"
                        width="full"
                        size="sm"
                        onClick={handleAddComponent}
                      >
                        Dodaj komponent
                      </Button>
                    )}
                  </Box>
                </Collapse>
              </Box>
            )}

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
                Usuń pozycję
              </Button>
            )}
            <Spacer />
            <Button variant="outline" onClick={onClose} size="sm">
              Zamknij
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>

      {/* Modal edycji komponentu (zagnieżdżony) */}
      {selectedComponent && (
        <ComponentEditModal
          isOpen={isCompOpen}
          onClose={() => { closeComp(); setSelectedComponent(null); }}
          component={selectedComponent}
          groupId={groupId}
          parentItemId={item.id}
          currencySymbol={currencySymbol}
          templateStructure={templateStructure}
          editable={editable}
          renderFieldInput={renderFieldInput}
          updateComponentFieldValue={updateComponentFieldValue}
          onDeleteItem={onDeleteItem}
        />
      )}
    </>
  );
};
