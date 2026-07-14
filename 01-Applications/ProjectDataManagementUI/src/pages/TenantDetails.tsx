import { useEffect, useState, useRef } from "react";
import { useParams, useNavigate, Link as RouterLink } from "react-router-dom";
import {
  Box,
  Heading,
  Text,
  Spinner,
  VStack,
  useColorModeValue,
  Button,
  Input,
  FormControl,
  FormLabel,
  HStack,
  Badge,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  IconButton,
  Tabs,
  TabList,
  Tab,
  TabPanels,
  TabPanel,
  AlertDialog,
  AlertDialogBody,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogContent,
  AlertDialogOverlay,
  useDisclosure,
  Select,
  Tooltip,
  Icon,
  Textarea,
} from "@chakra-ui/react";
import { Building2, Trash2, ArrowLeft, Edit2, Save, X, UserPlus, Users, Plus } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { getTenantDetails, updateTenant, removeTenantMember, removeTenantInvitation, inviteTenantMember, updateTenantMemberAdmin } from "../services/tenantService";
import type { TenantDetails as TenantDetailsType } from "../types/auth.types";
import { getInvitationStatusName, getInvitationStatusColor } from "../types/auth.types";
import { getTenantRoleName, getTenantRoleColor } from "../constants/roleCodes";
import { useAuth } from "../context/AuthContext";

import { useToastNotification } from "../hooks/useToastNotification";
import { useContractors, useCreateContractor, useUpdateContractor, useDeleteContractor } from "../hooks/queries";
import { useModal } from "../hooks/useModal";
import { useTenantPermissions } from "../hooks/useTenantPermissions";
import AppModal from "../components/ui/AppModal";
import DeleteAlertDialog from "../components/ui/DeleteAlertDialog";
import type { ContractorWeb, CreateContractorRequest } from "../types/contractor.types";
import { formatDateShort } from "../utils/formatters";

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

const emptyContractorForm: ContractorFormValues = {
  name: "",
  taxId: "",
  email: "",
  phoneNumber: "",
  street: "",
  city: "",
  postalCode: "",
  country: "",
  notes: "",
};

function ContractorsTabPanel({ tenantId }: { tenantId: string }) {
  const { canEdit } = useTenantPermissions();
  const {showSuccess, showError, showApiError } = useToastNotification();
  const { data: contractors = [], isLoading } = useContractors(tenantId || undefined);
  const createMutation = useCreateContractor(tenantId);
  const updateMutation = useUpdateContractor(tenantId);
  const deleteMutation = useDeleteContractor(tenantId);
  const formModal = useModal();
  const deleteDialog = useModal();
  const [editingContractor, setEditingContractor] = useState<ContractorWeb | null>(null);
  const [deletingContractor, setDeletingContractor] = useState<ContractorWeb | null>(null);
  const [form, setForm] = useState<ContractorFormValues>(emptyContractorForm);

  const handleOpenCreate = () => {
    setEditingContractor(null);
    setForm(emptyContractorForm);
    formModal.onOpen();
  };

  const handleOpenEdit = (c: ContractorWeb) => {
    setEditingContractor(c);
    setForm({
      name: c.name,
      taxId: c.taxId ?? "",
      email: c.email ?? "",
      phoneNumber: c.phoneNumber ?? "",
      street: c.street ?? "",
      city: c.city ?? "",
      postalCode: c.postalCode ?? "",
      country: c.country ?? "",
      notes: c.notes ?? "",
    });
    formModal.onOpen();
  };

  const handleSave = async () => {
    if (!form.name.trim()) {
      showError("Błąd", "Nazwa kontrahenta jest wymagana");
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
        await updateMutation.mutateAsync({ contractorId: editingContractor.id, data: { ...payload, id: editingContractor.id } });
        showSuccess("Sukces", "Kontrahent zaktualizowany");
      } else {
        await createMutation.mutateAsync(payload);
        showSuccess("Sukces", "Kontrahent dodany");
      }
      formModal.onClose();
    } catch {
      showError("Błąd", "Nie udało się zapisać kontrahenta");
    }
  };

  const handleDelete = async () => {
    if (!deletingContractor) return;
    try {
      await deleteMutation.mutateAsync(deletingContractor.id);
      showSuccess("Sukces", "Kontrahent usunięty");
      deleteDialog.onClose();
    } catch {
      showError("Błąd", "Nie udało się usunąć kontrahenta");
    }
  };

  const isSaving = createMutation.isPending || updateMutation.isPending;

  return (
    <>
      {canEdit && (
        <Box p={{ base: 3, md: 4 }} borderBottom="1px solid" borderColor="neutral.200">
          <Button
            size={{ base: "xs", md: "sm" }}
            leftIcon={<Plus size={14} />}
            colorScheme="primary"
            variant="ghost"
            onClick={handleOpenCreate}
            fontSize={{ base: "xs", md: "sm" }}
          >
            Dodaj kontrahenta
          </Button>
        </Box>
      )}
      {isLoading ? (
        <Box p={4} textAlign="center">
          <Spinner size="sm" color="primary.500" />
        </Box>
      ) : contractors.length === 0 ? (
        <Box p={{ base: 3, md: 4 }}>
          <Text color="neutral.500" textAlign="center">
            Brak kontrahentów w tej organizacji
          </Text>
        </Box>
      ) : (
        <Box overflowX={{ base: "auto", md: "visible" }}>
          <Table variant="simple" size={{ base: "xs", md: "sm" }}>
            <Thead>
              <Tr>
                <Th fontSize={{ base: "xs", md: "sm" }}>Nazwa</Th>
                <Th fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", md: "table-cell" }}>NIP</Th>
                <Th fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", lg: "table-cell" }}>Email</Th>
                <Th fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", md: "table-cell" }}>Miasto</Th>
                {canEdit && <Th fontSize={{ base: "xs", md: "sm" }}>Akcje</Th>}
              </Tr>
            </Thead>
            <Tbody>
              {contractors.map((c) => (
                <Tr key={c.id} cursor="pointer" _hover={{ bg: "neutral.50" }} onClick={() => handleOpenEdit(c)}>
                  <Td fontSize={{ base: "xs", md: "sm" }}>{c.name}</Td>
                  <Td fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", md: "table-cell" }}>{c.taxId ?? "—"}</Td>
                  <Td fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", lg: "table-cell" }}>{c.email ?? "—"}</Td>
                  <Td fontSize={{ base: "xs", md: "sm" }} display={{ base: "none", md: "table-cell" }}>{c.city ?? "—"}</Td>
                  {canEdit && (
                    <Td>
                      <HStack spacing={1}>
                        <Tooltip label="Usuń">
                          <IconButton
                            aria-label="Usuń kontrahenta"
                            icon={<Trash2 size={14} />}
                            size="xs"
                            variant="ghost"
                            colorScheme="red"
                            onClick={(e) => { e.stopPropagation(); setDeletingContractor(c); deleteDialog.onOpen(); }}
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
        title={editingContractor ? "Edytuj kontrahenta" : "Dodaj kontrahenta"}
        actionLabel={editingContractor ? "Zapisz" : "Dodaj"}
        actionColorScheme="green"
        onAction={handleSave}
        isActionLoading={isSaving}
        isActionDisabled={!form.name.trim()}
        desktopSize="xl"
      >
        <VStack spacing={3}>
          <FormControl isRequired>
            <FormLabel>Nazwa</FormLabel>
            <Input value={form.name} onChange={(e) => setForm((p) => ({ ...p, name: e.target.value }))} placeholder="Nazwa kontrahenta" />
          </FormControl>
          <FormControl>
            <FormLabel>NIP</FormLabel>
            <Input value={form.taxId} onChange={(e) => setForm((p) => ({ ...p, taxId: e.target.value }))} placeholder="NIP" />
          </FormControl>
          <FormControl>
            <FormLabel>Email</FormLabel>
            <Input type="email" value={form.email} onChange={(e) => setForm((p) => ({ ...p, email: e.target.value }))} placeholder="adres@email.pl" />
          </FormControl>
          <FormControl>
            <FormLabel>Telefon</FormLabel>
            <Input value={form.phoneNumber} onChange={(e) => setForm((p) => ({ ...p, phoneNumber: e.target.value }))} placeholder="Numer telefonu" />
          </FormControl>
          <HStack w="100%" spacing={3} align="flex-start">
            <FormControl>
              <FormLabel>Ulica</FormLabel>
              <Input value={form.street} onChange={(e) => setForm((p) => ({ ...p, street: e.target.value }))} placeholder="Ulica i numer" />
            </FormControl>
            <FormControl>
              <FormLabel>Miasto</FormLabel>
              <Input value={form.city} onChange={(e) => setForm((p) => ({ ...p, city: e.target.value }))} placeholder="Miasto" />
            </FormControl>
          </HStack>
          <HStack w="100%" spacing={3} align="flex-start">
            <FormControl>
              <FormLabel>Kod pocztowy</FormLabel>
              <Input value={form.postalCode} onChange={(e) => setForm((p) => ({ ...p, postalCode: e.target.value }))} placeholder="00-000" />
            </FormControl>
            <FormControl>
              <FormLabel>Kraj</FormLabel>
              <Input value={form.country} onChange={(e) => setForm((p) => ({ ...p, country: e.target.value }))} placeholder="Kraj" />
            </FormControl>
          </HStack>
          <FormControl>
            <FormLabel>Notatki</FormLabel>
            <Textarea value={form.notes} onChange={(e) => setForm((p) => ({ ...p, notes: e.target.value }))} placeholder="Opcjonalne notatki..." rows={3} />
          </FormControl>
        </VStack>
      </AppModal>

      <DeleteAlertDialog
        isOpen={deleteDialog.isOpen}
        onClose={deleteDialog.onClose}
        onConfirm={handleDelete}
        itemName={deletingContractor?.name}
        isLoading={deleteMutation.isPending}
      />
    </>
  );
}

export default function TenantDetails() {
  const { tenantId } = useParams<{ tenantId: string }>();
  const navigate = useNavigate();
  const { showSuccess, showError, showApiSuccess, showApiError } = useToastNotification();
  const { user } = useAuth();

  const [tenant, setTenant] = useState<TenantDetailsType | null>(null);
  const [loading, setLoading] = useState(true);

  const [isEditingName, setIsEditingName] = useState(false);
  const [editedName, setEditedName] = useState("");
  const [updatingName, setUpdatingName] = useState(false);

  const [isInviting, setIsInviting] = useState(false);
  const [inviteEmail, setInviteEmail] = useState("");
  const [sendingInvite, setSendingInvite] = useState(false);

  const [deletingMemberId, setDeletingMemberId] = useState<string | null>(null);
  const [deletingInvitationId, setDeletingInvitationId] = useState<string | null>(null);

  const { isOpen: isMemberDeleteOpen, onOpen: onMemberDeleteOpen, onClose: onMemberDeleteClose } = useDisclosure();
  const { isOpen: isInvitationDeleteOpen, onOpen: onInvitationDeleteOpen, onClose: onInvitationDeleteClose } = useDisclosure();
  const cancelRef = useRef<HTMLButtonElement>(null);

  const labelColor = useColorModeValue("gray.700", "gray.300");

  useEffect(() => {
    async function loadTenant() {
      if (!tenantId) {
        navigate("/tenants/managed");
        return;
      }

      try {
        const tenantData = await getTenantDetails(tenantId);
        setTenant(tenantData);
        setEditedName(tenantData.name);
      } catch (error) {
        showApiError(error);
        navigate("/tenants/managed");
      } finally {
        setLoading(false);
      }
    }

    loadTenant();
  }, [tenantId, navigate]);

  const handleUpdateName = async () => {
    if (!editedName.trim()) {
      showError("Błąd walidacji", "Nazwa organizacji nie może być pusta");
      return;
    }

    if (!tenantId) return;

    setUpdatingName(true);
    try {
      const updated = await updateTenant(tenantId, editedName);
      setTenant((prev) => (prev ? { ...prev, name: updated.name } : null));
      setIsEditingName(false);
      showApiSuccess('tenantUpdated');
    } catch (error) {
      showApiError(error);
    } finally {
      setUpdatingName(false);
    }
  };

  const handleInviteMember = async () => {
    if (!inviteEmail.trim()) {
      showError("Błąd walidacji", "Adres email nie może być pusty");
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(inviteEmail)) {
      showError("Błąd walidacji", "Podaj prawidłowy adres email");
      return;
    }

    if (!tenantId) return;

    setSendingInvite(true);
    try {
      await inviteTenantMember(tenantId, inviteEmail);
      const updated = await getTenantDetails(tenantId);
      setTenant(updated);
      setIsInviting(false);
      setInviteEmail("");
      showApiSuccess('inviteSent');
    } catch (error) {
      showApiError(error);
    } finally {
      setSendingInvite(false);
    }
  };

  const handleRemoveMember = async () => {
    if (!tenantId || !deletingMemberId) return;

    try {
      await removeTenantMember(tenantId, deletingMemberId);
      setTenant((prev) =>
        prev
          ? {
            ...prev,
            members: prev.members.filter((m) => m.userId !== deletingMemberId),
          }
          : null
      );
      showApiSuccess('memberRemoved');
    } catch (error) {
      showApiError(error);
    } finally {
      setDeletingMemberId(null);
      onMemberDeleteClose();
    }
  };

  const handleRemoveInvitation = async () => {
    if (!tenantId || !deletingInvitationId) return;

    try {
      await removeTenantInvitation(tenantId, deletingInvitationId);
      setTenant((prev) =>
        prev
          ? {
            ...prev,
            invitations: prev.invitations.filter((i) => i.invitationId !== deletingInvitationId),
          }
          : null
      );
      showApiSuccess('inviteCancelled');
    } catch (error) {
      showApiError(error);
    } finally {
      setDeletingInvitationId(null);
      onInvitationDeleteClose();
    }
  };

  const handleToggleAdmin = async (userId: string, isAdmin: boolean) => {
    if (!tenantId) return;

    if (user?.id === userId) {
      showError("Błąd", "Nie możesz zmienić własnej roli");
      return;
    }

    try {
      await updateTenantMemberAdmin(tenantId, userId, isAdmin);
      setTenant((prev) =>
        prev
          ? {
              ...prev,
              members: prev.members.map((m) =>
                m.userId === userId ? { ...m, isAdmin } : m
              ),
            }
          : null
      );
      showApiSuccess('roleUpdated');
    } catch (error) {
      showApiError(error);
    }
  };

  if (loading) {
    return (
      <MainLayout>
        <VStack spacing={4} align="center" justify="center" minH="50vh">
          <Spinner size="xl" color="primary.500" />
          <Text>Ładowanie szczegółów organizacji...</Text>
        </VStack>
      </MainLayout>
    );
  }

  if (!tenant) {
    return (
      <MainLayout>
        <VStack spacing={4} align="center" justify="center" minH="50vh">
          <Text>Nie znaleziono organizacji</Text>
          <Button onClick={() => navigate("/tenants/managed")}>Powrót do listy organizacji</Button>
        </VStack>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box bg="white" minH="100vh" p={{ base: 3, sm: 4, md: 10 }}>
        <VStack spacing={6} maxW="1200px" mx="auto" align="stretch">
          {/* Header */}
          <HStack justify="space-between">
            <HStack spacing={3}>
              <Button
                variant="ghost"
                leftIcon={<ArrowLeft size={20} />}
                onClick={() => navigate("/tenants/managed")}
              >
                Powrót
              </Button>
            </HStack>
          </HStack>

          {/* Informacje podstawowe */}
          <Box
            bg="white"
            p={{ base: 3, md: 6 }}
            rounded="lg"
            borderWidth="1px"
            borderColor="neutral.200"
          >
            <VStack align="stretch" spacing={4}>
              <HStack
                justify="space-between"
                flexWrap={{ base: "wrap", md: "nowrap" }}
                gap={{ base: 2, md: 2 }}
              >
                <HStack spacing={3} minW={0}>
                  <Building2 size={32} />
                  <Heading size="lg" noOfLines={1}>
                    Szczegóły organizacji
                  </Heading>
                </HStack>
                <HStack
                  spacing={{ base: 1, md: 2 }}
                  flexWrap="wrap"
                  justifyContent={{ base: "flex-start", md: "flex-end" }}
                >
                  {!isEditingName && (
                    <>
                      <Button
                        size={{ base: "xs", md: "sm" }}
                        variant="ghost"
                        leftIcon={<Edit2 size={16} />}
                        onClick={() => setIsEditingName(true)}
                        fontSize={{ base: "xs", md: "sm" }}
                      >
                        Edytuj
                      </Button>
                    </>
                  )}
                </HStack>
              </HStack>

              {isEditingName ? (
                <VStack spacing={3} align="stretch">
                  <FormControl>
                    <FormLabel color={labelColor}>Nazwa organizacji</FormLabel>
                    <Input
                      value={editedName}
                      onChange={(e) => setEditedName(e.target.value)}
                      onKeyPress={(e) => {
                        if (e.key === "Enter" && !updatingName) {
                          handleUpdateName();
                        }
                      }}
                    />
                  </FormControl>
                  <HStack spacing={2}>
                    <Button
                      size="sm"
                      colorScheme="primary"
                      leftIcon={<Save size={16} />}
                      onClick={handleUpdateName}
                      isLoading={updatingName}
                      flex={1}
                    >
                      Zapisz
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      colorScheme="gray"
                      leftIcon={<X size={16} />}
                      onClick={() => {
                        setIsEditingName(false);
                        setEditedName(tenant.name);
                      }}
                      isDisabled={updatingName}
                      flex={1}
                    >
                      Anuluj
                    </Button>
                  </HStack>
                </VStack>
              ) : (
                <VStack align="flex-start" spacing={2}>
                  <HStack>
                    <Text fontSize="2xl" fontWeight="bold">
                      {tenant.name}
                    </Text>
                  </HStack>
                  <Text fontSize="sm" color="neutral.500">
                    Utworzono:{" "}
                    {formatDateShort(tenant.createdAt)}
                  </Text>
                  <Badge colorScheme={getTenantRoleColor(tenant.isAdmin)}>
                    {getTenantRoleName(tenant.isAdmin)}
                  </Badge>
                </VStack>
              )}
            </VStack>
          </Box>

          {/* Tabs: Członkowie / Zaproszenia / Kontrahenci */}
          <Box
            bg="white"
            rounded="lg"
            borderWidth="1px"
            borderColor="neutral.200"
          >
            <Tabs>
              <TabList px={{ base: 3, md: 4 }} pt={2}>
                <Tab>
                  <HStack spacing={2}>
                    <Text>Członkowie</Text>
                    <Badge>{tenant.members.length}</Badge>
                  </HStack>
                </Tab>
                <Tab>
                  <HStack spacing={2}>
                    <Text>Zaproszenia</Text>
                    <Badge>{tenant.invitations.length}</Badge>
                  </HStack>
                </Tab>
                <Tab>
                  <HStack spacing={2}>
                    <Users size={14} />
                    <Text>Kontrahenci</Text>
                  </HStack>
                </Tab>
              </TabList>

              <TabPanels>
                {/* Tab: Członkowie */}
                <TabPanel p={0}>
                  {isInviting ? (
                    <Box
                      p={{ base: 3, md: 4 }}
                      bg="neutral.50"
                      borderBottom="1px solid"
                      borderColor="neutral.200"
                    >
                      <VStack spacing={3} align="stretch">
                        <FormControl>
                          <FormLabel fontSize="sm">
                            Adres email osoby zapraszanej
                          </FormLabel>
                          <Input
                            type="email"
                            value={inviteEmail}
                            onChange={(e) => setInviteEmail(e.target.value)}
                            placeholder="jan.kowalski@example.com"
                            bg="white"
                            onKeyPress={(e) => {
                              if (e.key === "Enter" && !sendingInvite) {
                                handleInviteMember();
                              }
                            }}
                          />
                        </FormControl>
                        <HStack
                          spacing={2}
                          flexWrap={{ base: "wrap", md: "nowrap" }}
                        >
                          <Button
                            size={{ base: "sm", md: "md" }}
                            colorScheme="primary"
                            onClick={handleInviteMember}
                            isLoading={sendingInvite}
                            flex={{ base: "1 1 100%", md: "1" }}
                            fontSize={{ base: "xs", md: "sm" }}
                          >
                            Wyślij zaproszenie
                          </Button>
                          <Button
                            size={{ base: "sm", md: "md" }}
                            variant="ghost"
                            colorScheme="gray"
                            onClick={() => {
                              setIsInviting(false);
                              setInviteEmail("");
                            }}
                            isDisabled={sendingInvite}
                            flex={{ base: "1 1 100%", md: "1" }}
                            fontSize={{ base: "xs", md: "sm" }}
                          >
                            Anuluj
                          </Button>
                        </HStack>
                      </VStack>
                    </Box>
                  ) : (
                    <Box
                      p={{ base: 3, md: 4 }}
                      borderBottom="1px solid"
                      borderColor="neutral.200"
                    >
                      <Button
                        size={{ base: "xs", md: "sm" }}
                        leftIcon={<UserPlus size={14} />}
                        colorScheme="primary"
                        variant="ghost"
                        onClick={() => setIsInviting(true)}
                        fontSize={{ base: "xs", md: "sm" }}
                      >
                        Zaproś
                      </Button>
                    </Box>
                  )}
                  {tenant.members.length === 0 ? (
                    <Box p={{ base: 3, md: 4 }}>
                      <Text color="neutral.500" textAlign="center">
                        Brak członków w tej organizacji
                      </Text>
                    </Box>
                  ) : (
                    <Box overflowX={{ base: "auto", md: "visible" }}>
                      <Table variant="simple" size={{ base: "xs", md: "sm" }}>
                        <Thead>
                          <Tr>
                            <Th fontSize={{ base: "xs", md: "sm" }}>
                              Imię i nazwisko
                            </Th>
                            <Th
                              fontSize={{ base: "xs", md: "sm" }}
                              display={{ base: "none", lg: "table-cell" }}
                            >
                              Email
                            </Th>
                            <Th fontSize={{ base: "xs", md: "sm" }}>Rola</Th>
                            <Th
                              fontSize={{ base: "xs", md: "sm" }}
                              display={{ base: "none", md: "table-cell" }}
                            >
                              Data dołączenia
                            </Th>
                            <Th fontSize={{ base: "xs", md: "sm" }}>Akcje</Th>
                          </Tr>
                        </Thead>
                        <Tbody>
                          {tenant.members.map((member) => (
                            <Tr key={member.userId}>
                              <Td fontSize={{ base: "xs", md: "sm" }}>
                                {member.firstName} {member.lastName}
                                {user?.id === member.userId && (
                                  <Badge ml={2} colorScheme="green" fontSize="xs">
                                    Ty
                                  </Badge>
                                )}
                              </Td>
                              <Td
                                fontSize={{ base: "xs", md: "sm" }}
                                display={{ base: "none", lg: "table-cell" }}
                              >
                                {member.email}
                              </Td>
                              <Td fontSize={{ base: "xs", md: "sm" }}>
                                <Badge
                                  colorScheme={getTenantRoleColor(member.isAdmin)}
                                  fontSize={{ base: "2xs", md: "xs" }}
                                >
                                  {getTenantRoleName(member.isAdmin)}
                                </Badge>
                              </Td>
                              <Td
                                fontSize={{ base: "xs", md: "sm" }}
                                display={{ base: "none", md: "table-cell" }}
                              >
                                {formatDateShort(member.joinedAt)}
                              </Td>
                              <Td>
                                <HStack spacing={2}>
                                  {user?.id !== member.userId && (
                                    <Select
                                      size="sm"
                                      value={member.isAdmin ? "admin" : "member"}
                                      onChange={(e) => handleToggleAdmin(member.userId, e.target.value === "admin")}
                                      width="130px"
                                    >
                                      <option value="member">Członek</option>
                                      <option value="admin">Administrator</option>
                                    </Select>
                                  )}
                                  {user?.id !== member.userId && (
                                    <Tooltip label="Usuń członka">
                                      <IconButton
                                        aria-label="Usuń członka"
                                        icon={<Trash2 size={16} />}
                                        size="sm"
                                        colorScheme="red"
                                        variant="ghost"
                                        onClick={() => {
                                          setDeletingMemberId(member.userId);
                                          onMemberDeleteOpen();
                                        }}
                                      />
                                    </Tooltip>
                                  )}
                                </HStack>
                              </Td>
                            </Tr>
                          ))}
                        </Tbody>
                      </Table>
                    </Box>
                  )}
                </TabPanel>

                {/* Tab: Zaproszenia */}
                <TabPanel p={0}>
                  {isInviting ? (
                    <Box
                      p={{ base: 3, md: 4 }}
                      bg="neutral.50"
                      borderBottom="1px solid"
                      borderColor="neutral.200"
                    >
                      <VStack spacing={3} align="stretch">
                        <FormControl>
                          <FormLabel fontSize="sm">
                            Adres email osoby zapraszanej
                          </FormLabel>
                          <Input
                            type="email"
                            value={inviteEmail}
                            onChange={(e) => setInviteEmail(e.target.value)}
                            placeholder="jan.kowalski@example.com"
                            bg="white"
                            onKeyPress={(e) => {
                              if (e.key === "Enter" && !sendingInvite) {
                                handleInviteMember();
                              }
                            }}
                          />
                        </FormControl>
                        <HStack
                          spacing={2}
                          flexWrap={{ base: "wrap", md: "nowrap" }}
                        >
                          <Button
                            size={{ base: "sm", md: "md" }}
                            colorScheme="primary"
                            onClick={handleInviteMember}
                            isLoading={sendingInvite}
                            flex={{ base: "1 1 100%", md: "1" }}
                            fontSize={{ base: "xs", md: "sm" }}
                          >
                            Wyślij zaproszenie
                          </Button>
                          <Button
                            size={{ base: "sm", md: "md" }}
                            variant="ghost"
                            colorScheme="gray"
                            onClick={() => {
                              setIsInviting(false);
                              setInviteEmail("");
                            }}
                            isDisabled={sendingInvite}
                            flex={{ base: "1 1 100%", md: "1" }}
                            fontSize={{ base: "xs", md: "sm" }}
                          >
                            Anuluj
                          </Button>
                        </HStack>
                      </VStack>
                    </Box>
                  ) : (
                    <Box
                      p={{ base: 3, md: 4 }}
                      borderBottom="1px solid"
                      borderColor="neutral.200"
                    >
                      <Button
                        size={{ base: "xs", md: "sm" }}
                        leftIcon={<UserPlus size={14} />}
                        colorScheme="primary"
                        variant="ghost"
                        onClick={() => setIsInviting(true)}
                        fontSize={{ base: "xs", md: "sm" }}
                      >
                        Zaproś
                      </Button>
                    </Box>
                  )}
                  {tenant.invitations.length === 0 ? (
                    <Box p={{ base: 3, md: 4 }}>
                      <Text color="neutral.500" textAlign="center">
                        Brak aktywnych zaproszeń
                      </Text>
                    </Box>
                  ) : (
                    <Box overflowX={{ base: "auto", md: "visible" }}>
                      <Table variant="simple" size={{ base: "xs", md: "sm" }}>
                        <Thead>
                          <Tr>
                            <Th fontSize={{ base: "xs", md: "sm" }}>Email</Th>
                            <Th
                              fontSize={{ base: "xs", md: "sm" }}
                              display={{ base: "none", lg: "table-cell" }}
                            >
                              Zaproszony przez
                            </Th>
                            <Th
                              fontSize={{ base: "xs", md: "sm" }}
                              display={{ base: "none", md: "table-cell" }}
                            >
                              Data zaproszenia
                            </Th>
                            <Th
                              fontSize={{ base: "xs", md: "sm" }}
                              display={{ base: "none", md: "table-cell" }}
                            >
                              Wygasa
                            </Th>
                            <Th
                              fontSize={{ base: "xs", md: "sm" }}
                              display={{ base: "none", md: "table-cell" }}
                            >
                              Projekt
                            </Th>
                            <Th fontSize={{ base: "xs", md: "sm" }}>Status</Th>
                            <Th fontSize={{ base: "xs", md: "sm" }}>Akcje</Th>
                          </Tr>
                        </Thead>
                        <Tbody>
                          {tenant.invitations.map((invitation) => (
                            <Tr key={invitation.invitationId}>
                              <Td
                                fontSize={{ base: "xs", md: "sm" }}
                                whiteSpace="nowrap"
                              >
                                {invitation.email}
                              </Td>
                              <Td
                                fontSize={{ base: "xs", md: "sm" }}
                                display={{ base: "none", lg: "table-cell" }}
                              >
                                {invitation.invitedByUserName}
                              </Td>
                              <Td
                                fontSize={{ base: "xs", md: "sm" }}
                                display={{ base: "none", md: "table-cell" }}
                              >
                                {formatDateShort(invitation.createdAt)}
                              </Td>
                              <Td
                                fontSize={{ base: "xs", md: "sm" }}
                                display={{ base: "none", md: "table-cell" }}
                              >
                                {invitation.expiresAt
                                  ? formatDateShort(invitation.expiresAt)
                                  : "Brak"}
                              </Td>
                              <Td
                                fontSize={{ base: "xs", md: "sm" }}
                                display={{ base: "none", md: "table-cell" }}
                              >
                                {invitation.projectName ?? "—"}
                              </Td>
                              <Td fontSize={{ base: "xs", md: "sm" }}>
                                <Badge
                                  colorScheme={getInvitationStatusColor(
                                    invitation.status
                                  )}
                                  fontSize={{ base: "2xs", md: "xs" }}
                                >
                                  {getInvitationStatusName(invitation.status)}
                                </Badge>
                              </Td>
                              <Td>
                                <IconButton
                                  aria-label="Usuń zaproszenie"
                                  icon={<Trash2 size={16} />}
                                  size="sm"
                                  colorScheme="red"
                                  variant="ghost"
                                  onClick={() => {
                                    setDeletingInvitationId(
                                      invitation.invitationId
                                    );
                                    onInvitationDeleteOpen();
                                  }}
                                />
                              </Td>
                            </Tr>
                          ))}
                        </Tbody>
                      </Table>
                    </Box>
                  )}
                </TabPanel>

                {/* Tab: Kontrahenci */}
                <TabPanel p={0}>
                  <ContractorsTabPanel tenantId={tenantId ?? ""} />
                </TabPanel>
              </TabPanels>
            </Tabs>
          </Box>

          {/* Dialogi */}
          <Box>
            {/* Dialog potwierdzenia usunięcia członka */}
            <AlertDialog
              isOpen={isMemberDeleteOpen}
              leastDestructiveRef={cancelRef}
              onClose={onMemberDeleteClose}
            >
              <AlertDialogOverlay>
                <AlertDialogContent>
                  <AlertDialogHeader fontSize="lg" fontWeight="bold">
                    Usuń członka
                  </AlertDialogHeader>

                  <AlertDialogBody>
                    Czy na pewno chcesz usunąć tego członka z organizacji? Tej
                    operacji nie można cofnąć.
                  </AlertDialogBody>

                  <AlertDialogFooter>
                    <Button ref={cancelRef} onClick={onMemberDeleteClose}>
                      Anuluj
                    </Button>
                    <Button colorScheme="red" onClick={handleRemoveMember} ml={3}>
                      Usuń
                    </Button>
                  </AlertDialogFooter>
                </AlertDialogContent>
              </AlertDialogOverlay>
            </AlertDialog>

            {/* Dialog potwierdzenia usunięcia zaproszenia */}
            <AlertDialog
              isOpen={isInvitationDeleteOpen}
              leastDestructiveRef={cancelRef}
              onClose={onInvitationDeleteClose}
            >
              <AlertDialogOverlay>
                <AlertDialogContent>
                  <AlertDialogHeader fontSize="lg" fontWeight="bold">
                    Usuń zaproszenie
                  </AlertDialogHeader>

                  <AlertDialogBody>
                    Czy na pewno chcesz anulować to zaproszenie? Tej operacji nie
                    można cofnąć.
                  </AlertDialogBody>

                  <AlertDialogFooter>
                    <Button ref={cancelRef} onClick={onInvitationDeleteClose}>
                      Anuluj
                    </Button>
                    <Button
                      colorScheme="red"
                      onClick={handleRemoveInvitation}
                      ml={3}
                    >
                      Usuń
                    </Button>
                  </AlertDialogFooter>
                </AlertDialogContent>
              </AlertDialogOverlay>
            </AlertDialog>
          </Box>
        </VStack>
      </Box>
    </MainLayout>
  );
}
