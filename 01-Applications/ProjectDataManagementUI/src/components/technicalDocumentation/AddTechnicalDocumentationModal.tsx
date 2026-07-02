import { useState } from 'react';
import {
  FormControl,
  FormLabel,
  Input,
  Textarea,
  VStack,
} from '@chakra-ui/react';
import AppModal from '../ui/AppModal';
import { MultiDocumentDropzone } from '../ui/MultiDocumentDropzone';
import { useCreateTechnicalDocumentation } from '../../hooks/queries';
import { useToastNotification } from '../../hooks/useToastNotification';

export interface AddTechnicalDocumentationModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
}

export function AddTechnicalDocumentationModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
}: AddTechnicalDocumentationModalProps): React.ReactElement {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [files, setFiles] = useState<File[]>([]);

  const { showSuccess, showApiError } = useToastNotification();
  const { mutateAsync: createDocumentation, isPending } = useCreateTechnicalDocumentation(
    tenantId,
    projectId
  );

  const resetForm = (): void => {
    setName('');
    setDescription('');
    setFiles([]);
  };

  const handleClose = (): void => {
    if (isPending) {
      return;
    }
    resetForm();
    onClose();
  };

  const handleSubmit = async (): Promise<void> => {
    if (!name.trim() || files.length === 0) {
      return;
    }

    try {
      await createDocumentation({
        name: name.trim(),
        description: description.trim() || undefined,
        files,
      });
      showSuccess(
        'Dokumentacja dodana',
        'Dokumentacja została dodana i oczekuje na przetwarzanie.'
      );
      resetForm();
      onClose();
    } catch (error) {
      showApiError(error);
    }
  };

  const isSubmitDisabled = !name.trim() || files.length === 0;

  return (
    <AppModal
      isOpen={isOpen}
      onClose={handleClose}
      title="Dodaj dokumentację techniczną"
      actionLabel="Dodaj"
      actionColorScheme="primary"
      onAction={handleSubmit}
      isActionLoading={isPending}
      isActionDisabled={isSubmitDisabled}
      cancelLabel="Anuluj"
    >
      <VStack spacing={4} align="stretch">
        <FormControl isRequired>
          <FormLabel>Nazwa</FormLabel>
          <Input
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Np. Dokumentacja budowlana — etap I"
            isDisabled={isPending}
          />
        </FormControl>

        <FormControl>
          <FormLabel>Opis</FormLabel>
          <Textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Opcjonalny opis dokumentacji"
            rows={3}
            isDisabled={isPending}
          />
        </FormControl>

        <FormControl isRequired>
          <FormLabel>Pliki</FormLabel>
          <MultiDocumentDropzone
            files={files}
            onFilesChange={setFiles}
            isDisabled={isPending}
          />
        </FormControl>
      </VStack>
    </AppModal>
  );
}
