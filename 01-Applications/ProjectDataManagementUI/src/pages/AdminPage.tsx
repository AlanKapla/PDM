import React, { useState } from "react";
import {
  Box,
  Tabs,
  TabList,
  TabPanels,
  Tab,
  TabPanel,
  Heading,
  Text,
  Spinner,
  Alert,
  AlertIcon,
  useDisclosure,
} from "@chakra-ui/react";
import { Users, Building2, CreditCard } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { AdminUserTable } from "../components/admin/AdminUserTable";
import { AdminUserDetailsModal } from "../components/admin/AdminUserDetailsModal";
import { AdminTenantTable } from "../components/admin/AdminTenantTable";
import { AdminTenantDetailsModal } from "../components/admin/AdminTenantDetailsModal";
import { AdminSubscriptionPlansTable } from "../components/admin/AdminSubscriptionPlansTable";
import { AdminSubscriptionPlanEditModal } from "../components/admin/AdminSubscriptionPlanEditModal";
import { useAdminUsers, useAdminTenants, useAdminSubscriptionPlans } from "../hooks/queries";
import type { SubscriptionPlanDefinitionWeb } from "../types/admin.types";

export default function AdminPage(): React.ReactElement {
  const { data: users = [], isLoading: usersLoading, isError: usersError } = useAdminUsers();
  const { data: tenants = [], isLoading: tenantsLoading, isError: tenantsError } = useAdminTenants();
  const { data: subscriptionPlans = [], isLoading: plansLoading, isError: plansError } = useAdminSubscriptionPlans();

  const {
    isOpen: isUserModalOpen,
    onOpen: onUserModalOpen,
    onClose: onUserModalClose,
  } = useDisclosure();
  const {
    isOpen: isTenantModalOpen,
    onOpen: onTenantModalOpen,
    onClose: onTenantModalClose,
  } = useDisclosure();
  const {
    isOpen: isPlanModalOpen,
    onOpen: onPlanModalOpen,
    onClose: onPlanModalClose,
  } = useDisclosure();

  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const [selectedTenantId, setSelectedTenantId] = useState<string | null>(null);
  const [selectedPlan, setSelectedPlan] = useState<SubscriptionPlanDefinitionWeb | null>(null);

  function handleViewUser(userId: string): void {
    setSelectedUserId(userId);
    onUserModalOpen();
  }

  function handleCloseUser(): void {
    onUserModalClose();
    setSelectedUserId(null);
  }

  function handleViewTenant(tenantId: string): void {
    setSelectedTenantId(tenantId);
    onTenantModalOpen();
  }

  function handleCloseTenant(): void {
    onTenantModalClose();
    setSelectedTenantId(null);
  }

  function handleEditPlan(plan: SubscriptionPlanDefinitionWeb): void {
    setSelectedPlan(plan);
    onPlanModalOpen();
  }

  function handleClosePlan(): void {
    onPlanModalClose();
    setSelectedPlan(null);
  }

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 8 }}>
        <Heading size="lg" mb={1}>
          Panel administratora
        </Heading>
        <Text color="gray.500" mb={6} fontSize="sm">
          Dostęp tylko dla SuperAdmin
        </Text>

        <Tabs variant="enclosed" colorScheme="primary">
          <TabList>
            <Tab gap={2}>
              <Users size={16} />
              Użytkownicy
            </Tab>
            <Tab gap={2}>
              <Building2 size={16} />
              Organizacje
            </Tab>
            <Tab gap={2}>
              <CreditCard size={16} />
              Plany subskrypcji
            </Tab>
          </TabList>

          <TabPanels>
            {/* Użytkownicy */}
            <TabPanel px={0} pt={4}>
              {usersLoading && (
                <Box py={10} textAlign="center">
                  <Spinner color="primary.500" />
                </Box>
              )}
              {usersError && (
                <Alert status="error" borderRadius="md">
                  <AlertIcon />
                  Nie udało się załadować listy użytkowników.
                </Alert>
              )}
              {!usersLoading && !usersError && (
                <AdminUserTable users={users} onViewDetails={handleViewUser} />
              )}
            </TabPanel>

            {/* Organizacje */}
            <TabPanel px={0} pt={4}>
              {tenantsLoading && (
                <Box py={10} textAlign="center">
                  <Spinner color="primary.500" />
                </Box>
              )}
              {tenantsError && (
                <Alert status="error" borderRadius="md">
                  <AlertIcon />
                  Nie udało się załadować listy organizacji.
                </Alert>
              )}
              {!tenantsLoading && !tenantsError && (
                <AdminTenantTable tenants={tenants} onViewDetails={handleViewTenant} />
              )}
            </TabPanel>

            {/* Plany subskrypcji */}
            <TabPanel px={0} pt={4}>
              {plansLoading && (
                <Box py={10} textAlign="center">
                  <Spinner color="primary.500" />
                </Box>
              )}
              {plansError && (
                <Alert status="error" borderRadius="md">
                  <AlertIcon />
                  Nie udało się załadować planów subskrypcji.
                </Alert>
              )}
              {!plansLoading && !plansError && (
                <AdminSubscriptionPlansTable plans={subscriptionPlans} onEdit={handleEditPlan} />
              )}
            </TabPanel>
          </TabPanels>
        </Tabs>
      </Box>

      <AdminUserDetailsModal
        userId={selectedUserId}
        isOpen={isUserModalOpen}
        onClose={handleCloseUser}
      />

      <AdminTenantDetailsModal
        tenantId={selectedTenantId}
        isOpen={isTenantModalOpen}
        onClose={handleCloseTenant}
      />

      <AdminSubscriptionPlanEditModal
        plan={selectedPlan}
        isOpen={isPlanModalOpen}
        onClose={handleClosePlan}
      />
    </MainLayout>
  );
}
