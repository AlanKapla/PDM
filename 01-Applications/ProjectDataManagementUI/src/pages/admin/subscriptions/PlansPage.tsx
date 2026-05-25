import React from "react";
import {
  Box,
  Heading,
  Text,
  Spinner,
  Alert,
  AlertIcon,
  useDisclosure,
} from "@chakra-ui/react";
import MainLayout from "../../../layout/MainLayout";
import { PlanDefinitionTable } from "./components/PlanDefinitionTable";
import { EditPlanModal } from "./components/EditPlanModal";
import { useAdminSubscriptionPlansList } from "../../../hooks/queries";
import type { PlanDefinition } from "../../../types/subscription";

export default function PlansPage(): React.ReactElement {
  const { data: plans = [], isLoading, isError } = useAdminSubscriptionPlansList();
  const { isOpen, onOpen, onClose } = useDisclosure();
  const [selectedPlan, setSelectedPlan] = React.useState<PlanDefinition | null>(null);

  function handleEdit(plan: PlanDefinition): void {
    setSelectedPlan(plan);
    onOpen();
  }

  function handleClose(): void {
    onClose();
    setSelectedPlan(null);
  }

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 8 }}>
        <Heading size="lg" mb={1}>
          Plany subskrypcji
        </Heading>
        <Text color="gray.500" mb={6} fontSize="sm">
          Zarządzanie definicjami planów subskrypcji
        </Text>

        {isLoading && (
          <Box py={10} textAlign="center">
            <Spinner color="primary.500" />
          </Box>
        )}

        {isError && (
          <Alert status="error" borderRadius="md">
            <AlertIcon />
            Nie udało się załadować planów subskrypcji.
          </Alert>
        )}

        {!isLoading && !isError && (
          <PlanDefinitionTable plans={plans} onEdit={handleEdit} />
        )}
      </Box>

      <EditPlanModal plan={selectedPlan} isOpen={isOpen} onClose={handleClose} />
    </MainLayout>
  );
}
