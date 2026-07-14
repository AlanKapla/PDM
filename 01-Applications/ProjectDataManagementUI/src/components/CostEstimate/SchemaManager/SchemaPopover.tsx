/**
 * Lightweight schema manager popover (prototype style)
 * Alternative to full modal - for quick column visibility toggles
 */

import React, { useState } from 'react';
import {
  Popover,
  PopoverTrigger,
  PopoverContent,
  PopoverBody,
  PopoverHeader,
  Button,
  VStack,
  HStack,
  Text,
  IconButton,
  Input,
  Tooltip,
  Divider,
  Box,
} from '@chakra-ui/react';
import { Settings, Eye, EyeOff, Plus } from 'lucide-react';
import type { CostEstimateSchemaWeb, CostEstimateFieldDefinitionWeb } from '../../../types/costEstimate.types.new';
import { updateAdditionalField } from '../../../api/costEstimateApi';
import { useToastNotification } from '../../../hooks/useToastNotification';

interface SchemaPopoverProps {
  schema: CostEstimateSchemaWeb;
  costEstimateId: string;
  tenantId: string;
  projectId: string;
  onSchemaUpdated: () => void;
  onOpenFullManager: () => void;
  isReadOnly?: boolean;
}

export const SchemaPopover: React.FC<SchemaPopoverProps> = ({
  schema,
  costEstimateId,
  tenantId,
  projectId,
  onSchemaUpdated,
  onOpenFullManager,
  isReadOnly = false,
}) => {
  const { showSuccess, showError } = useToastNotification();
  const [isOpen, setIsOpen] = useState(false);

  // Separate visible and hidden fields
  const visibleFields = schema.fieldDefinitions.filter((f) => f.isVisible);
  const hiddenFields = schema.fieldDefinitions.filter((f) => !f.isVisible);

  const handleToggleVisibility = async (field: CostEstimateFieldDefinitionWeb): Promise<void> => {
    try {
      await updateAdditionalField(tenantId, projectId, costEstimateId, field.id, {
        name: field.label,
      });

      showSuccess(
        'Kolumna zaktualizowana',
        field.isVisible ? 'Kolumna ukryta' : 'Kolumna pokazana'
      );

      onSchemaUpdated();
    } catch (error) {
      showError('Błąd', 'Nie udało się zmienić widoczności kolumny');
    }
  };

  const getFieldScopeLabel = (fieldScope: number): string => {
    if (fieldScope === 0) return 'Grupa';
    if (fieldScope === 1) return 'System';
    if (fieldScope === 2) return 'Kalkulowane';
    if (fieldScope === 3) return 'Użytkownik';
    return 'Inne';
  };

  const getFieldTypeLabel = (fieldType: number): string => {
    if (fieldType === 101) return 'Liczba';
    if (fieldType === 102) return 'Tekst';
    if (fieldType >= 200 && fieldType < 300) return 'Auto';
    return 'Pole';
  };

  return (
    <Popover
      isOpen={isOpen}
      onClose={() => setIsOpen(false)}
      placement="bottom-start"
      closeOnBlur={true}
    >
      <PopoverTrigger>
        <Button
          leftIcon={<Settings size={14} />}
          size="sm"
          variant="outline"
          colorScheme="gray"
          onClick={() => setIsOpen(!isOpen)}
        >
          Kolumny
        </Button>
      </PopoverTrigger>

      <PopoverContent
        w="360px"
        borderRadius="14px"
        boxShadow="0 18px 50px rgba(20,33,47,0.16), 0 4px 14px rgba(20,33,47,0.08)"
        border="1px solid"
        borderColor="neutral.200"
      >
        <PopoverHeader
          borderBottom="none"
          pt={4}
          pb={2}
        >
          <HStack justify="space-between">
            <Text fontSize="md" fontWeight="extrabold" letterSpacing="-0.01em">
              Zarządzaj kolumnami
            </Text>
          </HStack>
          <Text fontSize="xs" color="neutral.500" mt={1}>
            Szybkie pokazywanie/ukrywanie kolumn
          </Text>
        </PopoverHeader>

        <PopoverBody pb={4}>
          <VStack align="stretch" spacing={3}>
            {/* Visible Fields */}
            {visibleFields.length > 0 && (
              <VStack align="stretch" spacing={1}>
                {visibleFields.map((field) => (
                  <HStack
                    key={field.id}
                    spacing={2}
                    px={1.5}
                    py={1}
                    borderRadius="9px"
                    _hover={{ bg: 'neutral.50' }}
                  >
                    <Tooltip label="Ukryj kolumnę">
                      <IconButton
                        aria-label="Ukryj"
                        icon={<Eye size={16} />}
                        size="sm"
                        variant="solid"
                        colorScheme="primary"
                        bg="primary.50"
                        color="primary.500"
                        _hover={{ bg: 'primary.100' }}
                        onClick={() => handleToggleVisibility(field)}
                        isDisabled={isReadOnly}
                        w="30px"
                        h="30px"
                        minW="30px"
                      />
                    </Tooltip>

                    <Text
                      flex={1}
                      fontSize="sm"
                      fontWeight="semibold"
                      minW={0}
                      isTruncated
                    >
                      {field.label}
                    </Text>

                    <Text
                      fontSize="xs"
                      fontWeight="bold"
                      color="primary.700"
                      bg="primary.50"
                      px={2}
                      py="2px"
                      borderRadius="6px"
                    >
                      {getFieldTypeLabel(field.fieldType)}
                    </Text>

                    <Text
                      fontSize="xs"
                      color="neutral.500"
                      maxW="92px"
                      isTruncated
                    >
                      {getFieldScopeLabel(field.fieldScope)}
                    </Text>
                  </HStack>
                ))}
              </VStack>
            )}

            {/* Divider */}
            {hiddenFields.length > 0 && (
              <>
                <Divider />
                <HStack
                  spacing={1.5}
                  fontSize="xs"
                  fontWeight="bold"
                  color="neutral.500"
                  textTransform="uppercase"
                  letterSpacing="0.05em"
                  px={1.5}
                >
                  <EyeOff size={12} />
                  <Text>Ukryte kolumny ({hiddenFields.length})</Text>
                </HStack>
              </>
            )}

            {/* Hidden Fields */}
            {hiddenFields.length > 0 && (
              <VStack align="stretch" spacing={1}>
                {hiddenFields.map((field) => (
                  <HStack
                    key={field.id}
                    spacing={2}
                    px={1.5}
                    py={1}
                    borderRadius="9px"
                    _hover={{ bg: 'neutral.50' }}
                  >
                    <Text
                      flex={1}
                      fontSize="sm"
                      fontWeight="semibold"
                      color="neutral.400"
                      minW={0}
                      isTruncated
                    >
                      {field.label}
                    </Text>

                    <Text
                      fontSize="xs"
                      fontWeight="bold"
                      color="neutral.500"
                      bg="neutral.100"
                      px={2}
                      py="2px"
                      borderRadius="6px"
                    >
                      {getFieldTypeLabel(field.fieldType)}
                    </Text>

                    <Tooltip label="Pokaż kolumnę">
                      <Button
                        leftIcon={<Eye size={14} />}
                        size="xs"
                        colorScheme="primary"
                        variant="solid"
                        onClick={() => handleToggleVisibility(field)}
                        isDisabled={isReadOnly}
                        fontSize="xs"
                        fontWeight="bold"
                      >
                        Pokaż
                      </Button>
                    </Tooltip>
                  </HStack>
                ))}
              </VStack>
            )}

            {/* Full Manager Button */}
            <Divider mt={1} />
            <Button
              leftIcon={<Plus size={14} />}
              size="sm"
              colorScheme="primary"
              variant="outline"
              onClick={() => {
                setIsOpen(false);
                onOpenFullManager();
              }}
              isDisabled={isReadOnly}
            >
              Zarządzaj wszystkimi kolumnami
            </Button>
          </VStack>
        </PopoverBody>
      </PopoverContent>
    </Popover>
  );
};
