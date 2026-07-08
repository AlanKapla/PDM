import React, { useState, useEffect } from 'react';
import {
  VStack,
  FormControl,
  FormLabel,
  Input,
  FormErrorMessage,
} from '@chakra-ui/react';
import AppModal from './ui/AppModal';
import { useAddProjectCostCategory } from '../hooks/useProjectCostCategories';
import { useToastNotification } from '../hooks/useToastNotification';

export interface CostCategoryQuickAddInitialValues {
  name?: string;
  code?: string;
}

export interface CostCategoryQuickAddModalProps {
  tenantId: string;
  projectId: string;
  isOpen: boolean;
  onClose: () => void;
  onCreated: (categoryId: string, categoryName: string) => void;
  /** Wartości wstępne — np. z sugestii AI */
  initialValues?: CostCategoryQuickAddInitialValues;
}

interface QuickAddFormState {
  name: string;
  code: string;
}

const EMPTY_FORM: QuickAddFormState = {
  name: '',
  code: '',
};

export function CostCategoryQuickAddModal({
  tenantId,
  projectId,
  isOpen,
  onClose,
  onCreated,
  initialValues,
}: CostCategoryQuickAddModalProps): React.ReactElement {
  const [form, setForm] = useState<QuickAddFormState>(EMPTY_FORM);
  const [nameError, setNameError] = useState<string>('');
  const mutation = useAddProjectCostCategory(tenantId, projectId);
  const { showError } = useToastNotification();

  useEffect(() => {
    if (isOpen) {
      setForm({
        name: initialValues?.name ?? '',
        code: initialValues?.code ?? '',
      });
      setNameError('');
    }
  }, [isOpen, initialValues?.name, initialValues?.code]);

  const handleClose = () => {
    setNameError('');
    onClose();
  };

  const handleAction = async () => {
    if (!form.name.trim()) {
      setNameError('Nazwa jest wymagana');
      return;
    }
    setNameError('');
    try {
      const categoryId: string = await mutation.mutateAsync({
        name: form.name.trim(),
        code: form.code.trim() || undefined,
      });
      onCreated(categoryId, form.name.trim());
      handleClose();
    } catch (err) {
      showError(
        'Błąd dodawania kategorii',
        err instanceof Error ? err.message : undefined
      );
    }
  };

  return (
    <AppModal
      isOpen={isOpen}
      onClose={handleClose}
      title="Dodaj kategorię kosztów"
      actionLabel="Dodaj"
      actionColorScheme="green"
      onAction={handleAction}
      isActionLoading={mutation.isPending}
      isActionDisabled={mutation.isPending}
    >
      <VStack spacing={4} align="stretch">
        <FormControl isRequired isInvalid={!!nameError}>
          <FormLabel>Nazwa *</FormLabel>
          <Input
            value={form.name}
            onChange={(e) => setForm((p) => ({ ...p, name: e.target.value }))}
            placeholder="Nazwa kategorii"
          />
          {nameError && <FormErrorMessage role="alert">{nameError}</FormErrorMessage>}
        </FormControl>

        <FormControl>
          <FormLabel>Kod</FormLabel>
          <Input
            value={form.code}
            onChange={(e) => setForm((p) => ({ ...p, code: e.target.value }))}
            placeholder="np. MAT, ROB"
          />
        </FormControl>
      </VStack>
    </AppModal>
  );
}

export default CostCategoryQuickAddModal;
