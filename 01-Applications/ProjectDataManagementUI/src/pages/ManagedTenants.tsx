import { useEffect, useState, Fragment } from "react";
import {
  Box,
  Heading,
  Text,
  Spinner,
  VStack,
  Button,
  Input,
  FormControl,
  FormLabel,
  HStack,
  useToast,
  Badge,
  Stack,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  IconButton,
  Collapse,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
  useDisclosure,
  Flex,
} from "@chakra-ui/react";
import {
  Building2,
  Plus,
  Edit2,
  UserPlus,
  ChevronDown,
  ChevronUp,
  Trash2,
} from "lucide-react";

import MainLayout from "../layout/MainLayout";
import {
  getUserTenants,
  createTenant,
  updateTenant,
  inviteTenantMember,
  removeTenantMember,
} from "../services/tenantService";
import type { TenantDetails } from "../types/auth.types";
import {
  TenantRole,
  getTenantRoleName,
  getTenantRoleColor,
  InvitationStatus,
  getInvitationStatusName,
  getInvitationStatusColor,
} from "../types/auth.types";
import { PageHeader } from "../components/PageHeader";

export default function ManagedTenants() {
  const [tenants, setTenants] = useState<TenantDetails[]>([]);
  const [loading, setLoading] = useState(true);

  const [isCreatingTenant, setIsCreatingTenant] = useState(false);
  const [newTenantName, setNewTenantName] = useState("");
  const [creatingTenant, setCreatingTenant] = useState(false);

  const [editingTenantId, setEditingTenantId] = useState<string | null>(null);
  const [editTenantName, setEditTenantName] = useState("");
  const [updatingTenant, setUpdatingTenant] = useState(false);

  const [invitingTenantId, setInvitingTenantId] = useState<string | null>(null);
  const [inviteEmail, setInviteEmail] = useState("");
  const [sendingInvite, setSendingInvite] = useState(false);

  const [removingMemberId, setRemovingMemberId] = useState<string | null>(null);
  const [memberToRemove, setMemberToRemove] = useState<{
    tenantId: string;
    userId: string;
    name: string;
  } | null>(null);

  const [expandedTenants, setExpandedTenants] = useState<Set<string>>(
    () => new Set()
  );

  const {
    isOpen: isRemoveModalOpen,
    onOpen: onRemoveModalOpen,
    onClose: onRemoveModalClose,
  } = useDisclosure();

  const toast = useToast();

  // tylko organizacje, gdzie jesteś Admin
  const managedTenants = tenants.filter(
    (t: TenantDetails) => t.role === TenantRole.Admin
  );

  useEffect(() => {
    async function load() {
      try {
        const tenantsData = await getUserTenants();
        setTenants(tenantsData);
      } catch (error) {
        console.error("Błąd ładowania danych:", error);
      } finally {
        setLoading(false);
      }
    }
    load();
  }, []);

  const toggleTenantExpand = (tenantId: string) => {
    setExpandedTenants((prev) => {
      const next = new Set(prev);
      if (next.has(tenantId)) next.delete(tenantId);
      else next.add(tenantId);
      return next;
    });
  };

  const handleCreateTenant = async () => {
    if (!newTenantName.trim()) {
      toast({
        title: "Błąd walidacji",
        description: "Nazwa organizacji nie może być pusta",
        status: "error",
        duration: 3000,
      });
      return;
    }

    setCreatingTenant(true);
    try {
      const newTenant = await createTenant(newTenantName);
      if (newTenant) {
        setTenants((prev) => [...prev, newTenant]);
        setNewTenantName("");
        setIsCreatingTenant(false);
        toast({
          title: "Organizacja utworzona",
          description: `Organizacja "${newTenant.name}" została utworzona`,
          status: "success",
          duration: 3000,
        });
      } else {
        toast({
          title: "Błąd tworzenia organizacji",
          status: "error",
          duration: 3000,
        });
      }
    } catch (error) {
      console.error("Błąd tworzenia tenanta:", error);
      toast({
        title: "Błąd",
        description: "Wystąpił problem z połączeniem",
        status: "error",
        duration: 3000,
      });
    } finally {
      setCreatingTenant(false);
    }
  };

  const handleUpdateTenant = async () => {
    if (!editTenantName.trim() || !editingTenantId) {
      toast({
        title: "Błąd walidacji",
        description: "Nazwa organizacji nie może być pusta",
        status: "error",
        duration: 3000,
      });
      return;
    }

    setUpdatingTenant(true);
    try {
      const updatedTenant = await updateTenant(editingTenantId, editTenantName);
      if (updatedTenant) {
        setTenants((prev) =>
          prev.map((t) => (t.id === editingTenantId ? updatedTenant : t))
        );
        setEditingTenantId(null);
        setEditTenantName("");
        toast({
          title: "Organizacja zaktualizowana",
          status: "success",
          duration: 3000,
        });
      } else {
        toast({
          title: "Błąd aktualizacji organizacji",
          status: "error",
          duration: 3000,
        });
      }
    } catch (error) {
      console.error("Błąd aktualizacji tenanta:", error);
      toast({
        title: "Błąd",
        description: "Wystąpił problem z połączeniem",
        status: "error",
        duration: 3000,
      });
    } finally {
      setUpdatingTenant(false);
    }
  };

  const openRemoveMemberModal = (
    tenantId: string,
    userId: string,
    memberName: string
  ) => {
    setMemberToRemove({ tenantId, userId, name: memberName });
    onRemoveModalOpen();
  };

  const handleRemoveMember = async () => {
    if (!memberToRemove) return;

    const { tenantId, userId, name } = memberToRemove;
    setRemovingMemberId(userId);
    onRemoveModalClose();

    try {
      const success = await removeTenantMember(tenantId, userId);
      if (success) {
        setTenants((prevTenants) =>
          prevTenants.map((tenant) =>
            tenant.id === tenantId
              ? {
                  ...tenant,
                  members: tenant.members.filter(
                    (m) => m.userId !== userId
                  ),
                }
              : tenant
          )
        );

        toast({
          title: "Członek usunięty",
          description: `${name} nie ma już dostępu do tej organizacji`,
          status: "success",
          duration: 4000,
        });
      } else {
        toast({
          title: "Nie udało się usunąć członka",
          status: "error",
          duration: 4000,
        });
      }
    } catch (error) {
      console.error("Błąd usuwania członka:", error);
      toast({
        title: "Błąd połączenia",
        status: "error",
        duration: 4000,
      });
    } finally {
      setRemovingMemberId(null);
      setMemberToRemove(null);
    }
  };

  const handleInviteMember = async () => {
    if (!inviteEmail.trim()) {
      toast({
        title: "Błąd walidacji",
        description: "Adres email nie może być pusty",
        status: "error",
        duration: 3000,
      });
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(inviteEmail)) {
      toast({
        title: "Błąd walidacji",
        description: "Podaj prawidłowy adres email",
        status: "error",
        duration: 3000,
      });
      return;
    }

    if (!invitingTenantId) return;

    setSendingInvite(true);
    try {
      const success = await inviteTenantMember(invitingTenantId, inviteEmail);
      if (success) {
        setInvitingTenantId(null);
        setInviteEmail("");
        toast({
          title: "Zaproszenie wysłane",
          description: `Zaproszenie zostało wysłane na adres ${inviteEmail}`,
          status: "success",
          duration: 4000,
        });
      } else {
        toast({
          title: "Błąd wysyłania zaproszenia",
          status: "error",
          duration: 3000,
        });
      }
    } catch (error) {
      console.error("Błąd wysyłania zaproszenia:", error);
      toast({
        title: "Błąd połączenia",
        status: "error",
        duration: 3000,
      });
    } finally {
      setSendingInvite(false);
    }
  };

  if (loading) {
    return (
      <MainLayout>
        <VStack spacing={4} align="center" justify="center" minH="60vh">
          <Spinner size="xl" color="gray.300" />
          <Text color="gray.400">Ładowanie organizacji...</Text>
        </VStack>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box bg="#0f0f0f" minH="100vh" p={{ base: 4, md: 8 }}>
        <Box maxW="1200px" mx="auto">
          {/* HEADER */}
          <PageHeader
            title="Organizacje, którymi zarządzasz"
            breadcrumb={["Organizacje", "Zarządzasz"]}
            icon={Building2}
          />

          {/* TWORZENIE NOWEJ ORGANIZACJI */}
          <Box
            mt={6}
            mb={4}
            bg="#131313"
            border="1px solid #1f1f1f"
            rounded="md"
            p={5}
          >
            {!isCreatingTenant ? (
              <Flex justify="space-between" align="center">
                <Box>
                  <Text color="gray.200" fontWeight="medium">
                    Utwórz nową organizację
                  </Text>
                  <Text color="gray.500" fontSize="sm">
                    Organizacje służą do grupowania projektów i współpracy z zespołem.
                  </Text>
                </Box>
                <Button
                  leftIcon={<Plus size={16} />}
                  colorScheme="blue"
                  variant="solid"
                  onClick={() => setIsCreatingTenant(true)}
                >
                  Nowa organizacja
                </Button>
              </Flex>
            ) : (
              <VStack align="stretch" spacing={4}>
                <FormControl>
                  <FormLabel color="gray.300" fontSize="sm">
                    Nazwa organizacji
                  </FormLabel>
                  <Input
                    value={newTenantName}
                    onChange={(e) => setNewTenantName(e.target.value)}
                    placeholder="Wprowadź nazwę organizacji"
                    bg="#0f0f0f"
                    border="1px solid #2a2a2a"
                    _placeholder={{ color: "gray.600" }}
                    onKeyDown={(e) => {
                      if (e.key === "Enter" && !creatingTenant) {
                        e.preventDefault();
                        handleCreateTenant();
                      }
                    }}
                  />
                </FormControl>
                <HStack spacing={3}>
                  <Button
                    colorScheme="blue"
                    onClick={handleCreateTenant}
                    isLoading={creatingTenant}
                  >
                    Utwórz
                  </Button>
                  <Button
                    variant="ghost"
                    onClick={() => {
                      setIsCreatingTenant(false);
                      setNewTenantName("");
                    }}
                    isDisabled={creatingTenant}
                  >
                    Anuluj
                  </Button>
                </HStack>
              </VStack>
            )}
          </Box>

          {/* LISTA ORGANIZACJI */}
          <Box mt={6}>
            <Heading size="sm" color="gray.300" mb={3}>
              Twoje organizacje
            </Heading>

            {managedTenants.length === 0 ? (
              <Box
                bg="#131313"
                border="1px solid #1f1f1f"
                rounded="md"
                p={8}
                textAlign="center"
              >
                <Text color="gray.400">
                  Nie zarządzasz jeszcze żadną organizacją. Utwórz nową powyżej.
                </Text>
              </Box>
            ) : (
              <Box
                bg="#131313"
                border="1px solid #1f1f1f"
                rounded="md"
                overflow="hidden"
              >
                <Table variant="simple" size="sm">
                  <Thead>
                    <Tr bg="#141414">
                      <Th color="gray.400" fontSize="xs">
                        Organizacja
                      </Th>
                      <Th color="gray.400" fontSize="xs">
                        Utworzono
                      </Th>
                      <Th color="gray.400" fontSize="xs" isNumeric>
                        Członkowie
                      </Th>
                      <Th color="gray.400" fontSize="xs" isNumeric>
                        Zaproszenia
                      </Th>
                      <Th color="gray.400" fontSize="xs">
                        Akcje
                      </Th>
                    </Tr>
                  </Thead>
                  <Tbody>
                    {managedTenants.map((tenant) => {
                      const isExpanded = expandedTenants.has(tenant.id);
                      const pendingInvites = tenant.invitations.filter(
                        (inv) => inv.status === InvitationStatus.Pending
                      );

                      return (
                        <Fragment key={tenant.id}>
                          {/* Wiersz główny organizacji */}
                          <Tr
                            _hover={{ bg: "#191919" }}
                            bg={isExpanded ? "#181818" : "transparent"}
                          >
                            <Td>
                              {editingTenantId === tenant.id ? (
                                <Input
                                  size="sm"
                                  value={editTenantName}
                                  onChange={(e) =>
                                    setEditTenantName(e.target.value)
                                  }
                                  bg="#0f0f0f"
                                  border="1px solid #2a2a2a"
                                  onKeyDown={(e) => {
                                    if (
                                      e.key === "Enter" &&
                                      !updatingTenant
                                    ) {
                                      e.preventDefault();
                                      handleUpdateTenant();
                                    }
                                  }}
                                />
                              ) : (
                                <Text color="gray.200" fontWeight="medium">
                                  {tenant.name}
                                </Text>
                              )}
                            </Td>
                            <Td>
                              <Text fontSize="xs" color="gray.500">
                                {new Date(
                                  tenant.createdAt
                                ).toLocaleDateString("pl-PL")}
                              </Text>
                            </Td>
                            <Td isNumeric>
                              <Text fontSize="sm" color="gray.300">
                                {tenant.members.length}
                              </Text>
                            </Td>
                            <Td isNumeric>
                              <Text fontSize="sm" color="gray.300">
                                {pendingInvites.length}
                              </Text>
                            </Td>
                            <Td>
                              <HStack justify="flex-end" spacing={1}>
                                {editingTenantId === tenant.id ? (
                                  <>
                                    <Button
                                      size="xs"
                                      colorScheme="blue"
                                      isLoading={updatingTenant}
                                      onClick={handleUpdateTenant}
                                    >
                                      Zapisz
                                    </Button>
                                    <Button
                                      size="xs"
                                      variant="ghost"
                                      onClick={() => {
                                        setEditingTenantId(null);
                                        setEditTenantName("");
                                      }}
                                      isDisabled={updatingTenant}
                                    >
                                      Anuluj
                                    </Button>
                                  </>
                                ) : invitingTenantId === tenant.id ? (
                                  <>
                                    <Input
                                      size="xs"
                                      type="email"
                                      placeholder="email@firma.pl"
                                      value={inviteEmail}
                                      onChange={(e) =>
                                        setInviteEmail(e.target.value)
                                      }
                                      bg="#0f0f0f"
                                      border="1px solid #2a2a2a"
                                      onKeyDown={(e) => {
                                        if (
                                          e.key === "Enter" &&
                                          !sendingInvite
                                        ) {
                                          e.preventDefault();
                                          handleInviteMember();
                                        }
                                      }}
                                    />
                                    <Button
                                      size="xs"
                                      colorScheme="blue"
                                      isLoading={sendingInvite}
                                      onClick={handleInviteMember}
                                    >
                                      Wyślij
                                    </Button>
                                    <Button
                                      size="xs"
                                      variant="ghost"
                                      onClick={() => {
                                        setInvitingTenantId(null);
                                        setInviteEmail("");
                                      }}
                                      isDisabled={sendingInvite}
                                    >
                                      Anuluj
                                    </Button>
                                  </>
                                ) : (
                                  <>
                                    <Button
                                      size="xs"
                                      variant="ghost"
                                      leftIcon={<UserPlus size={14} />}
                                      onClick={() => {
                                        setInvitingTenantId(tenant.id);
                                        setInviteEmail("");
                                      }}
                                    >
                                      Zaproś
                                    </Button>
                                    <Button
                                      size="xs"
                                      variant="ghost"
                                      leftIcon={<Edit2 size={14} />}
                                      onClick={() => {
                                        setEditingTenantId(tenant.id);
                                        setEditTenantName(tenant.name);
                                      }}
                                    >
                                      Edytuj
                                    </Button>
                                    <IconButton
                                      aria-label="Pokaż szczegóły"
                                      icon={
                                        isExpanded ? (
                                          <ChevronUp size={18} />
                                        ) : (
                                          <ChevronDown size={18} />
                                        )
                                      }
                                      size="xs"
                                      variant="ghost"
                                      onClick={() =>
                                        toggleTenantExpand(tenant.id)
                                      }
                                    />
                                  </>
                                )}
                              </HStack>
                            </Td>
                          </Tr>

                          {/* Wiersz rozsuwany ze szczegółami */}
                          <Tr>
                            <Td colSpan={5} p={0}>
                              <Collapse in={isExpanded} animateOpacity>
                                <Box
                                  borderTop="1px solid #1f1f1f"
                                  bg="#101010"
                                  px={5}
                                  py={4}
                                >
                                  <Stack
                                    direction={{ base: "column", md: "row" }}
                                    spacing={6}
                                    align="flex-start"
                                  >
                                    {/* Członkowie */}
                                    <Box flex={1}>
                                      <Text
                                        fontSize="sm"
                                        fontWeight="medium"
                                        color="gray.200"
                                        mb={2}
                                      >
                                        Członkowie
                                      </Text>

                                      {tenant.members.length === 0 ? (
                                        <Text
                                          fontSize="xs"
                                          color="gray.500"
                                        >
                                          Brak członków w tej organizacji.
                                        </Text>
                                      ) : (
                                        <VStack
                                          align="stretch"
                                          spacing={2}
                                        >
                                          {tenant.members.map((member) => (
                                            <Flex
                                              key={member.userId}
                                              justify="space-between"
                                              align="center"
                                              bg="#151515"
                                              rounded="md"
                                              px={3}
                                              py={2}
                                            >
                                              <Box>
                                                <Text
                                                  fontSize="sm"
                                                  color="gray.200"
                                                >
                                                  {member.firstName}{" "}
                                                  {member.lastName}
                                                </Text>
                                                <Text
                                                  fontSize="xs"
                                                  color="gray.500"
                                                >
                                                  {member.email}
                                                </Text>
                                                <Text
                                                  fontSize="xs"
                                                  color="gray.500"
                                                >
                                                  Dołączył:{" "}
                                                  {new Date(
                                                    member.joinedAt
                                                  ).toLocaleDateString(
                                                    "pl-PL"
                                                  )}
                                                </Text>
                                              </Box>
                                              <HStack spacing={2}>
                                                <Badge
                                                  colorScheme={getTenantRoleColor(
                                                    member.role
                                                  )}
                                                  fontSize="xs"
                                                >
                                                  {getTenantRoleName(
                                                    member.role
                                                  )}
                                                </Badge>
                                                <IconButton
                                                  aria-label="Usuń członka"
                                                  icon={
                                                    <Trash2 size={14} />
                                                  }
                                                  size="xs"
                                                  variant="ghost"
                                                  colorScheme="red"
                                                  onClick={() =>
                                                    openRemoveMemberModal(
                                                      tenant.id,
                                                      member.userId,
                                                      `${member.firstName} ${member.lastName}`
                                                    )
                                                  }
                                                  isLoading={
                                                    removingMemberId ===
                                                    member.userId
                                                  }
                                                  isDisabled={
                                                    removingMemberId !== null
                                                  }
                                                />
                                              </HStack>
                                            </Flex>
                                          ))}
                                        </VStack>
                                      )}
                                    </Box>

                                    {/* Zaproszenia Pending */}
                                    <Box flex={1}>
                                      <Text
                                        fontSize="sm"
                                        fontWeight="medium"
                                        color="gray.200"
                                        mb={2}
                                      >
                                        Zaproszenia (oczekujące)
                                      </Text>

                                      {pendingInvites.length === 0 ? (
                                        <Text
                                          fontSize="xs"
                                          color="gray.500"
                                        >
                                          Brak aktywnych zaproszeń.
                                        </Text>
                                      ) : (
                                        <VStack
                                          align="stretch"
                                          spacing={2}
                                        >
                                          {pendingInvites.map((invitation) => (
                                            <Box
                                              key={invitation.invitationId}
                                              bg="#151515"
                                              rounded="md"
                                              px={3}
                                              py={2}
                                            >
                                              <Text
                                                fontSize="sm"
                                                color="gray.200"
                                              >
                                                {invitation.email}
                                              </Text>
                                              <HStack spacing={2} mt={1}>
                                                <Badge
                                                  colorScheme={getInvitationStatusColor(
                                                    invitation.status
                                                  )}
                                                  fontSize="xs"
                                                >
                                                  {getInvitationStatusName(
                                                    invitation.status
                                                  )}
                                                </Badge>
                                                <Text
                                                  fontSize="xs"
                                                  color="gray.500"
                                                >
                                                  Wysłano:{" "}
                                                  {new Date(
                                                    invitation.createdAt
                                                  ).toLocaleDateString(
                                                    "pl-PL"
                                                  )}
                                                </Text>
                                                {invitation.expiresAt && (
                                                  <Text
                                                    fontSize="xs"
                                                    color="orange.400"
                                                  >
                                                    Wygasa:{" "}
                                                    {new Date(
                                                      invitation.expiresAt
                                                    ).toLocaleDateString(
                                                      "pl-PL"
                                                    )}
                                                  </Text>
                                                )}
                                              </HStack>
                                            </Box>
                                          ))}
                                        </VStack>
                                      )}
                                    </Box>
                                  </Stack>
                                </Box>
                              </Collapse>
                            </Td>
                          </Tr>
                        </Fragment>
                      );
                    })}
                  </Tbody>
                </Table>
              </Box>
            )}
          </Box>
        </Box>
      </Box>

      {/* MODAL POTWIERDZENIA USUNIĘCIA CZŁONKA */}
      <Modal
        isOpen={isRemoveModalOpen}
        onClose={onRemoveModalClose}
        isCentered
      >
        <ModalOverlay />
        <ModalContent bg="#131313" border="1px solid #1f1f1f">
          <ModalHeader color="gray.100">
            Usuń członka z organizacji
          </ModalHeader>
          <ModalCloseButton />
          <ModalBody>
            <VStack align="flex-start" spacing={3}>
              <Text color="gray.200">
                Czy na pewno chcesz usunąć{" "}
                <Text as="span" fontWeight="bold">
                  {memberToRemove?.name}
                </Text>{" "}
                z tej organizacji?
              </Text>
              <Text fontSize="sm" color="gray.500">
                Ta osoba straci dostęp do wszystkich projektów i danych w tej
                organizacji.
              </Text>
            </VStack>
          </ModalBody>
          <ModalFooter>
            <Button variant="ghost" mr={3} onClick={onRemoveModalClose}>
              Anuluj
            </Button>
            <Button colorScheme="red" onClick={handleRemoveMember}>
              Usuń członka
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    </MainLayout>
  );
}
