import React, { useState } from 'react';
import {
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
  Button,
  FormControl,
  FormLabel,
  Input,
  Select,
  VStack,
  FormHelperText,
} from '@chakra-ui/react';
import { addAdditionalField } from '../../../api/costEstimateApi';
import { useToastNotification } from '../../../hooks/useToastNotification';
import type { AdditionalFieldType } from '../../../types/costEstimate.types.new';

interface AddFieldModalProps {
  isOpen: boolean;
  onClose: () => void;
  costEstimateId: string;
  tenantId: string;
  projectId: string;
  onFieldAdded: () => void;
}

// Field types for user-defined additional fields (AdditionalFieldType enum)
// String=0, Decimal=1, Boolean=2, DateTime=3
const FIELD_TYPE_OPTIONS: { value: AdditionalFieldType; label: string; description: string }[] = [
  { value: 0, label: 'Tekst', description: 'Pole tekstowe' },
  { value: 1, label: 'Liczba', description: 'Pole numeryczne (decimal)' },
  { value: 2, label: 'Tak/Nie', description: 'Pole logiczne (checkbox)' },
  { value: 3, label: 'Data', description: 'Pole daty' },
];

export const AddFieldModal: React.FC<AddFieldModalProps> = ({
  isOpen,
  onClose,
  costEstimateId,
  tenantId,
  projectId,
  onFieldAdded,
}) => {
  const { showSuccess, showError, showApiError } = useToastNotification();
  const [label, setLabel] = useState('');
  const [fieldType, setFieldType] = useState<AdditionalFieldType>(0);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (): Promise<void> => {
    if (!label.trim()) {
      showError('Błąd', 'Nazwa pola jest wymagana');
      return;
    }

    setIsSubmitting(true);
    try {
      await addAdditionalField(tenantId, projectId, costEstimateId, {
        name: label.trim(),
        fieldType,
      });

      showSuccess('Pole dodane', `Pomyślnie dodano pole "${label.trim()}"`);

      setLabel('');
      setFieldType(0);
      onFieldAdded();
    } catch (error) {
      showApiError(error);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent): void => {
    if (e.key === 'Enter' && !isSubmitting) {
      handleSubmit();
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} size="md">
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>Dodaj nową kolumnę</ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          <VStack spacing={4}>
            <FormControl isRequired>
              <FormLabel fontSize="sm">Nazwa pola</FormLabel>
              <Input
                placeholder="np. Numer katalogowy"
                value={label}
                onChange={(e) => setLabel(e.target.value)}
                onKeyDown={handleKeyDown}
                autoFocus
              />
              <FormHelperText>Ta nazwa będzie widoczna w nagłówku kolumny</FormHelperText>
            </FormControl>

            <FormControl isRequired>
              <FormLabel fontSize="sm">Typ pola</FormLabel>
              <Select
                value={fieldType}
                onChange={(e) => setFieldType(Number(e.target.value))}
              >
                {FIELD_TYPE_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label} - {option.description}
                  </option>
                ))}
              </Select>
              <FormHelperText>Typ określa jaki rodzaj danych będzie przechowywany</FormHelperText>
            </FormControl>
          </VStack>
        </ModalBody>

        <ModalFooter>
          <Button variant="ghost" mr={3} onClick={onClose} isDisabled={isSubmitting}>
            Anuluj
          </Button>
          <Button
            colorScheme="primary"
            onClick={handleSubmit}
            isLoading={isSubmitting}
            loadingText="Dodawanie..."
          >
            Dodaj
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
};
