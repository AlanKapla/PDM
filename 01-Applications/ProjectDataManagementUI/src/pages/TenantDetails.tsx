import { useEffect, useState, useRef } from "react";
import { useParams, useNavigate } from "react-router-dom";
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
  useToast,
  Badge,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  IconButton,
  Collapse,
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
} from "@chakra-ui/react";
import { Building2, ChevronDown, ChevronUp, Trash2, ArrowLeft, Edit2, Save, X, UserPlus, Shield, Power } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { getTenantDetails, updateTenant, removeTenantMember, removeTenantInvitation, inviteTenantMember, updateTenantMemberRole } from "../services/tenantService";
import type { TenantDetails as TenantDetailsType } from "../types/auth.types";
import { getInvitationStatusName, getInvitationStatusColor } from "../types/auth.types";
import { getRoleName, getRoleColor } from "../constants/roleCodes";
import { useAuth } from "../context/AuthContext";
import { tenantApi } from "../api/tenantApi";
import { roleApi, type RoleWeb } from "../api/roleApi";
import { handleApiError } from "../utils/handleApiError";

export default function TenantDetails() {
  const { tenantId } = useParams<{ tenantId: string }>();
  const navigate = useNavigate();
  const toast = useToast();
  const { user } = useAuth();

  const [tenant, setTenant] = useState<TenantDetailsType | null>(null);
  const [loading, setLoading] = useState(true);

  const [isEditingName, setIsEditingName] = useState(false);
  const [editedName, setEditedName] = useState("");
  const [updatingName, setUpdatingName] = useState(false);

  const [membersExpanded, setMembersExpanded] = useState(true);
  const [invitationsExpanded, setInvitationsExpanded] = useState(true);

  const [isInviting, setIsInviting] = useState(false);
  const [inviteEmail, setInviteEmail] = useState("");
  const [sendingInvite, setSendingInvite] = useState(false);

  const [deletingMemberId, setDeletingMemberId] = useState<string | null>(null);
  const [deletingInvitationId, setDeletingInvitationId] = useState<string | null>(null);

  const [togglingStatus, setTogglingStatus] = useState(false);

  const [editingRoleMemberId, setEditingRoleMemberId] = useState<string | null>(null);
  const [editedRoleId, setEditedRoleId] = useState<string>("");
  const [updatingRole, setUpdatingRole] = useState(false);
  const [availableRoles, setAvailableRoles] = useState<RoleWeb[]>([]);

  const { isOpen: isMemberDeleteOpen, onOpen: onMemberDeleteOpen, onClose: onMemberDeleteClose } = useDisclosure();
  const { isOpen: isInvitationDeleteOpen, onOpen: onInvitationDeleteOpen, onClose: onInvitationDeleteClose } = useDisclosure();
  const { isOpen: isToggleStatusOpen, onOpen: onToggleStatusOpen, onClose: onToggleStatusClose } = useDisclosure();
  const cancelRef = useRef<HTMLButtonElement>(null);

  const cardBg = useColorModeValue("white", "gray.800");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const borderColor = useColorModeValue("gray.200", "gray.600");
  const labelColor = useColorModeValue("gray.700", "gray.300");
  const inviteBg = useColorModeValue("blue.50", "blue.900");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

  useEffect(() => {
    async function loadTenant() {
      if (!tenantId) {
        navigate("/tenants/managed");
        return;
      }

      try {
        const tenantData = await getTenantDetails(tenantId);

        if (!tenantData) {
          toast({
            title: "Błąd",
            description: "Nie znaleziono organizacji",
            status: "error",
            duration: 3000,
          });
          navigate("/tenants/managed");
          return;
        }

        setTenant(tenantData);
        setEditedName(tenantData.name);
      } catch (error) {
        console.error("Błąd ładowania tenanta:", error);
        toast({
          title: "Błąd",
          description: "Nie udało się załadować danych organizacji",
          status: "error",
          duration: 3000,
        });
      } finally {
        setLoading(false);
      }
    }

    async function loadRoles() {
      try {
        const roles = await roleApi.getAvailableRoles('tenant');
        setAvailableRoles(roles);
      } catch (error) {
        console.error('Failed to load roles:', error);
      }
    }

    loadTenant();
    loadRoles();
  }, [tenantId, navigate, toast]);

  const handleUpdateName = async () => {
    if (!editedName.trim()) {
      toast({
        title: "Błąd walidacji",
        description: "Nazwa organizacji nie może być pusta",
        status: "error",
        duration: 3000,
      });
      return;
    }

    if (!tenantId) return;

    setUpdatingName(true);
    try {
      const updated = await updateTenant(tenantId, editedName);

      if (updated) {
        // Aktualizuj tylko nazwę w istniejącym stanie (nie zastępuj całego obiektu)
        setTenant(prev => prev ? { ...prev, name: updated.name } : null);
        setIsEditingName(false);
        toast({
          title: "Zaktualizowano",
          description: "Nazwa organizacji została zmieniona",
          status: "success",
          duration: 3000,
        });
      } else {
        toast({
          title: "Błąd",
          description: "Nie udało się zaktualizować nazwy",
          status: "error",
          duration: 3000,
        });
      }
    } catch (error) {
      console.error("Błąd aktualizacji:", error);
      toast({
        title: "Błąd",
        description: "Wystąpił problem z połączeniem",
        status: "error",
        duration: 3000,
      });
    } finally {
      setUpdatingName(false);
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

    if (!tenantId) return;

    setSendingInvite(true);
    try {
      const success = await inviteTenantMember(tenantId, inviteEmail);

      if (success) {
        // Odśwież dane tenanta aby pobrać nowe zaproszenie
        const updated = await getTenantDetails(tenantId);
        if (updated) {
          setTenant(updated);
        }

        setIsInviting(false);
        setInviteEmail("");
        toast({
          title: "Zaproszenie wysłane",
          description: `Zaproszenie zostało wysłane na adres ${inviteEmail}`,
          status: "success",
          duration: 5000,
        });
      } else {
        toast({
          title: "Błąd",
          description: "Nie udało się wysłać zaproszenia",
          status: "error",
          duration: 3000,
        });
      }
    } catch (error) {
      console.error("Błąd zapraszania:", error);
      toast({
        title: "Błąd",
        description: "Wystąpił problem z połączeniem",
        status: "error",
        duration: 3000,
      });
    } finally {
      setSendingInvite(false);
    }
  };

  const handleRemoveMember = async () => {
    if (!tenantId || !deletingMemberId) return;

    try {
      const success = await removeTenantMember(tenantId, deletingMemberId);

      if (success) {
        setTenant((prev) =>
          prev
            ? {
              ...prev,
              members: prev.members.filter((m) => m.userId !== deletingMemberId),
            }
            : null
        );
        toast({
          title: "Usunięto członka",
          description: "Członek został usunięty z organizacji",
          status: "success",
          duration: 3000,
        });
      } else {
        toast({
          title: "Błąd",
          description: "Nie udało się usunąć członka",
          status: "error",
          duration: 3000,
        });
      }
    } catch (error) {
      console.error("Błąd usuwania członka:", error);
      toast({
        title: "Błąd",
        description: "Wystąpił problem z połączeniem",
        status: "error",
        duration: 3000,
      });
    } finally {
      setDeletingMemberId(null);
      onMemberDeleteClose();
    }
  };

  const handleRemoveInvitation = async () => {
    if (!tenantId || !deletingInvitationId) return;

    try {
      const success = await removeTenantInvitation(tenantId, deletingInvitationId);

      if (success) {
        setTenant((prev) =>
          prev
            ? {
              ...prev,
              invitations: prev.invitations.filter((i) => i.invitationId !== deletingInvitationId),
            }
            : null
        );
        toast({
          title: "Usunięto zaproszenie",
          description: "Zaproszenie zostało anulowane",
          status: "success",
          duration: 3000,
        });
      } else {
        toast({
          title: "Błąd",
          description: "Nie udało się usunąć zaproszenia",
          status: "error",
          duration: 3000,
        });
      }
    } catch (error) {
      console.error("Błąd usuwania zaproszenia:", error);
      toast({
        title: "Błąd",
        description: "Wystąpił problem z połączeniem",
        status: "error",
        duration: 3000,
      });
    } finally {
      setDeletingInvitationId(null);
      onInvitationDeleteClose();
    }
  };

  const handleUpdateMemberRole = async (userId: string) => {
    if (!tenantId) return;

    if (user?.id === userId) {
      toast({
        title: "Błąd",
        description: "Nie możesz zmienić własnej roli",
        status: "error",
        duration: 3000,
      });
      return;
    }

    setUpdatingRole(true);
    try {
      const success = await updateTenantMemberRole(tenantId, userId, editedRoleId);

      if (success) {
        setEditingRoleMemberId(null);
        toast({
          title: "Zaktualizowano rolę",
          description: "Rola członka została zmieniona",
          status: "success",
          duration: 3000,
          isClosable: true,
        });
        // Przeładuj dane
        window.location.reload();
      } else {
        toast({
          title: "Błąd",
          description: "Nie udało się zmienić roli",
          status: "error",
          duration: 3000,
        });
      }
    } catch (error) {
      console.error("Błąd zmiany roli:", error);
      toast({
        title: "Błąd",
        description: "Wystąpił problem z połączeniem",
        status: "error",
        duration: 3000,
      });
    } finally {
      setUpdatingRole(false);
    }
  };

  const handleToggleTenantStatus = async () => {
    if (!tenant || !tenantId) return;

    const newStatus = !tenant.isActive;
    setTogglingStatus(true);

    try {
      await tenantApi.toggleTenantStatus(tenantId, newStatus);

      toast({
        title: newStatus ? "Organizacja aktywowana" : "Organizacja zdezaktywowana",
        description: newStatus
          ? "Organizacja została pomyślnie aktywowana"
          : "Organizacja została pomyślnie zdezaktywowana",
        status: "success",
        duration: 4000,
      });

      onToggleStatusClose();

      // Odśwież dane tenanta
      const updated = await getTenantDetails(tenantId);
      if (updated) {
        setTenant(updated);
      }
    } catch (error) {
      console.error("Błąd podczas toggle tenant status:", error);
      const { title, description } = handleApiError(error);
      toast({
        title,
        description,
        status: "error",
        duration: 5000,
      });
    } finally {
      setTogglingStatus(false);
    }
  };

  if (loading) {
    return (
      <MainLayout>
        <VStack spacing={4} align="center" justify="center" minH="50vh">
          <Spinner size="xl" color="blue.500" />
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
      <Box bg={pageBg} minH="100vh" p={{ base: 3, sm: 4, md: 10 }}>
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
            bg={cardBg}
            p={{ base: 3, md: 6 }}
            rounded="lg"
            shadow="md"
            borderWidth="1px"
            borderColor={borderColor}
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
                      <Tooltip
                        label={
                          tenant.isActive
                            ? "Dezaktywuj organizację"
                            : "Aktywuj organizację"
                        }
                      >
                        <Button
                          size={{ base: "xs", md: "sm" }}
                          variant="ghost"
                          leftIcon={<Power size={16} />}
                          colorScheme={tenant.isActive ? "red" : "green"}
                          onClick={onToggleStatusOpen}
                          fontSize={{ base: "10px", md: "sm" }}
                        >
                          {tenant.isActive ? "Dezaktywuj" : "Aktywuj"}
                        </Button>
                      </Tooltip>
                      <Button
                        size={{ base: "xs", md: "sm" }}
                        variant="ghost"
                        leftIcon={<Edit2 size={16} />}
                        onClick={() => setIsEditingName(true)}
                        fontSize={{ base: "10px", md: "sm" }}
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
                      colorScheme="blue"
                      leftIcon={<Save size={16} />}
                      onClick={handleUpdateName}
                      isLoading={updatingName}
                      flex={1}
                    >
                      Zapisz
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
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
                    <Badge colorScheme={tenant.isActive ? "green" : "gray"}>
                      {tenant.isActive ? "Aktywna" : "Nieaktywna"}
                    </Badge>
                  </HStack>
                  <Text fontSize="sm" color="gray.500">
                    Utworzono:{" "}
                    {new Date(tenant.createdAt).toLocaleDateString("pl-PL")}
                  </Text>
                  <Badge colorScheme={getRoleColor(tenant.roleCode)}>
                    {getRoleName(tenant.roleCode)}
                  </Badge>
                </VStack>
              )}
            </VStack>
          </Box>

          {/* Członkowie + zaproszenia + dialogi */}
          <Box
            bg={cardBg}
            rounded="lg"
            shadow="md"
            borderWidth="1px"
            borderColor={borderColor}
          >
            {/* Członkowie */}
            <HStack
              p={{ base: 3, md: 4 }}
              justify="space-between"
              flexWrap={{ base: "wrap", md: "nowrap" }}
              gap={{ base: 2, md: 0 }}
            >
              <HStack
                spacing={3}
                cursor="pointer"
                onClick={() => setMembersExpanded(!membersExpanded)}
                flex={1}
              >
                <Heading size="md">Członkowie</Heading>
                <Badge>{tenant.members.length}</Badge>
              </HStack>
              <HStack spacing={{ base: 1, md: 2 }} flexWrap="wrap">
                {!isInviting && (
                  <Button
                    size={{ base: "xs", md: "sm" }}
                    leftIcon={<UserPlus size={14} />}
                    colorScheme="blue"
                    variant="ghost"
                    onClick={() => setIsInviting(true)}
                    fontSize={{ base: "10px", md: "sm" }}
                  >
                    Zaproś
                  </Button>
                )}
                <IconButton
                  aria-label="Rozwiń/Zwiń"
                  icon={
                    membersExpanded ? (
                      <ChevronUp size={20} />
                    ) : (
                      <ChevronDown size={20} />
                    )
                  }
                  variant="ghost"
                  size="sm"
                  onClick={() => setMembersExpanded(!membersExpanded)}
                />
              </HStack>
            </HStack>

            <Collapse in={membersExpanded} animateOpacity>
              <Box borderTop="1px solid" borderColor={borderColor}>
                {isInviting && (
                  <Box
                    p={{ base: 3, md: 4 }}
                    bg={inviteBg}
                    borderBottom="1px solid"
                    borderColor={borderColor}
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
                          bg={cardBg}
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
                          colorScheme="blue"
                          onClick={handleInviteMember}
                          isLoading={sendingInvite}
                          flex={{ base: "1 1 100%", md: "1" }}
                          fontSize={{ base: "xs", md: "sm" }}
                        >
                          Wyślij zaproszenie
                        </Button>
                        <Button
                          size={{ base: "sm", md: "md" }}
                          variant="outline"
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
                )}

                {tenant.members.length === 0 ? (
                  <Box p={{ base: 3, md: 4 }}>
                    <Text color="gray.500" textAlign="center">
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
                                colorScheme={getRoleColor(member.roleCode)}
                                fontSize={{ base: "8px", md: "xs" }}
                              >
                                {getRoleName(member.roleCode)}
                              </Badge>
                            </Td>
                            <Td
                              fontSize={{ base: "xs", md: "sm" }}
                              display={{ base: "none", md: "table-cell" }}
                            >
                              {new Date(
                                member.joinedAt
                              ).toLocaleDateString("pl-PL")}
                            </Td>
                            <Td>
                              {editingRoleMemberId === member.userId ? (
                                <HStack spacing={2}>
                                  <Select
                                    size="sm"
                                    value={editedRoleId}
                                    onChange={(e) =>
                                      setEditedRoleId(e.target.value)
                                    }
                                    isDisabled={updatingRole}
                                    width="150px"
                                  >
                                    {availableRoles.map((role) => (
                                      <option key={role.id} value={role.id}>
                                        {role.name}
                                      </option>
                                    ))}
                                  </Select>
                                  <IconButton
                                    aria-label="Zapisz rolę"
                                    icon={<Save size={14} />}
                                    size="sm"
                                    colorScheme="green"
                                    onClick={() =>
                                      handleUpdateMemberRole(member.userId)
                                    }
                                    isLoading={updatingRole}
                                  />
                                  <IconButton
                                    aria-label="Anuluj"
                                    icon={<X size={14} />}
                                    size="sm"
                                    variant="ghost"
                                    onClick={() =>
                                      setEditingRoleMemberId(null)
                                    }
                                    isDisabled={updatingRole}
                                  />
                                </HStack>
                              ) : (
                                <HStack spacing={2}>
                                  {user?.id !== member.userId && (
                                    <Tooltip label="Zmień rolę">
                                      <IconButton
                                        aria-label="Edytuj rolę"
                                        icon={<Shield size={14} />}
                                        size="sm"
                                        variant="ghost"
                                        onClick={() => {
                                          const role = availableRoles.find(
                                            (r) => r.code === member.roleCode
                                          );
                                          setEditingRoleMemberId(member.userId);
                                          setEditedRoleId(
                                            role?.id || member.roleCode
                                          );
                                        }}
                                      />
                                    </Tooltip>
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
                              )}
                            </Td>
                          </Tr>
                        ))}
                      </Tbody>
                    </Table>
                  </Box>
                )}
              </Box>
            </Collapse>

            {/* Zaproszenia */}
            <Box
              bg={cardBg}
              rounded="lg"
              shadow="md"
              borderTopWidth="1px"
              borderColor={borderColor}
            >
              <HStack
                p={{ base: 3, md: 4 }}
                justify="space-between"
                cursor="pointer"
                onClick={() => setInvitationsExpanded(!invitationsExpanded)}
                _hover={{ bg: hoverBg }}
              >
                <HStack spacing={3}>
                  <Heading size="md">Zaproszenia</Heading>
                  <Badge>{tenant.invitations.length}</Badge>
                </HStack>
                <IconButton
                  aria-label="Rozwiń/Zwiń"
                  icon={
                    invitationsExpanded ? (
                      <ChevronUp size={20} />
                    ) : (
                      <ChevronDown size={20} />
                    )
                  }
                  variant="ghost"
                  size="sm"
                />
              </HStack>

              <Collapse in={invitationsExpanded} animateOpacity>
                <Box borderTop="1px solid" borderColor={borderColor}>
                  {tenant.invitations.length === 0 ? (
                    <Box p={{ base: 3, md: 4 }}>
                      <Text color="gray.500" textAlign="center">
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
                                {new Date(
                                  invitation.createdAt
                                ).toLocaleDateString("pl-PL")}
                              </Td>
                              <Td
                                fontSize={{ base: "xs", md: "sm" }}
                                display={{ base: "none", md: "table-cell" }}
                              >
                                {invitation.expiresAt
                                  ? new Date(
                                    invitation.expiresAt
                                  ).toLocaleDateString("pl-PL")
                                  : "Brak"}
                              </Td>
                              <Td fontSize={{ base: "xs", md: "sm" }}>
                                <Badge
                                  colorScheme={getInvitationStatusColor(
                                    invitation.status
                                  )}
                                  fontSize={{ base: "8px", md: "xs" }}
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
                </Box>
              </Collapse>
            </Box>

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

            {/* Dialog potwierdzenia zmiany statusu organizacji */}
            <AlertDialog
              isOpen={isToggleStatusOpen}
              leastDestructiveRef={cancelRef}
              onClose={onToggleStatusClose}
            >
              <AlertDialogOverlay>
                <AlertDialogContent
                  maxW={{ base: "90vw", md: "600px" }}
                  mx={{ base: 4, md: 0 }}
                >
                  <AlertDialogHeader fontSize="lg" fontWeight="bold">
                    {tenant?.isActive
                      ? "Dezaktywuj organizację"
                      : "Aktywuj organizację"}
                  </AlertDialogHeader>

                  <AlertDialogBody>
                    <VStack align="flex-start" spacing={4}>
                      <Text>
                        Czy na pewno chcesz{" "}
                        {tenant?.isActive ? "zdezaktywować" : "aktywować"}{" "}
                        organizację{" "}
                        <Text
                          as="span"
                          fontWeight="bold"
                          color="blue.500"
                        >
                          {tenant?.name}
                        </Text>
                        ?
                      </Text>
                      {tenant?.isActive ? (
                        <Box
                          p={4}
                          bg={useColorModeValue("orange.50", "orange.900")}
                          borderRadius="md"
                          borderWidth="1px"
                          borderColor={useColorModeValue(
                            "orange.200",
                            "orange.700"
                          )}
                          width="100%"
                        >
                          <VStack align="flex-start" spacing={3}>
                            <HStack spacing={2}>
                              <Icon as={Power} color="orange.500" />
                              <Text
                                fontWeight="bold"
                                color="orange.600"
                                fontSize="sm"
                              >
                                ⚠️ Ważne informacje:
                              </Text>
                            </HStack>
                            <Text fontSize="sm">
                              • Zdezaktywowana organizacja będzie{" "}
                              <Text as="span" fontWeight="bold">
                                niedostępna
                              </Text>{" "}
                              dla wszystkich użytkowników
                            </Text>
                            <Text fontSize="sm">
                              • Nie będzie można edytować ani zapraszać nowych
                              członków
                            </Text>
                            <Text fontSize="sm">
                              • Wszystkie dane organizacji zostaną zachowane
                            </Text>
                            <Text fontSize="sm">
                              • Możesz ponownie aktywować organizację w każdej
                              chwili
                            </Text>
                            <Text
                              fontSize="sm"
                              fontWeight="medium"
                              color="orange.700"
                              mt={2}
                            >
                              Operacja nie usuwa organizacji, tylko zawiesza jej
                              działanie.
                            </Text>
                          </VStack>
                        </Box>
                      ) : (
                        <Box
                          p={4}
                          bg={useColorModeValue("green.50", "green.900")}
                          borderRadius="md"
                          borderWidth="1px"
                          borderColor={useColorModeValue(
                            "green.200",
                            "green.700"
                          )}
                          width="100%"
                        >
                          <VStack align="flex-start" spacing={3}>
                            <HStack spacing={2}>
                              <Icon as={Power} color="green.500" />
                              <Text
                                fontWeight="bold"
                                color="green.600"
                                fontSize="sm"
                              >
                                ℹ️ Informacje:
                              </Text>
                            </HStack>
                            <Text fontSize="sm">
                              • Organizacja stanie się{" "}
                              <Text as="span" fontWeight="bold">
                                dostępna
                              </Text>{" "}
                              dla wszystkich członków
                            </Text>
                            <Text fontSize="sm">
                              • Będzie można edytować i zapraszać nowych członków
                            </Text>
                            <Text fontSize="sm">
                              • Wszystkie dane organizacji są zachowane
                            </Text>
                          </VStack>
                        </Box>
                      )}
                    </VStack>
                  </AlertDialogBody>

                  <AlertDialogFooter>
                    <Button
                      ref={cancelRef}
                      onClick={onToggleStatusClose}
                      isDisabled={togglingStatus}
                    >
                      Anuluj
                    </Button>
                    <Button
                      colorScheme={tenant?.isActive ? "red" : "green"}
                      onClick={handleToggleTenantStatus}
                      isLoading={togglingStatus}
                      loadingText={
                        tenant?.isActive ? "Dezaktywuję..." : "Aktywuję..."
                      }
                      ml={3}
                    >
                      {tenant?.isActive
                        ? "Dezaktywuj organizację"
                        : "Aktywuj organizację"}
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
