import React, { useState, useCallback } from 'react';
import {
  Box,
  Button,
  FormControl,
  FormLabel,
  Heading,
  HStack,
  IconButton,
  Input,
  Spinner,
  Table,
  Tbody,
  Td,
  Text,
  Th,
  Thead,
  Tr,
  VStack,
} from '@chakra-ui/react';
import { Plus, Trash2, ChevronUp, ChevronDown } from 'lucide-react';
import AppModal from '../ui/AppModal';
import DeleteAlertDialog from '../ui/DeleteAlertDialog';
import {
  useProjectCostCategories,
  useAddProjectCostCategory,
  useUpdateProjectCostCategory,
  useDeleteProjectCostCategory,
  useReorderProjectCostCategories,
} from '../../hooks/useProjectCostCategories';
import { useToastNotification } from '../../hooks/useToastNotification';
import type { ProjectCostCategoryDto } from '../../api/projectApi';
import { CHART_PALETTE } from '../../features/dashboard/utils/chartTheme';

export interface CostCategoryManagerProps {
  tenantId: string;
  projectId: string;
  canEdit: boolean;
}

type ModalMode = 'add' | 'edit';

interface FormData {
  name: string;
  code: string;
  color: string;
}

const EMPTY_FORM: FormData = { name: '', code: '', color: '' };

function ColorSwatch({
  color,
  isSelected,
  onSelect,
  label,
}: {
  color: string;
  isSelected: boolean;
  onSelect: () => void;
  label: string;
}): React.ReactElement {
  return (
    <Box
      as="button"
      type="button"
      aria-label={label}
      aria-pressed={isSelected}
      w="28px"
      h="28px"
      borderRadius="md"
      bg={color}
      borderWidth="2px"
      borderColor={isSelected ? 'primary.600' : 'neutral.200'}
      onClick={onSelect}
      _hover={{ borderColor: 'primary.400' }}
    />
  );
}

function CategoryColorDot({ color }: { color?: string }): React.ReactElement | null {
  if (!color) {
    return null;
  }
  return (
    <Box
      as="span"
      display="inline-block"
      w="12px"
      h="12px"
      borderRadius="sm"
      bg={color}
      mr={2}
      verticalAlign="middle"
      aria-hidden="true"
    />
  );
}

export function CostCategoryManager({
  tenantId,
  projectId,
  canEdit,
}: CostCategoryManagerProps): React.ReactElement {
  const { data: categories, isLoading } = useProjectCostCategories(tenantId, projectId);
  const addMutation = useAddProjectCostCategory(tenantId, projectId);
  const updateMutation = useUpdateProjectCostCategory(tenantId, projectId);
  const deleteMutation = useDeleteProjectCostCategory(tenantId, projectId);
  const reorderMutation = useReorderProjectCostCategories(tenantId, projectId);
  const { showSuccess, showError, showApiError } = useToastNotification();

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [modalMode, setModalMode] = useState<ModalMode>('add');
  const [editingCategory, setEditingCategory] = useState<ProjectCostCategoryDto | null>(null);
  const [formData, setFormData] = useState<FormData>(EMPTY_FORM);
  const [deleteTarget, setDeleteTarget] = useState<ProjectCostCategoryDto | null>(null);

  const sortedCategories = categories ?? [];

  const openAddModal = useCallback(() => {
    setModalMode('add');
    setEditingCategory(null);
    setFormData(EMPTY_FORM);
    setIsModalOpen(true);
  }, []);

  const openEditModal = useCallback((category: ProjectCostCategoryDto) => {
    setModalMode('edit');
    setEditingCategory(category);
    setFormData({
      name: category.name,
      code: category.code ?? '',
      color: category.color ?? '',
    });
    setIsModalOpen(true);
  }, []);

  const closeModal = useCallback(() => {
    setIsModalOpen(false);
    setEditingCategory(null);
    setFormData(EMPTY_FORM);
  }, []);

  const handleSave = useCallback(async () => {
    if (!formData.name.trim()) {
      showError('Błąd', 'Nazwa kategorii jest wymagana');
      return;
    }

    const colorValue = formData.color.trim() || undefined;
    const codeValue = formData.code.trim() || undefined;

    try {
      if (modalMode === 'add') {
        await addMutation.mutateAsync({
          name: formData.name.trim(),
          code: codeValue,
          color: colorValue,
        });
        showSuccess('Sukces', 'Kategoria została dodana');
      } else if (editingCategory) {
        await updateMutation.mutateAsync({
          categoryId: editingCategory.id,
          data: {
            name: formData.name.trim(),
            code: codeValue,
            color: colorValue,
            order: editingCategory.order,
          },
        });
        showSuccess('Sukces', 'Kategoria została zaktualizowana');
      }
      closeModal();
    } catch (error) {
      showApiError(error);
    }
  }, [
    formData,
    modalMode,
    editingCategory,
    addMutation,
    updateMutation,
    showSuccess,
    showError,
    closeModal,
    showApiError,
  ]);

  const handleDeleteConfirm = useCallback(async () => {
    if (!deleteTarget) {
      return;
    }

    try {
      await deleteMutation.mutateAsync(deleteTarget.id);
      showSuccess('Sukces', `Kategoria „${deleteTarget.name}” została usunięta`);
      setDeleteTarget(null);
    } catch (error) {
      showApiError(error);
    }
  }, [deleteTarget, deleteMutation, showSuccess, showApiError]);

  const handleMoveUp = useCallback(
    async (index: number) => {
      if (index <= 0) {
        return;
      }
      const ids: string[] = sortedCategories.map((c) => c.id);
      [ids[index - 1], ids[index]] = [ids[index], ids[index - 1]];
      try {
        await reorderMutation.mutateAsync(ids);
      } catch (error) {
        showApiError(error);
      }
    },
    [sortedCategories, reorderMutation, showApiError]
  );

  const handleMoveDown = useCallback(
    async (index: number) => {
      if (index >= sortedCategories.length - 1) {
        return;
      }
      const ids: string[] = sortedCategories.map((c) => c.id);
      [ids[index], ids[index + 1]] = [ids[index + 1], ids[index]];
      try {
        await reorderMutation.mutateAsync(ids);
      } catch (error) {
        showApiError(error);
      }
    },
    [sortedCategories, reorderMutation, showApiError]
  );

  if (isLoading) {
    return (
      <HStack spacing={3}>
        <Spinner size="sm" color="primary.600" />
        <Text fontSize="sm" color="neutral.600">
          Ładowanie kategorii kosztów…
        </Text>
      </HStack>
    );
  }

  if (!canEdit) {
    if (sortedCategories.length === 0) {
      return (
        <Text fontSize="sm" color="neutral.500">
          Brak zdefiniowanych kategorii kosztów.
        </Text>
      );
    }

    return (
      <Box>
        <Table variant="simple" size="sm">
          <Thead>
            <Tr>
              <Th>Nazwa</Th>
              <Th>Kod</Th>
            </Tr>
          </Thead>
          <Tbody>
            {sortedCategories.map((category) => (
              <Tr key={category.id}>
                <Td>
                  <CategoryColorDot color={category.color} />
                  {category.name}
                </Td>
                <Td>{category.code ?? '—'}</Td>
              </Tr>
            ))}
          </Tbody>
        </Table>
      </Box>
    );
  }

  const isSaving = addMutation.isPending || updateMutation.isPending;
  const isValid = formData.name.trim().length > 0;

  return (
    <Box>
      <HStack justify="space-between" mb={4}>
        <Heading size="xs" color="neutral.600" textTransform="uppercase">
          {sortedCategories.length} kategorii
        </Heading>
        <Button
          leftIcon={<Plus size={15} aria-hidden="true" />}
          colorScheme="primary"
          size="sm"
          onClick={openAddModal}
        >
          Dodaj kategorię
        </Button>
      </HStack>

      {sortedCategories.length === 0 ? (
        <Text fontSize="sm" color="neutral.500" py={4}>
          Brak zdefiniowanych kategorii kosztów. Kliknij „Dodaj kategorię”, aby dodać pierwszą.
        </Text>
      ) : (
        <Box overflowX="auto">
          <Table variant="simple" size="sm">
            <Thead>
              <Tr>
                <Th>Nazwa</Th>
                <Th>Kod</Th>
                <Th w="110px">Akcje</Th>
              </Tr>
            </Thead>
            <Tbody>
              {sortedCategories.map((category, index) => (
                <Tr
                  key={category.id}
                  cursor="pointer"
                  onClick={() => openEditModal(category)}
                  _hover={{ bg: 'neutral.50' }}
                >
                  <Td fontWeight="semibold">
                    <CategoryColorDot color={category.color} />
                    {category.name}
                  </Td>
                  <Td>{category.code ?? '—'}</Td>
                  <Td onClick={(e) => e.stopPropagation()}>
                    <HStack spacing={1}>
                      <IconButton
                        aria-label="Przenieś w górę"
                        icon={<ChevronUp size={14} aria-hidden="true" />}
                        size="xs"
                        variant="ghost"
                        isDisabled={index === 0 || reorderMutation.isPending}
                        onClick={() => handleMoveUp(index)}
                      />
                      <IconButton
                        aria-label="Przenieś w dół"
                        icon={<ChevronDown size={14} aria-hidden="true" />}
                        size="xs"
                        variant="ghost"
                        isDisabled={
                          index === sortedCategories.length - 1 || reorderMutation.isPending
                        }
                        onClick={() => handleMoveDown(index)}
                      />
                      <IconButton
                        aria-label="Usuń kategorię"
                        icon={<Trash2 size={14} aria-hidden="true" />}
                        size="xs"
                        variant="ghost"
                        colorScheme="red"
                        onClick={() => setDeleteTarget(category)}
                        isDisabled={reorderMutation.isPending}
                      />
                    </HStack>
                  </Td>
                </Tr>
              ))}
            </Tbody>
          </Table>
        </Box>
      )}

      <AppModal
        isOpen={isModalOpen}
        onClose={closeModal}
        title={modalMode === 'add' ? 'Dodaj kategorię kosztów' : 'Edytuj kategorię kosztów'}
        actionLabel={modalMode === 'add' ? 'Dodaj' : 'Zapisz'}
        actionColorScheme="primary"
        onAction={handleSave}
        isActionLoading={isSaving}
        isActionDisabled={!isValid || isSaving}
      >
        <VStack spacing={4} align="stretch">
          <FormControl isRequired>
            <FormLabel>Nazwa</FormLabel>
            <Input
              value={formData.name}
              onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))}
              placeholder="np. Materiały, Robocizna, Sprzęt"
              maxLength={100}
            />
          </FormControl>

          <FormControl>
            <FormLabel>Kod (opcjonalny)</FormLabel>
            <Input
              value={formData.code}
              onChange={(e) => setFormData((prev) => ({ ...prev, code: e.target.value }))}
              placeholder="np. MAT, ROB"
              maxLength={20}
            />
          </FormControl>

          <FormControl>
            <FormLabel>Kolor (opcjonalny)</FormLabel>
            <HStack spacing={2} flexWrap="wrap" mb={2}>
              {CHART_PALETTE.map((paletteColor, index) => (
                <ColorSwatch
                  key={paletteColor}
                  color={paletteColor}
                  isSelected={formData.color === paletteColor}
                  onSelect={() => setFormData((prev) => ({ ...prev, color: paletteColor }))}
                  label={`Kolor ${index + 1}`}
                />
              ))}
            </HStack>
            <HStack spacing={3}>
              <Input
                type="color"
                value={formData.color || '#3182CE'}
                onChange={(e) => setFormData((prev) => ({ ...prev, color: e.target.value }))}
                w="60px"
                p={1}
                aria-label="Wybierz własny kolor"
              />
              <Input
                value={formData.color}
                onChange={(e) => setFormData((prev) => ({ ...prev, color: e.target.value }))}
                placeholder="#3182CE"
                maxLength={20}
              />
              {formData.color && (
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => setFormData((prev) => ({ ...prev, color: '' }))}
                >
                  Wyczyść
                </Button>
              )}
            </HStack>
          </FormControl>
        </VStack>
      </AppModal>

      <DeleteAlertDialog
        isOpen={deleteTarget !== null}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDeleteConfirm}
        itemName={deleteTarget ? deleteTarget.name : undefined}
        isLoading={deleteMutation.isPending}
      />
    </Box>
  );
}

export default CostCategoryManager;
