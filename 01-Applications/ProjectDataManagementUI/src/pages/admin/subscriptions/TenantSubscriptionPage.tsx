import React from "react";
import { useParams } from "react-router-dom";
import {
  Box,
  Heading,
  Text,
  Spinner,
  Alert,
  AlertIcon,
  VStack,
  HStack,
  Button,
  useDisclosure,
} from "@chakra-ui/react";
import MainLayout from "../../../layout/MainLayout";
import { TenantSubscriptionCard } from "./components/TenantSubscriptionCard";
import { FullAccessToggle } from "./components/FullAccessToggle";
import { OverridesTable } from "./components/OverridesTable";
import { AddOverrideModal } from "./components/AddOverrideModal";
import { PaymentHistoryTable } from "./components/PaymentHistoryTable";
import { useTenantSubscription, useAdminPaymentHistory } from "../../../hooks/queries";

export default function TenantSubscriptionPage(): React.ReactElement {
  const { tenantId } = useParams<{ tenantId: string }>();
  const { data: subscription, isLoading, isError } = useTenantSubscription(
    tenantId ?? null,
  );
  const { data: payments = [] } = useAdminPaymentHistory(tenantId ?? null);
  const { isOpen, onOpen, onClose } = useDisclosure();

  if (!tenantId) {
    return (
      <MainLayout>
        <Box p={{ base: 4, md: 8 }}>
          <Alert status="warning" borderRadius="md">
            <AlertIcon />
            Brak identyfikatora tenanta w adresie URL.
          </Alert>
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 8 }}>
        <Heading size="lg" mb={1}>
          Subskrypcja tenanta
        </Heading>
        <Text color="gray.500" mb={6} fontSize="sm">
          {tenantId}
        </Text>

        {isLoading && (
          <Box py={10} textAlign="center">
            <Spinner color="primary.500" />
          </Box>
        )}

        {isError && (
          <Alert status="warning" borderRadius="md">
            <AlertIcon />
            Nie znaleziono subskrypcji dla tego tenanta.
          </Alert>
        )}

        {!isLoading && !isError && subscription && (
          <VStack spacing={6} align="stretch">
            {/* Sekcja 1 — subskrypcja */}
            <TenantSubscriptionCard
              subscription={subscription}
              tenantId={tenantId}
            />

            {/* Sekcja 2 — full access */}
            <FullAccessToggle
              subscription={subscription}
              tenantId={tenantId}
            />

            {/* Sekcja 3 — overrides */}
            <Box>
              <HStack justify="space-between" mb={3}>
                <Heading size="sm">Override'y</Heading>
                <Button size="sm" colorScheme="blue" onClick={onOpen}>
                  Dodaj override
                </Button>
              </HStack>
              <OverridesTable
                overrides={subscription.overrides}
                tenantId={tenantId}
              />
            </Box>

            {/* Sekcja 4 — historia płatności */}
            <PaymentHistoryTable payments={payments} />
          </VStack>
        )}
      </Box>

      {tenantId && (
        <AddOverrideModal
          tenantId={tenantId}
          isOpen={isOpen}
          onClose={onClose}
        />
      )}
    </MainLayout>
  );
}
