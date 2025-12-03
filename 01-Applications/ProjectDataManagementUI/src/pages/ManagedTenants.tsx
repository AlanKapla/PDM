import { useEffect, useState } from "react";
import {
  Box,
  Heading,
  Text,
  VStack,
  HStack,
  Button,
  Badge,
  Spinner,
  useToast,
  useColorModeValue,
  Divider,
} from "@chakra-ui/react";
import { Building2, Users, ArrowLeft } from "lucide-react";
import { useNavigate } from "react-router-dom";
import MainLayout from "../layout/MainLayout";
import { getUserTenants } from "../services/tenantService";
import { TenantRole, getTenantRoleName, getTenantRoleColor } from "../types/auth.types";
import type { TenantDetails } from "../types/auth.types";

export default function ManagedTenants() {
  const [tenants, setTenants] = useState<TenantDetails[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();
  const toast = useToast();

  const cardBg = useColorModeValue("white", "#101010");
  const border = useColorModeValue("gray.200", "#1e1e1e");
  const muted = useColorModeValue("gray.600", "gray.400");

  useEffect(() => {
    async function load() {
      setLoading(true);
      try {
        const allTenants = await getUserTenants();
        const managed = allTenants.filter((t) => t.role === TenantRole.Admin);
        setTenants(managed);
      } catch (err) {
        console.error("Błąd ładowania organizacji", err);
        toast({
          title: "Błąd ładowania organizacji",
          status: "error",
          duration: 4000,
          isClosable: true,
        });
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [toast]);

  return (
    <MainLayout>
      <Box maxW="1200px" mx="auto" px={4} py={10}>
        <Button
          leftIcon={<ArrowLeft size={18} />}
          variant="ghost"
          mb={6}
          onClick={() => navigate("/tenants")}
        >
          Wróć do wszystkich organizacji
        </Button>

        <VStack align="stretch" spacing={6}>
          <VStack align="flex-start" spacing={2}>
            <Heading size="lg">Organizacje, którymi zarządzasz</Heading>
            <Text fontSize="sm" color={muted}>
              Masz pełne uprawnienia administratora w tych organizacjach.
            </Text>
          </VStack>

          {loading ? (
            <HStack justify="center" py={10}>
              <Spinner size="xl" />
              <Text>Ładowanie organizacji...</Text>
            </HStack>
          ) : tenants.length === 0 ? (
            <Box
              p={10}
              textAlign="center"
              bg={cardBg}
              border="1px dashed"
              borderColor={border}
              rounded="2xl"
            >
              <Heading size="md" mb={3}>
                Brak zarządzanych organizacji
              </Heading>
              <Text color={muted} mb={6}>
                Nie zarządzasz jeszcze żadną organizacją. Utwórz nową organizację, aby rozpocząć.
              </Text>
              <Button colorScheme="blue" onClick={() => navigate("/tenants")}>
                Przejdź do organizacji
              </Button>
            </Box>
          ) : (
            <VStack spacing={4} align="stretch">
              {tenants.map((tenant) => (
                <Box
                  key={tenant.id}
                  p={6}
                  bg={cardBg}
                  rounded="xl"
                  border="1px solid"
                  borderColor={border}
                  shadow="sm"
                >
                  <HStack justify="space-between" mb={4}>
                    <HStack spacing={3}>
                      <Box
                        w="40px"
                        h="40px"
                        rounded="lg"
                        bg="gray.100"
                        display="flex"
                        alignItems="center"
                        justifyContent="center"
                      >
                        <Building2 size={20} />
                      </Box>
                      <VStack align="flex-start" spacing={1}>
                        <Text fontWeight="semibold" fontSize="lg">
                          {tenant.name}
                        </Text>
                        <HStack spacing={2}>
                          <Badge colorScheme={getTenantRoleColor(tenant.role)} fontSize="xs">
                            {getTenantRoleName(tenant.role)}
                          </Badge>
                          <Text fontSize="xs" color={muted}>
                            Utworzono: {new Date(tenant.createdAt).toLocaleDateString("pl-PL")}
                          </Text>
                        </HStack>
                      </VStack>
                    </HStack>

                    <Button
                      size="sm"
                      colorScheme="blue"
                      variant="outline"
                      onClick={() => navigate("/tenants")}
                    >
                      Zarządzaj
                    </Button>
                  </HStack>

                  <Divider borderColor={border} mb={4} />

                  <HStack spacing={6}>
                    <VStack align="flex-start" spacing={0}>
                      <Text fontSize="xs" color={muted}>
                        Członkowie
                      </Text>
                      <HStack spacing={2}>
                        <Users size={16} />
                        <Text fontSize="sm" fontWeight="medium">
                          {tenant.members?.length ?? 0}
                        </Text>
                      </HStack>
                    </VStack>

                    {tenant.invitations && tenant.invitations.filter(i => i.status === 0).length > 0 && (
                      <VStack align="flex-start" spacing={0}>
                        <Text fontSize="xs" color={muted}>
                          Oczekujące zaproszenia
                        </Text>
                        <Text fontSize="sm" fontWeight="medium" color="yellow.400">
                          {tenant.invitations.filter(i => i.status === 0).length}
                        </Text>
                      </VStack>
                    )}
                  </HStack>
                </Box>
              ))}
            </VStack>
          )}
        </VStack>
      </Box>
    </MainLayout>
  );
}
