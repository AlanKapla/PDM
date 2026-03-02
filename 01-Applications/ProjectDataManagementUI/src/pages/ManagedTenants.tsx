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
  Stack,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  IconButton,
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
  
  const toast = useToast();

  const cardBg = useColorModeValue("white", "gray.800");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const labelColor = useColorModeValue("gray.700", "gray.300");
  const borderColor = useColorModeValue("gray.200", "gray.600");

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
            {tenants.length === 0 ? (
              <Box bg={cardBg} p={6} rounded="lg" shadow="md" borderWidth="1px" borderColor={borderColor}>
                <Text color="gray.500" textAlign="center">
                  Nie zarządzasz jeszcze żadną organizacją. Utwórz nową!
                </Text>
              </Box>
            ) : (
              <Stack spacing={4}>
                {tenants.map((tenant) => (
                  <Box
                    key={tenant.id}
                    bg={cardBg}
                    rounded="lg"
                    shadow="md"
                    borderWidth="1px"
                    borderColor={borderColor}
                    overflow="hidden"
                    cursor="pointer"
                    onClick={() => navigate(`/tenants/${tenant.id}`)}
                    transition="all 0.2s"
                    _hover={{
                      shadow: "lg",
                      borderColor: "blue.400",
                      transform: "translateY(-2px)",
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
    </MainLayout>
  );
}
