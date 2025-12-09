import { useEffect, useState } from "react";
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
  Tooltip,
  Icon,
} from "@chakra-ui/react";
import { Building2, Plus, Edit2, UserPlus, ChevronDown, ChevronUp, Trash2, Power } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { getUserTenants, createTenant, updateTenant, inviteTenantMember, removeTenantMember } from "../services/tenantService";
import { tenantApi } from "../api/tenantApi";
import type { TenantDetails } from "../types/auth.types";
import { TenantRole, getTenantRoleName, getTenantRoleColor, InvitationStatus, getInvitationStatusName, getInvitationStatusColor } from "../types/auth.types";

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
  const [memberToRemove, setMemberToRemove] = useState<{ tenantId: string; userId: string; name: string } | null>(null);
  
  const [expandedTenants, setExpandedTenants] = useState<Set<string>>(new Set());
  
  const [tenantToToggle, setTenantToToggle] = useState<TenantDetails | null>(null);
  const [togglingStatus, setTogglingStatus] = useState(false);
  
  const { isOpen: isRemoveModalOpen, onOpen: onRemoveModalOpen, onClose: onRemoveModalClose } = useDisclosure();
  const { isOpen: isToggleStatusModalOpen, onOpen: onToggleStatusModalOpen, onClose: onToggleStatusModalClose } = useDisclosure();
  
  const toast = useToast();

  const cardBg = useColorModeValue("white", "gray.800");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const labelColor = useColorModeValue("gray.700", "gray.300");
  const borderColor = useColorModeValue("gray.200", "gray.600");

  // Tylko organizacje zarządzane
  const managedTenants = tenants.filter(t => t.role === TenantRole.Admin);

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
    setExpandedTenants(prev => {
      const newSet = new Set(prev);
      if (newSet.has(tenantId)) {
        newSet.delete(tenantId);
      } else {
        newSet.add(tenantId);
      }
      return newSet;
    });
  };

  const handleCreateTenant = async () => {
    if (!newTenantName.trim()) {
      toast({
        title: "Błąd walidacji",
        description: "Nazwa organizacji nie może być pusta",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
      return;
    }

    setCreatingTenant(true);
    try {
      const newTenant = await createTenant(newTenantName);
      
      if (newTenant) {
        setTenants([...tenants, newTenant]);
        setNewTenantName("");
        setIsCreatingTenant(false);
        toast({
          title: "Organizacja utworzona",
          description: `Organizacja "${newTenant.name}" została pomyślnie utworzona`,
          status: "success",
          duration: 3000,
          isClosable: true,
        });
      } else {
        toast({
          title: "Błąd tworzenia organizacji",
          description: "Nie udało się utworzyć nowej organizacji",
          status: "error",
          duration: 3000,
          isClosable: true,
        });
      }
    } catch (error) {
      console.error("Błąd tworzenia tenanta:", error);
      toast({
        title: "Błąd",
        description: "Wystąpił problem z połączeniem",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
    } finally {
      setCreatingTenant(false);
    }
  };

  const handleUpdateTenant = async () => {
    if (!editTenantName.trim()) {
      toast({
        title: "Błąd walidacji",
        description: "Nazwa organizacji nie może być pusta",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
      return;
    }

    if (!editingTenantId) return;

    setUpdatingTenant(true);
    try {
      const updatedTenant = await updateTenant(editingTenantId, editTenantName);
      
      if (updatedTenant) {
        setTenants(tenants.map(t => t.id === editingTenantId ? updatedTenant : t));
        setEditingTenantId(null);
        setEditTenantName("");
        toast({
          title: "Organizacja zaktualizowana",
          description: `Organizacja "${updatedTenant.name}" została pomyślnie zaktualizowana`,
          status: "success",
          duration: 3000,
          isClosable: true,
        });
      } else {
        toast({
          title: "Błąd aktualizacji organizacji",
          description: "Nie udało się zaktualizować organizacji",
          status: "error",
          duration: 3000,
          isClosable: true,
        });
      }
    } catch (error) {
      console.error("Błąd aktualizacji tenanta:", error);
      toast({
        title: "Błąd",
        description: "Wystąpił problem z połączeniem",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
    } finally {
      setUpdatingTenant(false);
    }
  };

  const openRemoveMemberModal = (tenantId: string, userId: string, memberName: string) => {
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
        // Aktualizacja lokalnej listy tenantów
        setTenants(prevTenants =>
          prevTenants.map(tenant =>
            tenant.id === tenantId
              ? {
                  ...tenant,
                  members: tenant.members.filter(m => m.userId !== userId),
                }
              : tenant
          )
        );
        
        toast({
          title: "✅ Członek usunięty pomyślnie",
          description: `${name} nie ma już dostępu do tej organizacji`,
          status: "success",
          duration: 4000,
          isClosable: true,
        });
      } else {
        toast({
          title: "Nie udało się usunąć członka",
          description: "Spróbuj ponownie lub skontaktuj się z administratorem",
          status: "error",
          duration: 5000,
          isClosable: true,
        });
      }
    } catch (error) {
      console.error("Błąd usuwania członka:", error);
      toast({
        title: "Wystąpił błąd połączenia",
        description: "Sprawdź połączenie internetowe i spróbuj ponownie",
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setRemovingMemberId(null);
      setMemberToRemove(null);
    }
  };

  const openToggleStatusModal = (tenant: TenantDetails) => {
    setTenantToToggle(tenant);
    onToggleStatusModalOpen();
  };

  const handleToggleTenantStatus = async () => {
    if (!tenantToToggle) return;

    const newStatus = !tenantToToggle.isActive;
    setTogglingStatus(true);
    
    try {
      const response = await tenantApi.toggleTenantStatus(tenantToToggle.id, newStatus);
      
      if (response.ok) {
        toast({
          title: newStatus ? "Organizacja aktywowana" : "Organizacja zdezaktywowana",
          description: newStatus 
            ? "Organizacja została pomyślnie aktywowana" 
            : "Organizacja została pomyślnie zdezaktywowana",
          status: "success",
          duration: 4000,
        });
        
        onToggleStatusModalClose();
        setTenantToToggle(null);
        
        // Odśwież listę tenantów
        const tenantsData = await getUserTenants();
        setTenants(tenantsData);
      } else {
        const errorText = await response.text();
        toast({
          title: "Błąd",
          description: errorText || `Nie udało się ${newStatus ? 'aktywować' : 'zdezaktywować'} organizacji`,
          status: "error",
          duration: 5000,
        });
      }
    } catch (error) {
      console.error("Błąd podczas toggle tenant status:", error);
      toast({
        title: "Błąd",
        description: `Wystąpił błąd podczas ${newStatus ? 'aktywacji' : 'dezaktywacji'} organizacji`,
        status: "error",
        duration: 5000,
      });
    } finally {
      setTogglingStatus(false);
    }
  };

  const handleInviteMember = async () => {
    if (!inviteEmail.trim()) {
      toast({
        title: "Błąd walidacji",
        description: "Adres email nie może być pusty",
        status: "error",
        duration: 3000,
        isClosable: true,
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
        isClosable: true,
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
          duration: 5000,
          isClosable: true,
        });
      } else {
        toast({
          title: "Błąd wysyłania zaproszenia",
          description: "Nie udało się wysłać zaproszenia",
          status: "error",
          duration: 3000,
          isClosable: true,
        });
      }
    } catch (error) {
      console.error("Błąd wysyłania zaproszenia:", error);
      toast({
        title: "Błąd",
        description: "Wystąpił problem z połączeniem",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
    } finally {
      setSendingInvite(false);
    }
  };

  if (loading) {
    return (
      <MainLayout>
        <VStack spacing={4} align="center" justify="center" minH="50vh">
          <Spinner size="xl" color="blue.500" />
          <Text>Ładowanie organizacji...</Text>
        </VStack>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box bg={pageBg} minH="100vh" p={{ base: 4, md: 6 }}>
        <VStack spacing={8} maxW="1200px" mx="auto" align="stretch">
          {/* Header */}
          <Stack direction={{ base: "column", md: "row" }} justify="space-between" align={{ base: "stretch", md: "center" }} spacing={4}>
            <HStack spacing={3}>
              <Building2 size={32} />
              <Heading size={{ base: "md", md: "lg" }}>Organizacje, którymi zarządzasz</Heading>
            </HStack>
            <Button
              leftIcon={<Plus size={20} />}
              colorScheme="blue"
              onClick={() => setIsCreatingTenant(true)}
              isDisabled={isCreatingTenant}
              width={{ base: "100%", md: "auto" }}
            >
              Nowa organizacja
            </Button>
          </Stack>

          {/* Formularz tworzenia nowej organizacji */}
          {isCreatingTenant && (
            <Box bg={cardBg} p={6} rounded="lg" shadow="md" borderWidth="1px" borderColor={borderColor}>
              <VStack spacing={4} align="stretch">
                <Heading size="md">Utwórz nową organizację</Heading>
                <FormControl>
                  <FormLabel color={labelColor}>Nazwa organizacji</FormLabel>
                  <Input
                    value={newTenantName}
                    onChange={(e) => setNewTenantName(e.target.value)}
                    placeholder="Wprowadź nazwę organizacji"
                    onKeyPress={(e) => {
                      if (e.key === "Enter" && !creatingTenant) {
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
                    flex={1}
                  >
                    Utwórz
                  </Button>
                  <Button
                    variant="outline"
                    onClick={() => {
                      setIsCreatingTenant(false);
                      setNewTenantName("");
                    }}
                    isDisabled={creatingTenant}
                    flex={1}
                  >
                    Anuluj
                  </Button>
                </HStack>
              </VStack>
            </Box>
          )}

          {/* Lista organizacji zarządzanych */}
          <Box>
            <Heading size="md" mb={4}>Twoje organizacje</Heading>
            {managedTenants.length === 0 ? (
              <Box bg={cardBg} p={6} rounded="lg" shadow="md" borderWidth="1px" borderColor={borderColor}>
                <Text color="gray.500" textAlign="center">
                  Nie zarządzasz jeszcze żadną organizacją. Utwórz nową!
                </Text>
              </Box>
            ) : (
              <Stack spacing={4}>
                {managedTenants.map((tenant) => (
                  <Box
                    key={tenant.id}
                    bg={cardBg}
                    rounded="lg"
                    shadow="md"
                    borderWidth="1px"
                    borderColor={borderColor}
                    overflow="hidden"
                  >
                    {/* Header organizacji */}
                    <Box p={4}>
                      {editingTenantId === tenant.id ? (
                        <VStack spacing={3} align="stretch">
                          <FormControl>
                            <Input
                              value={editTenantName}
                              onChange={(e) => setEditTenantName(e.target.value)}
                              placeholder="Nazwa organizacji"
                              onKeyPress={(e) => {
                                if (e.key === "Enter" && !updatingTenant) {
                                  handleUpdateTenant();
                                }
                              }}
                            />
                          </FormControl>
                          <HStack spacing={2}>
                            <Button
                              size="sm"
                              colorScheme="blue"
                              onClick={handleUpdateTenant}
                              isLoading={updatingTenant}
                              flex={1}
                            >
                              Zapisz
                            </Button>
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => {
                                setEditingTenantId(null);
                                setEditTenantName("");
                              }}
                              isDisabled={updatingTenant}
                              flex={1}
                            >
                              Anuluj
                            </Button>
                          </HStack>
                        </VStack>
                      ) : invitingTenantId === tenant.id ? (
                        <VStack spacing={3} align="stretch">
                          <FormControl>
                            <FormLabel fontSize="sm">Adres email osoby zapraszanej</FormLabel>
                            <Input
                              type="email"
                              value={inviteEmail}
                              onChange={(e) => setInviteEmail(e.target.value)}
                              placeholder="jan.kowalski@example.com"
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
                                setInvitingTenantId(null);
                                setInviteEmail("");
                              }}
                              isDisabled={sendingInvite}
                              flex={1}
                            >
                              Anuluj
                            </Button>
                          </HStack>
                        </VStack>
                      ) : (
                        <VStack align="stretch" spacing={3}>
                          <Stack direction={{ base: "column", md: "row" }} justify="space-between" align={{ base: "flex-start", md: "center" }} spacing={3}>
                            <VStack align="flex-start" spacing={1} flex={1}>
                              <HStack spacing={2}>
                                <Text fontWeight="bold" fontSize={{ base: "md", md: "lg" }}>{tenant.name}</Text>
                                <Badge colorScheme={tenant.isActive ? "green" : "gray"} fontSize="xs">
                                  {tenant.isActive ? "Aktywna" : "Nieaktywna"}
                                </Badge>
                                <Tooltip label={tenant.isActive ? "Dezaktywuj organizację" : "Aktywuj organizację"}>
                                  <IconButton
                                    aria-label={tenant.isActive ? "Dezaktywuj organizację" : "Aktywuj organizację"}
                                    icon={<Power size={16} />}
                                    size="xs"
                                    colorScheme={tenant.isActive ? "red" : "green"}
                                    variant="ghost"
                                    onClick={() => openToggleStatusModal(tenant)}
                                  />
                                </Tooltip>
                              </HStack>
                              <Text fontSize="xs" color="gray.500">
                                Utworzono: {new Date(tenant.createdAt).toLocaleDateString('pl-PL')}
                              </Text>
                            </VStack>
                            <HStack spacing={2} flexWrap="wrap">
                              <Button
                                size="sm"
                                variant="ghost"
                                leftIcon={<UserPlus size={14} />}
                                onClick={() => {
                                  setInvitingTenantId(tenant.id);
                                  setInviteEmail("");
                                }}
                                isDisabled={!tenant.isActive}
                              >
                                Zaproś
                              </Button>
                              <Button
                                size="sm"
                                variant="ghost"
                                leftIcon={<Edit2 size={14} />}
                                onClick={() => {
                                  setEditingTenantId(tenant.id);
                                  setEditTenantName(tenant.name);
                                }}
                                isDisabled={!tenant.isActive}
                              >
                                Edytuj
                              </Button>
                              <IconButton
                                aria-label="Pokaż członków"
                                icon={expandedTenants.has(tenant.id) ? <ChevronUp size={20} /> : <ChevronDown size={20} />}
                                size="sm"
                                variant="ghost"
                                onClick={() => toggleTenantExpand(tenant.id)}
                              />
                            </HStack>
                          </Stack>
                        </VStack>
                      )}
                    </Box>

                    {/* Lista członków i zaproszeń */}
                    <Collapse in={expandedTenants.has(tenant.id)} animateOpacity>
                      <Box borderTop="1px solid" borderColor={borderColor} overflowX="auto">
                        {tenant.members.length === 0 && tenant.invitations.filter(inv => inv.status === InvitationStatus.Pending).length === 0 ? (
                          <Box p={4}>
                            <Text color="gray.500" textAlign="center">
                              Brak członków i aktywnych zaproszeń w tej organizacji
                            </Text>
                          </Box>
                        ) : (
                          <Table variant="simple" size="sm">
                            <Thead>
                              <Tr>
                                <Th>Imię i nazwisko</Th>
                                <Th>Email</Th>
                                <Th>Rola / Status</Th>
                                <Th>Data</Th>
                                <Th>Akcje</Th>
                              </Tr>
                            </Thead>
                            <Tbody>
                              {/* Członkowie */}
                              {tenant.members.map((member) => (
                                <Tr key={member.userId}>
                                  <Td>{member.firstName} {member.lastName}</Td>
                                  <Td>{member.email}</Td>
                                  <Td>
                                    <Badge colorScheme={getTenantRoleColor(member.role)}>
                                      {getTenantRoleName(member.role)}
                                    </Badge>
                                  </Td>
                                  <Td>
                                    <Text fontSize="xs" color="gray.500">
                                      Dołączył: {new Date(member.joinedAt).toLocaleDateString('pl-PL')}
                                    </Text>
                                  </Td>
                                  <Td>
                                    <IconButton
                                      aria-label="Usuń członka"
                                      icon={<Trash2 size={16} />}
                                      size="sm"
                                      colorScheme="red"
                                      variant="ghost"
                                      onClick={() => openRemoveMemberModal(
                                        tenant.id,
                                        member.userId,
                                        `${member.firstName} ${member.lastName}`
                                      )}
                                      isLoading={removingMemberId === member.userId}
                                      isDisabled={removingMemberId !== null || !tenant.isActive}
                                    />
                                  </Td>
                                </Tr>
                              ))}
                              
                              {/* Zaproszenia - tylko Pending (Accepted są już w members) */}
                              {tenant.invitations.filter(inv => inv.status === InvitationStatus.Pending).map((invitation) => (
                                <Tr key={invitation.invitationId} bg={useColorModeValue("yellow.50", "yellow.900")}>
                                  <Td>
                                    <Text color="gray.500" fontSize="sm" fontStyle="italic">
                                      Oczekuje na akceptację
                                    </Text>
                                  </Td>
                                  <Td>{invitation.email}</Td>
                                  <Td>
                                    <Badge colorScheme={getInvitationStatusColor(invitation.status)}>
                                      {getInvitationStatusName(invitation.status)}
                                    </Badge>
                                  </Td>
                                  <Td>
                                    <VStack align="flex-start" spacing={0}>
                                      <Text fontSize="xs" color="gray.500">
                                        Wysłano: {new Date(invitation.createdAt).toLocaleDateString('pl-PL')}
                                      </Text>
                                      {invitation.expiresAt && (
                                        <Text fontSize="xs" color="orange.500">
                                          Wygasa: {new Date(invitation.expiresAt).toLocaleDateString('pl-PL')}
                                        </Text>
                                      )}
                                    </VStack>
                                  </Td>
                                  <Td>
                                    <Text fontSize="xs" color="gray.400">
                                      -
                                    </Text>
                                  </Td>
                                </Tr>
                              ))}
                            </Tbody>
                          </Table>
                        )}
                      </Box>
                    </Collapse>
                  </Box>
                ))}
              </Stack>
            )}
          </Box>
        </VStack>
      </Box>

      {/* Modal potwierdzenia usunięcia członka */}
      <Modal isOpen={isRemoveModalOpen} onClose={onRemoveModalClose} isCentered>
        <ModalOverlay />
        <ModalContent>
          <ModalHeader>Usuń członka z organizacji</ModalHeader>
          <ModalCloseButton />
          <ModalBody>
            <VStack align="flex-start" spacing={3}>
              <Text>
                Czy na pewno chcesz usunąć <Text as="span" fontWeight="bold">{memberToRemove?.name}</Text> z organizacji?
              </Text>
              <Text fontSize="sm" color="gray.600">
                Ta osoba straci dostęp do wszystkich zasobów i danych organizacji.
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

      {/* Modal potwierdzenia zmiany statusu organizacji */}
      <Modal isOpen={isToggleStatusModalOpen} onClose={onToggleStatusModalClose} isCentered size="lg">
        <ModalOverlay />
        <ModalContent>
          <ModalHeader>
            {tenantToToggle?.isActive ? "Dezaktywuj organizację" : "Aktywuj organizację"}
          </ModalHeader>
          <ModalCloseButton />
          <ModalBody>
            <VStack align="flex-start" spacing={4}>
              <Text>
                Czy na pewno chcesz {tenantToToggle?.isActive ? "zdezaktywować" : "aktywować"} organizację <Text as="span" fontWeight="bold" color="blue.500">{tenantToToggle?.name}</Text>?
              </Text>
              {tenantToToggle?.isActive ? (
                <Box
                  p={4}
                  bg={useColorModeValue("orange.50", "orange.900")}
                  borderRadius="md"
                  borderWidth="1px"
                  borderColor={useColorModeValue("orange.200", "orange.700")}
                  width="100%"
                >
                  <VStack align="flex-start" spacing={3}>
                    <HStack spacing={2}>
                      <Icon as={Power} color="orange.500" />
                      <Text fontWeight="bold" color="orange.600" fontSize="sm">
                        ⚠️ Ważne informacje:
                      </Text>
                    </HStack>
                    <Text fontSize="sm">
                      • Zdezaktywowana organizacja będzie <Text as="span" fontWeight="bold">niedostępna</Text> dla wszystkich użytkowników
                    </Text>
                    <Text fontSize="sm">
                      • Nie będzie można edytować ani zapraszać nowych członków
                    </Text>
                    <Text fontSize="sm">
                      • Wszystkie dane organizacji zostaną zachowane
                    </Text>
                    <Text fontSize="sm">
                      • Możesz ponownie aktywować organizację w każdej chwili
                    </Text>
                    <Text fontSize="sm" fontWeight="medium" color="orange.700" mt={2}>
                      Operacja nie usuwa organizacji, tylko zawiesza jej działanie.
                    </Text>
                  </VStack>
                </Box>
              ) : (
                <Box
                  p={4}
                  bg={useColorModeValue("green.50", "green.900")}
                  borderRadius="md"
                  borderWidth="1px"
                  borderColor={useColorModeValue("green.200", "green.700")}
                  width="100%"
                >
                  <VStack align="flex-start" spacing={3}>
                    <HStack spacing={2}>
                      <Icon as={Power} color="green.500" />
                      <Text fontWeight="bold" color="green.600" fontSize="sm">
                        ℹ️ Informacje:
                      </Text>
                    </HStack>
                    <Text fontSize="sm">
                      • Organizacja stanie się <Text as="span" fontWeight="bold">dostępna</Text> dla wszystkich członków
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
          </ModalBody>
          <ModalFooter>
            <Button 
              variant="ghost" 
              mr={3} 
              onClick={onToggleStatusModalClose}
              isDisabled={togglingStatus}
            >
              Anuluj
            </Button>
            <Button 
              colorScheme={tenantToToggle?.isActive ? "red" : "green"}
              onClick={handleToggleTenantStatus}
              isLoading={togglingStatus}
              loadingText={tenantToToggle?.isActive ? "Dezaktywuję..." : "Aktywuję..."}
            >
              {tenantToToggle?.isActive ? "Dezaktywuj organizację" : "Aktywuj organizację"}
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    </MainLayout>
  );
}
