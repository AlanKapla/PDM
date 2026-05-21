import React, { useState, useCallback, useRef } from 'react';
import {
  Box,
  Heading,
  HStack,
  VStack,
  Button,
  Input,
  InputGroup,
  InputLeftElement,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  IconButton,
  Tooltip,
  FormControl,
  FormLabel,
  Textarea,
} from '@chakra-ui/react';
import { Users, Plus, Edit2, Trash2, Search, Loader } from 'lucide-react';
import MainLayout from '../layout/MainLayout';
import { LoadingSpinner, EmptyState } from '../components/common';
import AppModal from '../components/ui/AppModal';
import DeleteAlertDialog from '../components/ui/DeleteAlertDialog';
import { useAuth } from '../context/AuthContext';
import { useTenantPermissions } from '../hooks/useTenantPermissions';
import { useModal } from '../hooks/useModal';
import { useToastNotification } from '../hooks/useToastNotification';
import {
  useContractors,
  useCreateContractor,
  useUpdateContractor,
  useDeleteContractor,
} from '../hooks/queries/useContractors';
import type {  ContractorWeb,
  CreateContractorRequest,
} from '../types/contractor.types';

interface ContractorFormValues {
  name: string;
  taxId: string;
  email: string;
  phoneNumber: string;
  street: string;
  city: string;
  postalCode: string;
  country: string;
  notes: string;
}

const emptyForm: ContractorFormValues = {
  name: '',
  taxId: '',
  email: '',
  phoneNumber: '',
  street: '',
  city: '',
  postalCode: '',
  country: '',
  notes: '',
};

export default function ContractorsPage(): React.ReactElement {
  const { user } = useAuth();
  const tenantId = user?.activeTenantId ?? '';
  const { canEdit } = useTenantPermissions();
  const { showSuccess, showError } = useToastNotification();

  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const handleSearchChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      const value = e.target.value;
      setSearch(value);
      if (debounceRef.current) clearTimeout(debounceRef.current);
      debounceRef.current = setTimeout(() => setDebouncedSearch(value), 300);
    },
    []
  );

  const { data: contractors = [], isLoading } = useContractors(
    tenantId || undefined,
    debouncedSearch
  );

  const createMutation = useCreateContractor(tenantId);
  const updateMutation = useUpdateContractor(tenantId);
  const deleteMutation = useDeleteContractor(tenantId);

  const formModal = useModal();
  const deleteDialog = useModal();

  const [editingContractor, setEditingContractor] =
    useState<ContractorWeb | null>(null);
  const [deletingContractor, setDeletingContractor] =
    useState<ContractorWeb | null>(null);
  const [form, setForm] = useState<ContractorFormValues>(emptyForm);
  const [loadingEdit, setLoadingEdit] = useState(false);

  const handleOpenCreate = useCallback(() => {
    setEditingContractor(null);
    setForm(emptyForm);
    formModal.onOpen();
  }, [formModal]);

  const handleOpenEdit = useCallback(
    async (contractor: ContractorWeb) => {
      setEditingContractor(contractor);
      setForm({
        name: contractor.name,
        taxId: contractor.taxId ?? '',
        email: contractor.email ?? '',
        phoneNumber: contractor.phoneNumber ?? '',
        street: contractor.street ?? '',
        city: contractor.city ?? '',
        postalCode: contractor.postalCode ?? '',
        country: contractor.country ?? '',
        notes: contractor.notes ?? '',
      });
      setLoadingEdit(false);
      formModal.onOpen();
    },
    [formModal]
  );

  const handleOpenDelete = useCallback(
    (contractor: ContractorWeb) => {
      setDeletingContractor(contractor);
      deleteDialog.onOpen();
    },
    [deleteDialog]
  );

  const handleSave = useCallback(async () => {
    if (!form.name.trim()) {
      showError('Błąd', 'Nazwa kontrahenta jest wymagana');
      return;
    }

    const payload: CreateContractorRequest = {
      name: form.name.trim(),
      taxId: form.taxId || null,
      email: form.email || null,
      phoneNumber: form.phoneNumber || null,
      street: form.street || null,
      city: form.city || null,
      postalCode: form.postalCode || null,
      country: form.country || null,
      notes: form.notes || null,
    };

    try {
      if (editingContractor) {
        await updateMutation.mutateAsync({
          contractorId: editingContractor.id,
          data: { ...payload, id: editingContractor.id },
        });
        showSuccess('Sukces', 'Kontrahent zaktualizowany');
      } else {
        await createMutation.mutateAsync(payload);
        showSuccess('Sukces', 'Kontrahent dodany');
      }
      formModal.onClose();
    } catch {
      showError('Błąd', 'Nie udało się zapisać kontrahenta');
    }
  }, [
    form,
    editingContractor,
    createMutation,
    updateMutation,
    formModal,
    showSuccess,
    showError,
  ]);

  const handleDelete = useCallback(async () => {
    if (!deletingContractor) return;
    try {
      await deleteMutation.mutateAsync(deletingContractor.id);
      showSuccess('Sukces', 'Kontrahent usunięty');
      deleteDialog.onClose();
    } catch {
      showError('Błąd', 'Nie udało się usunąć kontrahenta');
    }
  }, [deletingContractor, deleteMutation, deleteDialog, showSuccess, showError]);

  const isSaving = createMutation.isPending || updateMutation.isPending;

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 6 }}>
        <HStack justify="space-between" mb={6}>
          <Heading size="lg">Kontrahenci</Heading>
          {canEdit && (
            <Button
              leftIcon={<Plus size={16} />}
              colorScheme="green"
              size="sm"
              onClick={handleOpenCreate}
            >
              Dodaj kontrahenta
            </Button>
          )}
        </HStack>

        <Box mb={4}>
          <InputGroup maxW="md">
            <InputLeftElement pointerEvents="none">
              <Search size={16} />
            </InputLeftElement>
            <Input
              placeholder="Szukaj kontrahenta..."
              value={search}
              onChange={handleSearchChange}
            />
          </InputGroup>
        </Box>

        {isLoading ? (
          <LoadingSpinner />
        ) : contractors.length === 0 ? (
          <EmptyState
            icon={Users}
            title="Brak kontrahentów"
            description="Dodaj pierwszego kontrahenta klikając przycisk powyżej."
          />
        ) : (
          <Box overflowX="auto">
            <Table variant="simple" size="sm">
              <Thead>
                <Tr>
                  <Th>Nazwa</Th>
                  <Th>NIP</Th>
                  <Th>Email</Th>
                  <Th>Telefon</Th>
                  <Th>Miasto</Th>
                  {canEdit && <Th>Akcje</Th>}
                </Tr>
              </Thead>
              <Tbody>
                {contractors.map((contractor) => (
                  <Tr key={contractor.id}>
                    <Td fontWeight="medium">{contractor.name}</Td>
                    <Td>{contractor.taxId ?? '—'}</Td>
                    <Td>{contractor.email ?? '—'}</Td>
                    <Td>{contractor.phoneNumber ?? '—'}</Td>
                    <Td>{contractor.city ?? '—'}</Td>
                    {canEdit && (
                      <Td>
                        <HStack spacing={1}>
                          <Tooltip label="Edytuj">
                            <IconButton
                              aria-label="Edytuj kontrahenta"
                              icon={<Edit2 size={14} />}
                              size="xs"
                              variant="ghost"
                              onClick={() => handleOpenEdit(contractor)}
                            />
                          </Tooltip>
                          <Tooltip label="Usuń">
                            <IconButton
                              aria-label="Usuń kontrahenta"
                              icon={<Trash2 size={14} />}
                              size="xs"
                              variant="ghost"
                              colorScheme="red"
                              onClick={() => handleOpenDelete(contractor)}
                            />
                          </Tooltip>
                        </HStack>
                      </Td>
                    )}
                  </Tr>
                ))}
              </Tbody>
            </Table>
          </Box>
        )}

        <AppModal
          isOpen={formModal.isOpen}
          onClose={formModal.onClose}
          title={editingContractor ? 'Edytuj kontrahenta' : 'Dodaj kontrahenta'}
          actionLabel={editingContractor ? 'Zapisz' : 'Dodaj'}
          actionColorScheme="green"
          onAction={handleSave}
          isActionLoading={isSaving}
          isActionDisabled={!form.name.trim() || loadingEdit}
          desktopSize="xl"
        >
          {loadingEdit ? (
            <HStack justify="center" py={8}>
              <Loader size={24} />
            </HStack>
          ) : (
          <VStack spacing={3}>
            <FormControl isRequired>
              <FormLabel>Nazwa</FormLabel>
              <Input
                value={form.name}
                onChange={(e) =>
                  setForm((prev) => ({ ...prev, name: e.target.value }))
                }
                placeholder="Nazwa kontrahenta"
              />
            </FormControl>

            <FormControl>
              <FormLabel>NIP</FormLabel>
              <Input
                value={form.taxId}
                onChange={(e) =>
                  setForm((prev) => ({ ...prev, taxId: e.target.value }))
                }
                placeholder="NIP"
              />
            </FormControl>

            <FormControl>
              <FormLabel>Email</FormLabel>
              <Input
                type="email"
                value={form.email}
                onChange={(e) =>
                  setForm((prev) => ({ ...prev, email: e.target.value }))
                }
                placeholder="adres@email.pl"
              />
            </FormControl>

            <FormControl>
              <FormLabel>Telefon</FormLabel>
              <Input
                value={form.phoneNumber}
                onChange={(e) =>
                  setForm((prev) => ({ ...prev, phoneNumber: e.target.value }))
                }
                placeholder="Numer telefonu"
              />
            </FormControl>

            <HStack w="100%" spacing={3} align="flex-start">
              <FormControl>
                <FormLabel>Ulica</FormLabel>
                <Input
                  value={form.street}
                  onChange={(e) =>
                    setForm((prev) => ({ ...prev, street: e.target.value }))
                  }
                  placeholder="Ulica i numer"
                />
              </FormControl>
              <FormControl>
                <FormLabel>Miasto</FormLabel>
                <Input
                  value={form.city}
                  onChange={(e) =>
                    setForm((prev) => ({ ...prev, city: e.target.value }))
                  }
                  placeholder="Miasto"
                />
              </FormControl>
            </HStack>

            <HStack w="100%" spacing={3} align="flex-start">
              <FormControl>
                <FormLabel>Kod pocztowy</FormLabel>
                <Input
                  value={form.postalCode}
                  onChange={(e) =>
                    setForm((prev) => ({
                      ...prev,
                      postalCode: e.target.value,
                    }))
                  }
                  placeholder="00-000"
                />
              </FormControl>
              <FormControl>
                <FormLabel>Kraj</FormLabel>
                <Input
                  value={form.country}
                  onChange={(e) =>
                    setForm((prev) => ({ ...prev, country: e.target.value }))
                  }
                  placeholder="Kraj"
                />
              </FormControl>
            </HStack>

            <FormControl>
              <FormLabel>Notatki</FormLabel>
              <Textarea
                value={form.notes}
                onChange={(e) =>
                  setForm((prev) => ({ ...prev, notes: e.target.value }))
                }
                placeholder="Opcjonalne notatki..."
                rows={3}
              />
            </FormControl>
          </VStack>
          )}
        </AppModal>

        <DeleteAlertDialog
          isOpen={deleteDialog.isOpen}
          onClose={deleteDialog.onClose}
          onConfirm={handleDelete}
          itemName={deletingContractor?.name}
          isLoading={deleteMutation.isPending}
        />
      </Box>
    </MainLayout>
  );
}
