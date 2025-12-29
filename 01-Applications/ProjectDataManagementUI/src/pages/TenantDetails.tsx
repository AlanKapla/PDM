import { useEffect, useState } from "react";
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
} from "@chakra-ui/react";
import { Building2, ChevronDown, ChevronUp, Trash2, ArrowLeft, Edit2, Save, X, UserPlus } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { getUserTenants, updateTenant, removeTenantMember, removeTenantInvitation, inviteTenantMember } from "../services/tenantService";
import type { TenantDetails as TenantDetailsType } from "../types/auth.types";
import { getTenantRoleName, getTenantRoleColor, getInvitationStatusName, getInvitationStatusColor } from "../types/auth.types";
import { useRef } from "react";

export default function TenantDetails() {
  const { tenantId } = useParams<{ tenantId: string }>();
  const navigate = useNavigate();
  const toast = useToast();

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
  
  const { isOpen: isMemberDeleteOpen, onOpen: onMemberDeleteOpen, onClose: onMemberDeleteClose } = useDisclosure();
  const { isOpen: isInvitationDeleteOpen, onOpen: onInvitationDeleteOpen, onClose: onInvitationDeleteClose } = useDisclosure();
  const cancelRef = useRef<HTMLButtonElement>(null);

  const cardBg = useColorModeValue("white", "gray.800");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const borderColor = useColorModeValue("gray.200", "gray.600");
  const labelColor = useColorModeValue("gray.700", "gray.300");

  useEffect(() => {
    async function loadTenant() {
      if (!tenantId) {
        navigate("/tenants/managed");
        return;
      }

      try {
        const tenants = await getUserTenants();
        const found = tenants.find((t) => t.id === tenantId);

        if (!found) {
          toast({
            title: "Błąd",
            description: "Nie znaleziono organizacji",
            status: "error",
            duration: 3000,
          });
          navigate("/tenants/managed");
          return;
        }

        setTenant(found);
        setEditedName(found.name);
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

    loadTenant();
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
        setTenant(updated);
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
        const tenants = await getUserTenants();
        const updated = tenants.find((t) => t.id === tenantId);
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
      <Box bg={pageBg} minH="100vh" p={{ base: 4, md: 6 }}>
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
          <Box bg={cardBg} p={6} rounded="lg" shadow="md" borderWidth="1px" borderColor={borderColor}>
            <VStack align="stretch" spacing={4}>
              <HStack justify="space-between">
                <HStack spacing={3}>
                  <Building2 size={32} />
                  <Heading size="lg">Szczegóły organizacji</Heading>
                </HStack>
                {!isEditingName && (
                  <Button
                    size="sm"
                    variant="ghost"
                    leftIcon={<Edit2 size={16} />}
                    onClick={() => setIsEditingName(true)}
                  >
                    Edytuj
                  </Button>
                )}
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
                    Utworzono: {new Date(tenant.createdAt).toLocaleDateString("pl-PL")}
                  </Text>
                  <Badge colorScheme={getTenantRoleColor(tenant.role)}>
                    {getTenantRoleName(tenant.role)}
                  </Badge>
                </VStack>
              )}
            </VStack>
          </Box>

          {/* Członkowie */}
          <Box bg={cardBg} rounded="lg" shadow="md" borderWidth="1px" borderColor={borderColor}>
            <HStack
              p={4}
              justify="space-between"
            >
              <HStack spacing={3} cursor="pointer" onClick={() => setMembersExpanded(!membersExpanded)} flex={1}>
                <Heading size="md">Członkowie</Heading>
                <Badge>{tenant.members.length}</Badge>
              </HStack>
              <HStack spacing={2}>
                {!isInviting && (
                  <Button
                    size="sm"
                    leftIcon={<UserPlus size={14} />}
                    colorScheme="blue"
                    variant="ghost"
                    onClick={() => setIsInviting(true)}
                  >
                    Zaproś
                  </Button>
                )}
                <IconButton
                  aria-label="Rozwiń/Zwiń"
                  icon={membersExpanded ? <ChevronUp size={20} /> : <ChevronDown size={20} />}
                  variant="ghost"
                  size="sm"
                  onClick={() => setMembersExpanded(!membersExpanded)}
                />
              </HStack>
            </HStack>

            <Collapse in={membersExpanded} animateOpacity>
              <Box borderTop="1px solid" borderColor={borderColor}>
                {isInviting && (
                  <Box p={4} bg={useColorModeValue("blue.50", "blue.900")} borderBottom="1px solid" borderColor={borderColor}>
                    <VStack spacing={3} align="stretch">
                      <FormControl>
                        <FormLabel fontSize="sm">Adres email osoby zapraszanej</FormLabel>
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
                      <HStack spacing={2}>
                        <Button
                          size="sm"
                          colorScheme="blue"
                          onClick={handleInviteMember}
                          isLoading={sendingInvite}
                          flex={1}
                        >
                          Wyślij zaproszenie
                        </Button>
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => {
                            setIsInviting(false);
                            setInviteEmail("");
                          }}
                          isDisabled={sendingInvite}
                          flex={1}
                        >
                          Anuluj
                        </Button>
                      </HStack>
                    </VStack>
                  </Box>
                )}
                {tenant.members.length === 0 ? (
                  <Box p={4}>
                    <Text color="gray.500" textAlign="center">
                      Brak członków w tej organizacji
                    </Text>
                  </Box>
                ) : (
                  <Table variant="simple" size="sm">
                    <Thead>
                      <Tr>
                        <Th>Imię i nazwisko</Th>
                        <Th>Email</Th>
                        <Th>Rola</Th>
                        <Th>Data dołączenia</Th>
                        <Th>Akcje</Th>
                      </Tr>
                    </Thead>
                    <Tbody>
                      {tenant.members.map((member) => (
                        <Tr key={member.userId}>
                          <Td>
                            {member.firstName} {member.lastName}
                          </Td>
                          <Td>{member.email}</Td>
                          <Td>
                            <Badge colorScheme={getTenantRoleColor(member.role)}>
                              {getTenantRoleName(member.role)}
                            </Badge>
                          </Td>
                          <Td>{new Date(member.joinedAt).toLocaleDateString("pl-PL")}</Td>
                          <Td>
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
                          </Td>
                        </Tr>
                      ))}
                    </Tbody>
                  </Table>
                )}
              </Box>
            </Collapse>
          </Box>

          {/* Zaproszenia */}
          <Box bg={cardBg} rounded="lg" shadow="md" borderWidth="1px" borderColor={borderColor}>
            <HStack
              p={4}
              justify="space-between"
              cursor="pointer"
              onClick={() => setInvitationsExpanded(!invitationsExpanded)}
              _hover={{ bg: useColorModeValue("gray.50", "gray.700") }}
            >
              <HStack spacing={3}>
                <Heading size="md">Zaproszenia</Heading>
                <Badge>{tenant.invitations.length}</Badge>
              </HStack>
              <IconButton
                aria-label="Rozwiń/Zwiń"
                icon={invitationsExpanded ? <ChevronUp size={20} /> : <ChevronDown size={20} />}
                variant="ghost"
                size="sm"
              />
            </HStack>

            <Collapse in={invitationsExpanded} animateOpacity>
              <Box borderTop="1px solid" borderColor={borderColor}>
                {tenant.invitations.length === 0 ? (
                  <Box p={4}>
                    <Text color="gray.500" textAlign="center">
                      Brak aktywnych zaproszeń
                    </Text>
                  </Box>
                ) : (
                  <Table variant="simple" size="sm">
                    <Thead>
                      <Tr>
                        <Th>Email</Th>
                        <Th>Zaproszony przez</Th>
                        <Th>Data zaproszenia</Th>
                        <Th>Wygasa</Th>
                        <Th>Status</Th>
                        <Th>Akcje</Th>
                      </Tr>
                    </Thead>
                    <Tbody>
                      {tenant.invitations.map((invitation) => (
                        <Tr key={invitation.invitationId}>
                          <Td>{invitation.email}</Td>
                          <Td>{invitation.invitedByUserName}</Td>
                          <Td>{new Date(invitation.createdAt).toLocaleDateString("pl-PL")}</Td>
                          <Td>
                            {invitation.expiresAt
                              ? new Date(invitation.expiresAt).toLocaleDateString("pl-PL")
                              : "Brak"}
                          </Td>
                          <Td>
                            <Badge colorScheme={getInvitationStatusColor(invitation.status)}>
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
                                setDeletingInvitationId(invitation.invitationId);
                                onInvitationDeleteOpen();
                              }}
                            />
                          </Td>
                        </Tr>
                      ))}
                    </Tbody>
                  </Table>
                )}
              </Box>
            </Collapse>
          </Box>
        </VStack>
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
              Czy na pewno chcesz usunąć tego członka z organizacji? Tej operacji nie można cofnąć.
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
              Czy na pewno chcesz anulować to zaproszenie? Tej operacji nie można cofnąć.
            </AlertDialogBody>

            <AlertDialogFooter>
              <Button ref={cancelRef} onClick={onInvitationDeleteClose}>
                Anuluj
              </Button>
              <Button colorScheme="red" onClick={handleRemoveInvitation} ml={3}>
                Usuń
              </Button>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialogOverlay>
      </AlertDialog>
    </MainLayout>
  );
}
