import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
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
  Radio,
  RadioGroup,
  Stack,
} from "@chakra-ui/react";
import { Building2, CheckCircle2, Plus, Edit2, UserPlus, Eye } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { useAuth } from "../context/AuthContext";
import { getUserTenants, changeActiveTenant, createTenant, updateTenant, inviteTenantMember } from "../services/tenantService";
import type { UserTenant } from "../types/auth.types";
import { getRoleName, getRoleColor, RoleCodes } from "../constants/roleCodes";

export default function Tenants() {
  const navigate = useNavigate();
  const { user, refreshUser } = useAuth();
  const [tenants, setTenants] = useState<UserTenant[]>([]);
  const [activeTenantId, setActiveTenantId] = useState<string>("");
  const [changingTenant, setChangingTenant] = useState(false);
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
  
  const toast = useToast();

  const cardBg = useColorModeValue("white", "gray.800");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const labelColor = useColorModeValue("gray.700", "gray.300");
  const activeBg = useColorModeValue("blue.50", "blue.900");
  const borderColor = useColorModeValue("gray.200", "gray.600");

  // Podział na tenantów zarządzanych i współpracujących
  const managedTenants = tenants.filter(t => t.roleCode === RoleCodes.TENANT_ADMIN);
  const collaboratingTenants = tenants.filter(t => t.roleCode !== RoleCodes.TENANT_ADMIN);

  useEffect(() => {
    async function load() {
      try {
        const tenantsData = await getUserTenants();
        setTenants(tenantsData);
        
        // Ustaw activeTenantId z user (już jest w AuthContext z /me)
        if (user?.activeTenantId) {
          setActiveTenantId(user.activeTenantId);
        }
      } catch (error) {
        console.error("Błąd ładowania danych:", error);
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [user?.activeTenantId]);

  const handleTenantChange = async (newTenantId: string) => {
    if (newTenantId === activeTenantId) return;

    setChangingTenant(true);
    try {
      const success = await changeActiveTenant(newTenantId);
      
      if (success) {
        // Odśwież dane użytkownika z /me (zawiera nowy activeTenantId)
        await refreshUser();
        
        setActiveTenantId(newTenantId);
        toast({
          title: "Organizacja zmieniona",
          description: "Aktywna organizacja została zaktualizowana",
          status: "success",
          duration: 3000,
          isClosable: true,
        });
        
        // Przeładuj stronę aby odświeżyć wszystkie dane
        setTimeout(() => {
          window.location.reload();
        }, 500);
      } else {
        toast({
          title: "Błąd zmiany organizacji",
          description: "Nie udało się zmienić aktywnej organizacji",
          status: "error",
          duration: 3000,
          isClosable: true,
        });
      }
    } catch (error) {
      console.error("Błąd zmiany tenanta:", error);
      toast({
        title: "Błąd",
        description: "Wystąpił problem z połączeniem",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
    } finally {
      setChangingTenant(false);
    }
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
          <HStack justify="space-between" align="center">
            <HStack spacing={3}>
              <Building2 size={32} />
              <Heading size="lg">Organizacje</Heading>
            </HStack>
            <Button
              leftIcon={<Plus size={20} />}
              colorScheme="blue"
              onClick={() => setIsCreatingTenant(true)}
              isDisabled={isCreatingTenant}
            >
              Nowa organizacja
            </Button>
          </HStack>

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

          {/* Sekcja: Organizacje, z którymi współpracujesz */}
          <Box>
            <Heading size="md" mb={4}>Organizacje, z którymi współpracujesz</Heading>
            {collaboratingTenants.length === 0 ? (
              <Box bg={cardBg} p={6} rounded="lg" shadow="md" borderWidth="1px" borderColor={borderColor}>
                <Text color="gray.500" textAlign="center">
                  Nie współpracujesz jeszcze z żadną organizacją
                </Text>
              </Box>
            ) : (
              <Box bg={cardBg} p={6} rounded="lg" shadow="md" borderWidth="1px" borderColor={borderColor}>
                <RadioGroup value={activeTenantId} onChange={handleTenantChange}>
                  <Stack spacing={3}>
                    {collaboratingTenants.map((tenant) => (
                      <Box
                        key={tenant.id}
                        p={4}
                        rounded="lg"
                        border="1px solid"
                        borderColor={tenant.id === activeTenantId ? "blue.500" : borderColor}
                        bg={tenant.id === activeTenantId ? activeBg : "transparent"}
                        transition="all 0.2s"
                      >
                        <HStack justify="space-between">
                          <HStack spacing={3} flex={1}>
                            <Radio value={tenant.id} isDisabled={changingTenant || !tenant.isActive}>
                              <VStack align="flex-start" spacing={1}>
                                <HStack spacing={2}>
                                  <Text fontWeight={tenant.id === activeTenantId ? "bold" : "normal"}>
                                    {tenant.name}
                                  </Text>
                                  <Badge colorScheme={tenant.isActive ? "green" : "gray"} fontSize="xs">
                                    {tenant.isActive ? "Aktywna" : "Nieaktywna"}
                                  </Badge>
                                </HStack>
                                <HStack spacing={2}>
                                  <Text fontSize="xs" color="gray.500">
                                    Utworzono: {new Date(tenant.createdAt).toLocaleDateString('pl-PL')}
                                  </Text>
                                  <Badge colorScheme={getRoleColor(tenant.roleCode)} fontSize="xs">
                                    {getRoleName(tenant.roleCode)}
                                  </Badge>
                                </HStack>
                              </VStack>
                            </Radio>
                          </HStack>
                          {tenant.id === activeTenantId && (
                            <Badge colorScheme="blue" display="flex" alignItems="center" gap={1}>
                              <CheckCircle2 size={14} />
                              Aktywny
                            </Badge>
                          )}
                        </HStack>
                      </Box>
                    ))}
                  </Stack>
                </RadioGroup>
              </Box>
            )}
          </Box>

          {/* Sekcja: Organizacje, którymi zarządzasz */}
          <Box>
            <Heading size="md" mb={4}>Organizacje, którymi zarządzasz</Heading>
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
                    borderColor={tenant.id === activeTenantId ? "blue.500" : borderColor}
                    overflow="hidden"
                  >
                    {/* Header organizacji */}
                    <Box p={4} bg={tenant.id === activeTenantId ? activeBg : "transparent"}>
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
                          <HStack justify="space-between">
                            <VStack align="flex-start" spacing={1}>
                              <HStack>
                                <Text fontWeight="bold" fontSize="lg">{tenant.name}</Text>
                                <Badge colorScheme={tenant.isActive ? "green" : "gray"} fontSize="xs">
                                  {tenant.isActive ? "Aktywna" : "Nieaktywna"}
                                </Badge>
                                {tenant.id === activeTenantId && (
                                  <Badge colorScheme="blue" display="flex" alignItems="center" gap={1}>
                                    <CheckCircle2 size={14} />
                                    Aktywny
                                  </Badge>
                                )}
                              </HStack>
                              <Text fontSize="xs" color="gray.500">
                                Utworzono: {new Date(tenant.createdAt).toLocaleDateString('pl-PL')}
                              </Text>
                            </VStack>
                            <HStack spacing={2}>
                              <Button
                                size="sm"
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
                                size="sm"
                                variant="ghost"
                                leftIcon={<Edit2 size={14} />}
                                onClick={() => {
                                  setEditingTenantId(tenant.id);
                                  setEditTenantName(tenant.name);
                                }}
                              >
                                Edytuj
                              </Button>
                              {tenant.id !== activeTenantId && (
                                <Button
                                  size="sm"
                                  colorScheme="blue"
                                  variant="outline"
                                  onClick={() => handleTenantChange(tenant.id)}
                                  isLoading={changingTenant}
                                >
                                  Ustaw jako aktywny
                                </Button>
                              )}
                              <Button
                                size="sm"
                                colorScheme="blue"
                                leftIcon={<Eye size={14} />}
                                onClick={() => navigate(`/tenants/${tenant.id}`)}
                              >
                                Szczegóły
                              </Button>
                            </HStack>
                          </HStack>
                        </VStack>
                      )}
                    </Box>
                  </Box>
                ))}
              </Stack>
            )}
          </Box>
        </VStack>
      </Box>
    </MainLayout>
  );
}
