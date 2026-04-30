import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
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
  Badge,
  Stack,
  useDisclosure,
  Tooltip,
  Icon,
} from "@chakra-ui/react";
import { DeleteAlertDialog } from "../components/ui";
import { useToastNotification } from "../hooks/useToastNotification";
import { Building2, Plus, Trash2, Eye } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { getAdminTenants, createTenant, removeTenantMember } from "../services/tenantService";
import { handleApiError } from "../utils/handleApiError";
import { tenantApi } from "../api/tenantApi";
import type { TenantBasic, TenantDetails } from "../types/auth.types";
import { InvitationStatus, getInvitationStatusName, getInvitationStatusColor } from "../types/auth.types";
import { getRoleName, getRoleColor, RoleCodes } from "../constants/roleCodes";

export default function ManagedTenants() {
  const navigate = useNavigate();
  const [tenants, setTenants] = useState<TenantBasic[]>([]);
  const [loading, setLoading] = useState(true);
  
  const [isCreatingTenant, setIsCreatingTenant] = useState(false);
  const [newTenantName, setNewTenantName] = useState("");
  const [creatingTenant, setCreatingTenant] = useState(false);
  
  const [removingMemberId, setRemovingMemberId] = useState<string | null>(null);
  const [memberToRemove, setMemberToRemove] = useState<{ tenantId: string; userId: string; name: string } | null>(null);
  
  const { isOpen: isRemoveModalOpen, onOpen: onRemoveModalOpen, onClose: onRemoveModalClose } = useDisclosure();
  
  const { showApiSuccess, showError } = useToastNotification();

  // Pobierz tylko tenanty gdzie user jest adminem
  useEffect(() => {
    async function load() {
      try {
        const tenantsData = await getAdminTenants();
        setTenants(tenantsData);
      } catch (error) {
      } finally {
        setLoading(false);
      }
    }
    load();
  }, []);

  const handleCreateTenant = async () => {
    if (!newTenantName.trim()) {
      showError("Błąd walidacji", "Nazwa organizacji nie może być pusta");
      return;
    }

    setCreatingTenant(true);
    try {
      const newTenant = await createTenant(newTenantName);

      if (newTenant) {
        setTenants([...tenants, newTenant]);
        setNewTenantName("");
        setIsCreatingTenant(false);
        showApiSuccess('created');
      } else {
        showError("Błąd", "Nie udało się utworzyć organizacji");
      }
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setCreatingTenant(false);
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
        showApiSuccess('memberRemoved');
      } else {
        showError("Nie udało się usunąć członka", "Spróbuj ponownie lub skontaktuj się z administratorem");
      }
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setRemovingMemberId(null);
      setMemberToRemove(null);
    }
  };



  if (loading) {
    return (
      <MainLayout>
        <VStack spacing={4} align="center" justify="center" minH="50vh">
          <Spinner size="xl" color="primary.500" />
          <Text>Ładowanie organizacji...</Text>
        </VStack>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box bg="white" minH="100vh" p={{ base: 4, md: 6 }}>
        <VStack spacing={8} maxW="1200px" mx="auto" align="stretch">
          {/* Header */}
          <Stack direction={{ base: "column", md: "row" }} justify="space-between" align={{ base: "stretch", md: "center" }} spacing={4}>
            <HStack spacing={3}>
              <Building2 size={32} />
              <Heading size={{ base: "md", md: "lg" }}>Organizacje, którymi zarządzasz</Heading>
            </HStack>
            <Button
              leftIcon={<Plus size={20} />}
              colorScheme="primary"
              onClick={() => setIsCreatingTenant(true)}
              isDisabled={isCreatingTenant}
              width={{ base: "100%", md: "auto" }}
            >
              Nowa organizacja
            </Button>
          </Stack>

          {/* Formularz tworzenia nowej organizacji */}
          {isCreatingTenant && (
            <Box bg="white" p={6} rounded="lg" borderWidth="1px" borderColor="neutral.200">
              <VStack spacing={4} align="stretch">
                <Heading size="md">Utwórz nową organizację</Heading>
                <FormControl>
                  <FormLabel>Nazwa organizacji</FormLabel>
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
                    colorScheme="primary"
                    onClick={handleCreateTenant}
                    isLoading={creatingTenant}
                    flex={1}
                  >
                    Utwórz
                  </Button>
                  <Button
                    variant="ghost"
                    colorScheme="gray"
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
            {tenants.length === 0 ? (
              <Box bg="white" p={6} rounded="lg" borderWidth="1px" borderColor="neutral.200">
                <Text color="neutral.500" textAlign="center">
                  Nie zarządzasz jeszcze żadną organizacją. Utwórz nową!
                </Text>
              </Box>
            ) : (
              <Stack spacing={4}>
                {tenants.map((tenant) => (
                  <Box
                    key={tenant.id}
                    bg="white"
                    rounded="lg"
                    borderWidth="1px"
                    borderColor="neutral.200"
                    overflow="hidden"
                    cursor="pointer"
                    onClick={() => navigate(`/tenants/${tenant.id}`)}
                    transition="all 0.2s"
                    _hover={{
                      borderColor: "primary.300",
                      bg: "neutral.25",
                    }}
                  >
                    {/* Header organizacji */}
                    <Box p={{ base: 3, md: 4 }}>
                      <VStack align="flex-start" spacing={1}>
                        <HStack spacing={2}>
                          <Text fontWeight="bold" fontSize={{ base: "md", md: "lg" }}>{tenant.name}</Text>
                          <Badge colorScheme={tenant.isActive ? "green" : "gray"} fontSize="xs">
                            {tenant.isActive ? "Aktywna" : "Nieaktywna"}
                          </Badge>
                          <Badge colorScheme={getRoleColor(tenant.roleCode)} fontSize="xs">
                            {getRoleName(tenant.roleCode)}
                          </Badge>
                        </HStack>
                        <Text fontSize="xs" color="gray.500">
                          Utworzono: {new Date(tenant.createdAt).toLocaleDateString('pl-PL')}
                        </Text>
                      </VStack>
                    </Box>
                  </Box>
                ))}
              </Stack>
            )}
          </Box>
        </VStack>
      </Box>

      <DeleteAlertDialog
        isOpen={isRemoveModalOpen}
        onClose={onRemoveModalClose}
        onConfirm={handleRemoveMember}
        itemName={memberToRemove?.name}
        isLoading={!!removingMemberId}
      />
    </MainLayout>
  );
}
