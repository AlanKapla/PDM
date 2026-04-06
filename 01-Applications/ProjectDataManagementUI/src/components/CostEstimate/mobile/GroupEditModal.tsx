import React, { useState, useCallback, useMemo } from 'react';
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
  Divider,
  Spacer,
  Collapse,
} from '@chakra-ui/react';
import { X, Trash2, ChevronDown } from 'lucide-react';
import type { CostEstimateGroupWeb } from '../../../types/costEstimate.types.new';
import type { RenderFieldInputFn } from '../costEstimateTableTypes';
import { getGroupDisplayName, formatCurrencyValue, getGroupSummaryValues } from './MobileFieldInput';

interface GroupEditModalProps {
  isOpen: boolean;
  onClose: () => void;
  group: CostEstimateGroupWeb;
  level: number;
  groupNumber: string;
  currencySymbol: string;
  templateStructure: any;
  editable: boolean;
  updateGroupFieldValue: (groupId: string, fieldId: string, value: string | undefined) => void;
  onDeleteGroup?: (groupId: string) => void;
  renderFieldInput: RenderFieldInputFn;
}

export const GroupEditModal: React.FC<GroupEditModalProps> = ({
  isOpen,
  onClose,
  group,
  level,
  groupNumber,
  currencySymbol,
  templateStructure,
  editable,
  updateGroupFieldValue,
  onDeleteGroup,
  renderFieldInput,
}) => {
  const groupHeaderFields: any[] = templateStructure?.groupHeaderFields ?? [];

  /** Pola nagłówka grupy w kolejności z uiConfiguration.columns */
  const orderedGroupHeaderFields = useMemo((): any[] => {
    const columns: any[] = templateStructure?.uiConfiguration?.columns || [];
    if (columns.length === 0) return groupHeaderFields;
    const sorted = [...columns].sort((a: any, b: any) => a.order - b.order);
    const result: any[] = [];
    for (const col of sorted) {
      const fieldDef = groupHeaderFields.find((f: any) => f.fieldName === col.fieldName);
      if (fieldDef) result.push(fieldDef);
    }
    // Dopisz pola nagłówka nieobecne w columns
    for (const field of groupHeaderFields) {
      if (!result.includes(field)) result.push(field);
    }
    return result;
  }, [templateStructure, groupHeaderFields]);

  // Draft stanu lokalnego — klucz: fieldId, wartość: string | undefined
  const [draft, setDraft] = useState<Record<string, string | undefined>>(() => {
    const initial: Record<string, string | undefined> = {};
    for (const field of groupHeaderFields) {
      const fv = group.fieldValues.find((v) => v.fieldDefinitionId === field.id);
      const val = fv?.stringValue ?? fv?.decimalValue?.toString() ?? fv?.boolValue?.toString() ?? fv?.dateTimeValue ?? undefined;
      initial[field.id] = val;
    }
    return initial;
  });

  // Czy jakieś pole zostało zmienione
  // Stan zwiń, po auto-zapisie nie potrzeba isDirty
  const [openSections, setOpenSections] = useState<Record<string, boolean>>({});
  const toggleSection = (key: string) =>
    setOpenSections((prev) => ({ ...prev, [key]: !prev[key] }));

  // Auto-zapis każdego pola etapu od razu przy zmianie
  const handleGroupFieldChange = useCallback(
    (fieldId: string, newValue: string | undefined) => {
      setDraft((prev) => ({ ...prev, [fieldId]: newValue }));
      updateGroupFieldValue(group.id, fieldId, newValue);
    },
    [group.id, updateGroupFieldValue]
  );


  const handleDelete = useCallback(() => {
    if (onDeleteGroup) {
      onDeleteGroup(group.id);
      onClose();
    }
  }, [onDeleteGroup, group.id, onClose]);

  const totalItems = (group.items ?? []).length +
    (group.childGroups ?? []).reduce((acc, cg) => acc + (cg.items ?? []).length, 0);

  const displayName = getGroupDisplayName(group, templateStructure, groupNumber);
  const summaryValues = getGroupSummaryValues(group, templateStructure);
  const fallbackValue = summaryValues.length === 0
    ? formatCurrencyValue(group.totalNet ?? group.totalGross, currencySymbol)
    : null;
  const typeLabel = level === 0 ? 'Etap główny' : 'Pod-etap';

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
                <Text fontSize="xs" color="gray.500">{typeLabel}</Text>
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
                  <HStack
                    bg={level === 0 ? 'primary.50' : 'action.50'}
                    borderRadius="md"
                    px={3}
                    py={2}
                    spacing={4}
                  >
                    <VStack align="start" spacing={0}>
                      <Text fontSize="xs" color="gray.500">Pozycji</Text>
                      <Badge colorScheme={level === 0 ? 'primary' : 'action'}>{totalItems}</Badge>
                    </VStack>
                    <Divider orientation="vertical" h="32px" />
                    {fallbackValue !== null ? (
                      <VStack align="start" spacing={0}>
                        <Text fontSize="xs" color="gray.500">Łączna wartość</Text>
                        <Text fontSize="sm" fontWeight="bold" color={level === 0 ? 'primary.700' : 'action.700'}>
                          {fallbackValue}
                        </Text>
                      </VStack>
                    ) : (
                      summaryValues.map((sv, i) => (
                        <React.Fragment key={sv.label}>
                          {i > 0 && <Divider orientation="vertical" h="32px" />}
                          <VStack align="start" spacing={0}>
                            <Text fontSize="xs" color="gray.500">{sv.label}</Text>
                            <Text fontSize="sm" fontWeight="bold" color={level === 0 ? 'primary.700' : 'action.700'}>
                              {formatCurrencyValue(sv.value, currencySymbol)}
                            </Text>
                          </VStack>
                        </React.Fragment>
                      ))
                    )}
                  </HStack>
                </Box>
              </Collapse>
            </Box>

            {/* Sekcja: Pola etapu */}
            <Box>
              <HStack
                px={0}
                py={3}
                cursor="pointer"
                onClick={() => toggleSection('fields')}
                justify="space-between"
                userSelect="none"
              >
                <Text fontSize="sm" fontWeight="semibold" color="gray.700">Pola etapu</Text>
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
                  {orderedGroupHeaderFields.length === 0 ? (
                    <Text fontSize="sm" color="gray.400" fontStyle="italic" textAlign="center" py={4}>
                      Brak pól do edycji dla tego etapu.
                    </Text>
                  ) : (
                    <VStack spacing={4} align="stretch">
                      {orderedGroupHeaderFields.map((field: any) => {
                        const rendered = renderFieldInput(
                          field,
                          draft[field.id],
                          (v) => handleGroupFieldChange(field.id, v),
                          !editable
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
                  )}
                </Box>
              </Collapse>
            </Box>

          </ModalBody>

          <ModalFooter borderTopWidth="1px" borderTopColor="gray.100">
            {editable && onDeleteGroup && (
              <Button
                colorScheme="red"
                variant="ghost"
                leftIcon={<Trash2 size={16} />}
                onClick={handleDelete}
                size="sm"
              >
                Usuń etap
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
