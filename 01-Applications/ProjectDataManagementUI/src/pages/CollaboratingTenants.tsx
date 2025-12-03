import { useEffect, useState } from "react";
import {
  Box,
  Text,
  VStack,
  Flex,
  HStack,
  Spinner,
  RadioGroup,
  Radio,
  Badge,
  Icon,
} from "@chakra-ui/react";
import { Building2, CheckCircle2 } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { PageHeader } from "../components/PageHeader";
import {
  getUserTenants,
  getActiveTenant,
  changeActiveTenant,
} from "../services/tenantService";
import type { TenantDetails } from "../types/auth.types";
import { getTenantRoleName, getTenantRoleColor } from "../types/auth.types";

export default function CollaboratingTenants() {
  const [tenants, setTenants] = useState<TenantDetails[]>([]);
  const [activeTenantId, setActiveTenantId] = useState<string>("");
  const [changingTenant, setChangingTenant] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      try {
        const [tenantList, active] = await Promise.all([
          getUserTenants(),
          getActiveTenant(),
        ]);

        setTenants(tenantList);
        if (active?.activeTenantId) setActiveTenantId(active.activeTenantId);
      } finally {
        setLoading(false);
      }
    };

    load();
  }, []);

  const handleTenantChange = async (newId: string) => {
    if (newId === activeTenantId) return;

    setChangingTenant(true);
    try {
      const ok = await changeActiveTenant(newId);
      if (ok) {
        setActiveTenantId(newId);
        setTimeout(() => window.location.reload(), 500);
      }
    } finally {
      setChangingTenant(false);
    }
  };

  if (loading) {
    return (
      <MainLayout>
        <Flex justify="center" align="center" minH="60vh">
          <Spinner size="xl" color="gray.300" />
        </Flex>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={10}>
        <PageHeader
          title="Organizacje, z którymi współpracujesz"
          breadcrumb={["Organizacje", "Współpracujesz"]}
        />

        {/* BRAK ORGANIZACJI */}
        {tenants.length === 0 && (
          <Flex
            direction="column"
            align="center"
            bg="white"
            border="1px solid"
            borderColor="gray.200"
            rounded="md"
            p={12}
          >
            <Icon as={Building2} boxSize={20} color="gray.500" mb={4} />
            <Text fontSize="lg" color="gray.300" mb={1}>
              Nie współpracujesz jeszcze z żadną organizacją
            </Text>
            <Text color="gray.500" fontSize="sm">
              Gdy ktoś doda Cię do zespołu, pojawi się tutaj.
            </Text>
          </Flex>
        )}

        {/* LISTA ORGANIZACJI */}
        {tenants.length > 0 && (
          <Box
            bg="white"
            border="1px solid"
            borderColor="gray.200"
            rounded="md"
            mt={4}
            overflow="hidden"
          >
            <RadioGroup value={activeTenantId} onChange={handleTenantChange}>
              <VStack align="stretch" spacing={0}>
                {tenants.map((tenant) => {
                  const isActive = tenant.id === activeTenantId;

                  return (
                    <Flex
                      key={tenant.id}
                      justify="space-between"
                      align="center"
                      px={5}
                      py={4}
                      borderBottom="1px solid #1e1e1e"
                      _hover={{ bg: "#1a1a1a" }}
                      bg={isActive ? "#181818" : "transparent"}
                    >
                      {/* Lewa część */}
                      <HStack align="flex-start" spacing={4}>
                        <Radio value={tenant.id} isDisabled={changingTenant} />

                        <Box>
                          <Text
                            fontSize="md"
                            fontWeight={isActive ? "semibold" : "normal"}
                            color="gray.200"
                          >
                            {tenant.name}
                          </Text>

                          <HStack spacing={2} mt={1}>
                            <Text fontSize="xs" color="gray.500">
                              Utworzono:{" "}
                              {new Date(tenant.createdAt).toLocaleDateString(
                                "pl-PL"
                              )}
                            </Text>

                            <Badge
                              fontSize="xs"
                              colorScheme={getTenantRoleColor(tenant.role)}
                            >
                              {getTenantRoleName(tenant.role)}
                            </Badge>
                          </HStack>
                        </Box>
                      </HStack>

                      {/* Prawa część */}
                      {isActive && (
                        <HStack spacing={1} color="blue.400">
                          <CheckCircle2 size={16} />
                          <Text fontSize="sm">Aktywny</Text>
                        </HStack>
                      )}
                    </Flex>
                  );
                })}
              </VStack>
            </RadioGroup>

            {changingTenant && (
              <Flex px={5} py={3} align="center" gap={2}>
                <Spinner size="sm" />
                <Text fontSize="sm" color="gray.400">
                  Zmienianie aktywnej organizacji...
                </Text>
              </Flex>
            )}
          </Box>
        )}
      </Box>
    </MainLayout>
  );
}
