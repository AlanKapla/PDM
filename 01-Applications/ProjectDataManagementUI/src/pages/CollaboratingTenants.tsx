import { useEffect, useState } from "react";
import {
  Box,
  Heading,
  Text,
  Spinner,
  VStack,
  useColorModeValue,
  HStack,
  Badge,
  Radio,
  RadioGroup,
  Stack,
} from "@chakra-ui/react";
import { Building2, CheckCircle2 } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { useAuth } from "../context/AuthContext";
import { changeActiveTenant } from "../services/tenantService";
import { useMyTenants, tenantKeys } from "../hooks/queries";
import { useQueryClient } from "@tanstack/react-query";
import { useToastNotification } from "../hooks/useToastNotification";
import { handleApiError } from "../utils/handleApiError";
import type { UserTenant } from "../types/auth.types";
import { getRoleName, getRoleColor, RoleCodes } from "../constants/roleCodes";
import { useNavigate } from "react-router-dom";
import { SubscriptionStatus } from "../types/subscription";

function isSubscriptionBlocked(status: SubscriptionStatus | undefined | null): boolean {
  if (status == null) return false;
  return (
    status === SubscriptionStatus.PastDue ||
    status === SubscriptionStatus.Canceled ||
    status === SubscriptionStatus.GracePeriod
  );
}

export default function CollaboratingTenants() {
  const { user, refreshUser } = useAuth();
  const { showSuccess, showError, showApiSuccess, showWarning } = useToastNotification();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { data: tenants = [], isLoading: loading } = useMyTenants();
  const [activeTenantId, setActiveTenantId] = useState<string>("");
  const [changingTenant, setChangingTenant] = useState(false);

  const cardBg = useColorModeValue("white", "gray.800");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const activeBg = useColorModeValue("primary.50", "primary.900");
  const borderColor = useColorModeValue("gray.200", "gray.600");

  useEffect(() => {
    if (user?.activeTenantId) {
      setActiveTenantId(user.activeTenantId);
    }
  }, [user?.activeTenantId]);

  const handleTenantChange = async (newTenantId: string) => {
    if (newTenantId === activeTenantId) return;

    setChangingTenant(true);
    try {
      const result = await changeActiveTenant(newTenantId);
      setActiveTenantId(newTenantId);
      queryClient.invalidateQueries({ queryKey: tenantKeys.my() });
      await refreshUser();
      if (result.isSubscriptionBlocked) {
        navigate(`/tenants/${newTenantId}/subscription`);
        return;
      }
      showApiSuccess('tenantSwitched');
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setChangingTenant(false);
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
      <Box bg={pageBg} minH="100vh" p={{ base: 4, md: 6 }}>
        <VStack spacing={8} maxW="1200px" mx="auto" align="stretch">
          {/* Header */}
          <HStack spacing={3} flexWrap="wrap">
            <Building2 size={32} />
            <Heading size={{ base: "md", md: "lg" }}>Organizacje, z którymi współpracujesz</Heading>
          </HStack>

          {/* Lista organizacji */}
          <Box>
            {tenants.length === 0 ? (
              <Box bg={cardBg} p={6} rounded="lg" shadow="md" borderWidth="1px" borderColor={borderColor}>
                <Text color="neutral.500" textAlign="center">
                  Nie współpracujesz jeszcze z żadną organizacją
                </Text>
              </Box>
            ) : (
              <Box bg={cardBg} p={6} rounded="lg" shadow="md" borderWidth="1px" borderColor={borderColor}>
                <RadioGroup value={activeTenantId} onChange={handleTenantChange}>
                  <Stack spacing={3}>
                    {tenants.map((tenant) => (
                      <Box
                        key={tenant.id}
                        p={4}
                        rounded="lg"
                        border="1px solid"
                        borderColor={tenant.id === activeTenantId ? "primary.500" : borderColor}
                        bg={tenant.id === activeTenantId ? activeBg : "transparent"}
                        transition="all 0.2s"
                      >
                        <Stack direction={{ base: "column", md: "row" }} justify="space-between" spacing={3}>
                          <HStack spacing={3} flex={1} align="flex-start">
                            <Radio value={tenant.id} isDisabled={changingTenant || (isSubscriptionBlocked(tenant.subscriptionStatus) && tenant.roleCode !== RoleCodes.TENANT_ADMIN)} mt={1}>
                              <VStack align="flex-start" spacing={1}>
                                <Text fontWeight={tenant.id === activeTenantId ? "bold" : "normal"}>
                                  {tenant.name}
                                </Text>
                                <Stack direction={{ base: "column", sm: "row" }} spacing={2}>
                                  <Text fontSize="xs" color="neutral.500">
                                    Utworzono: {new Date(tenant.createdAt).toLocaleDateString('pl-PL')}
                                  </Text>
                                  <Badge colorScheme={getRoleColor(tenant.roleCode)} fontSize="xs">
                                    {getRoleName(tenant.roleCode)}
                                  </Badge>
                                  {isSubscriptionBlocked(tenant.subscriptionStatus) && (
                                    <Badge colorScheme="red" fontSize="xs">
                                      Nieaktywna subskrypcja
                                    </Badge>
                                  )}
                                </Stack>
                              </VStack>
                            </Radio>
                          </HStack>
                          {tenant.id === activeTenantId && (
                            <Badge colorScheme="primary" display="flex" alignItems="center" gap={1} alignSelf={{ base: "flex-start", md: "center" }} ml={{ base: 6, md: 0 }}>
                              <CheckCircle2 size={14} />
                              Włączona
                            </Badge>
                          )}
                        </Stack>
                      </Box>
                    ))}
                  </Stack>
                </RadioGroup>

                {changingTenant && (
                  <HStack mt={4} spacing={2}>
                    <Spinner size="sm" />
                    <Text fontSize="sm" color="neutral.500">
                      Przełączanie organizacji...
                    </Text>
                  </HStack>
                )}
              </Box>
            )}
          </Box>
        </VStack>
      </Box>
    </MainLayout>
  );
}
