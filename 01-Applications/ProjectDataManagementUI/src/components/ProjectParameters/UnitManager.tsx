import { useState, useCallback } from "react";
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
} from "@chakra-ui/react";
import { Plus, Trash2, ChevronUp, ChevronDown } from "lucide-react";
import AppModal from "../ui/AppModal";
import DeleteAlertDialog from "../ui/DeleteAlertDialog";
import {
  useProjectUnits,
  useAddProjectUnit,
  useUpdateProjectUnit,
  useDeleteProjectUnit,
  useReorderProjectUnits,
} from "../../hooks/useProjectUnits";
import { useToastNotification } from "../../hooks/useToastNotification";
import { handleApiError } from "../../utils/handleApiError";
import type { ProjectUnitDto } from "../../api/projectApi";

interface UnitManagerProps {
  tenantId: string;
  projectId: string;
  canEdit: boolean;
}

type ModalMode = "add" | "edit";

interface FormData {
  code: string;
  name: string;
  symbol: string;
}

const EMPTY_FORM: FormData = { code: "", name: "", symbol: "" };

export default function UnitManager({
  tenantId,
  projectId,
  canEdit,
}: UnitManagerProps) {
  const { data: units, isLoading } = useProjectUnits(tenantId, projectId);
  const addMutation = useAddProjectUnit(tenantId, projectId);
  const updateMutation = useUpdateProjectUnit(tenantId, projectId);
  const deleteMutation = useDeleteProjectUnit(tenantId, projectId);
  const reorderMutation = useReorderProjectUnits(tenantId, projectId);
  const { showSuccess, showError } = useToastNotification();

  // Modal state
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [modalMode, setModalMode] = useState<ModalMode>("add");
  const [editingUnit, setEditingUnit] = useState<ProjectUnitDto | null>(null);
  const [formData, setFormData] = useState<FormData>(EMPTY_FORM);

  // Delete dialog state
  const [deleteTarget, setDeleteTarget] = useState<ProjectUnitDto | null>(null);

  const sortedUnits = units ?? [];

  // ---------------------------------------------------------------------------
  // Modal handlers
  // ---------------------------------------------------------------------------

  const openAddModal = useCallback(() => {
    setModalMode("add");
    setEditingUnit(null);
    setFormData(EMPTY_FORM);
    setIsModalOpen(true);
  }, []);

  const openEditModal = useCallback((unit: ProjectUnitDto) => {
    setModalMode("edit");
    setEditingUnit(unit);
    setFormData({
      code: unit.code,
      name: unit.name,
      symbol: unit.symbol ?? "",
    });
    setIsModalOpen(true);
  }, []);

  const closeModal = useCallback(() => {
    setIsModalOpen(false);
    setEditingUnit(null);
    setFormData(EMPTY_FORM);
  }, []);

  // ---------------------------------------------------------------------------
  // Save handler
  // ---------------------------------------------------------------------------

  const handleSave = useCallback(async () => {
    if (!formData.code.trim() || !formData.name.trim()) {
      showError("Błąd", "Kod i nazwa jednostki są wymagane");
      return;
    }

    try {
      if (modalMode === "add") {
        await addMutation.mutateAsync({
          code: formData.code.trim(),
          name: formData.name.trim(),
          symbol: formData.symbol.trim() || undefined,
        });
        showSuccess("Sukces", "Jednostka została dodana");
      } else if (editingUnit) {
        await updateMutation.mutateAsync({
          unitId: editingUnit.id,
          data: {
            code: formData.code.trim(),
            name: formData.name.trim(),
            symbol: formData.symbol.trim() || undefined,
            order: editingUnit.order,
          },
        });
        showSuccess("Sukces", "Jednostka została zaktualizowana");
      }
      closeModal();
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError(title ?? "Błąd", description ?? "Nie udało się zapisać jednostki");
    }
  }, [formData, modalMode, editingUnit, addMutation, updateMutation, showSuccess, showError, closeModal]);

  // ---------------------------------------------------------------------------
  // Delete handler
  // ---------------------------------------------------------------------------

  const handleDeleteConfirm = useCallback(async () => {
    if (!deleteTarget) return;

    try {
      await deleteMutation.mutateAsync(deleteTarget.id);
      showSuccess("Sukces", `Jednostka „${deleteTarget.code}” została usunięta`);
      setDeleteTarget(null);
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError(title ?? "Błąd", description ?? "Nie udało się usunąć jednostki");
    }
  }, [deleteTarget, deleteMutation, showSuccess, showError]);

  // ---------------------------------------------------------------------------
  // Reorder handlers
  // ---------------------------------------------------------------------------

  const handleMoveUp = useCallback(
    async (index: number) => {
      if (index <= 0) return;
      const ids = sortedUnits.map((u) => u.id);
      [ids[index - 1], ids[index]] = [ids[index], ids[index - 1]];
      try {
        await reorderMutation.mutateAsync(ids);
      } catch {
        showError("Błąd", "Nie udało się zmienić kolejności");
      }
    },
    [sortedUnits, reorderMutation, showError]
  );

  const handleMoveDown = useCallback(
    async (index: number) => {
      if (index >= sortedUnits.length - 1) return;
      const ids = sortedUnits.map((u) => u.id);
      [ids[index], ids[index + 1]] = [ids[index + 1], ids[index]];
      try {
        await reorderMutation.mutateAsync(ids);
      } catch {
        showError("Błąd", "Nie udało się zmienić kolejności");
      }
    },
    [sortedUnits, reorderMutation, showError]
  );

  // ---------------------------------------------------------------------------
  // Loading state
  // ---------------------------------------------------------------------------

  if (isLoading) {
    return (
      <HStack spacing={3}>
        <Spinner size="sm" color="primary.600" />
        <Text fontSize="sm" color="neutral.600">
          Ładowanie jednostek…
        </Text>
      </HStack>
    );
  }

  // ---------------------------------------------------------------------------
  // Read-only view
  // ---------------------------------------------------------------------------

  if (!canEdit) {
    if (sortedUnits.length === 0) {
      return (
        <Text fontSize="sm" color="neutral.500">
          Brak zdefiniowanych jednostek miary.
        </Text>
      );
    }

    return (
      <Box>
        <Table variant="simple" size="sm">
          <Thead>
            <Tr>
              <Th>Kod</Th>
              <Th>Nazwa</Th>
              <Th>Symbol</Th>
            </Tr>
          </Thead>
          <Tbody>
            {sortedUnits.map((unit, index) => (
              <Tr key={unit.id}>
                <Td fontWeight="semibold">{unit.code}</Td>
                <Td>{unit.name}</Td>
                <Td>{unit.symbol ?? "—"}</Td>
              </Tr>
            ))}
          </Tbody>
        </Table>
      </Box>
    );
  }

  // ---------------------------------------------------------------------------
  // Editable view
  // ---------------------------------------------------------------------------

  const isSaving = addMutation.isPending || updateMutation.isPending;
  const isValid = formData.code.trim().length > 0 && formData.name.trim().length > 0;

  return (
    <Box>
      <HStack justify="space-between" mb={4}>
        <Heading size="xs" color="neutral.600" textTransform="uppercase">
          {sortedUnits.length} jednostk{sortedUnits.length === 1 ? "a" : "i"}
        </Heading>
        <Button
          leftIcon={<Plus size={15} />}
          colorScheme="primary"
          size="sm"
          onClick={openAddModal}
        >
          Dodaj jednostkę
        </Button>
      </HStack>

      {sortedUnits.length === 0 ? (
        <Text fontSize="sm" color="neutral.500" py={4}>
          Brak zdefiniowanych jednostek miary. Kliknij „Dodaj jednostkę", aby dodać pierwszą.
        </Text>
      ) : (
        <Box overflowX="auto">
          <Table variant="simple" size="sm">
            <Thead>
              <Tr>
                <Th>Kod</Th>
                <Th>Nazwa</Th>
                <Th>Symbol</Th>
                <Th w="110px">Akcje</Th>
              </Tr>
            </Thead>
            <Tbody>
              {sortedUnits.map((unit, index) => (
                <Tr
                  key={unit.id}
                  cursor="pointer"
                  onClick={() => openEditModal(unit)}
                  _hover={{ bg: "neutral.50" }}
                >
                  <Td fontWeight="semibold">{unit.code}</Td>
                  <Td>{unit.name}</Td>
                  <Td>{unit.symbol ?? "—"}</Td>
                  <Td onClick={(e) => e.stopPropagation()}>
                    <HStack spacing={1}>
                      <IconButton
                        aria-label="Przenieś w górę"
                        icon={<ChevronUp size={14} />}
                        size="xs"
                        variant="ghost"
                        isDisabled={index === 0 || reorderMutation.isPending}
                        onClick={() => handleMoveUp(index)}
                      />
                      <IconButton
                        aria-label="Przenieś w dół"
                        icon={<ChevronDown size={14} />}
                        size="xs"
                        variant="ghost"
                        isDisabled={
                          index === sortedUnits.length - 1 ||
                          reorderMutation.isPending
                        }
                        onClick={() => handleMoveDown(index)}
                      />
                      <IconButton
                        aria-label="Usuń jednostkę"
                        icon={<Trash2 size={14} />}
                        size="xs"
                        variant="ghost"
                        colorScheme="red"
                        onClick={() => setDeleteTarget(unit)}
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

      {/* Add / Edit Modal */}
      <AppModal
        isOpen={isModalOpen}
        onClose={closeModal}
        title={modalMode === "add" ? "Dodaj jednostkę miary" : "Edytuj jednostkę miary"}
        actionLabel={modalMode === "add" ? "Dodaj" : "Zapisz"}
        actionColorScheme="primary"
        onAction={handleSave}
        isActionLoading={isSaving}
        isActionDisabled={!isValid || isSaving}
      >
        <VStack spacing={4} align="stretch">
          <FormControl isRequired>
            <FormLabel>Kod</FormLabel>
            <Input
              value={formData.code}
              onChange={(e) =>
                setFormData((prev) => ({ ...prev, code: e.target.value }))
              }
              placeholder="np. szt, m², kg"
              maxLength={20}
            />
          </FormControl>

          <FormControl isRequired>
            <FormLabel>Nazwa</FormLabel>
            <Input
              value={formData.name}
              onChange={(e) =>
                setFormData((prev) => ({ ...prev, name: e.target.value }))
              }
              placeholder="np. sztuka, metr kwadratowy, kilogram"
              maxLength={100}
            />
          </FormControl>

          <FormControl>
            <FormLabel>Symbol (opcjonalny)</FormLabel>
            <Input
              value={formData.symbol}
              onChange={(e) =>
                setFormData((prev) => ({ ...prev, symbol: e.target.value }))
              }
              placeholder="np. szt, m², kg"
              maxLength={10}
            />
          </FormControl>
        </VStack>
      </AppModal>

      {/* Delete confirmation */}
      <DeleteAlertDialog
        isOpen={deleteTarget !== null}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDeleteConfirm}
        itemName={deleteTarget ? `${deleteTarget.code} (${deleteTarget.name})` : undefined}
        isLoading={deleteMutation.isPending}
      />
    </Box>
  );
}
