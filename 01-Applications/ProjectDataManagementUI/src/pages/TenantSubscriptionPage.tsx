import React from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Box,
  Heading,
  Text,
  Spinner,
  Alert,
  AlertIcon,
  VStack,
  Button,
  HStack,
  Card,
  CardHeader,
  CardBody,
  Badge,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  Select,
  useToast,
  useDisclosure,
  Divider,
} from "@chakra-ui/react";
import { ArrowLeft, CreditCard } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import {
  useMyTenantSubscription,
  useSubscriptionPlans,
  useSubscriptionStatus,
  useRequestPlanChange,
  useProcessMockPayment,
  usePaymentHistory,
} from "../hooks/queries";
import {
  PlanLabels,
  StatusLabels,
  formatLimit,
  SubscriptionPlan,
  SubscriptionStatus,
  type TenantSubscriptionInfo,
  type SubscriptionPlanInfo,
  type SubscriptionStatusInfo,
  type SubscriptionPaymentInfo,
} from "../types/subscription";
import ConfirmAlertDialog from "../components/ui/ConfirmAlertDialog";

function getPlanColorScheme(plan: SubscriptionPlan): string {
  switch (plan) {
    case SubscriptionPlan.Free:       return "gray";
    case SubscriptionPlan.Standard:   return "blue";
    case SubscriptionPlan.Premium:    return "purple";
    case SubscriptionPlan.Enterprise: return "orange";
  }
}

function getStatusColorScheme(status: SubscriptionStatus): string {
  switch (status) {
    case SubscriptionStatus.Active:      return "green";
    case SubscriptionStatus.Trialing:    return "yellow";
    case SubscriptionStatus.GracePeriod: return "yellow";
    case SubscriptionStatus.PastDue:     return "red";
    case SubscriptionStatus.Canceled:    return "red";
  }
}

function formatDate(value: string | null): string {
  if (!value) return "—";
  return new Date(value).toLocaleDateString("pl-PL");
}

// ── Karta bieżącej subskrypcji ─────────────────────────────────────────────

interface CurrentSubscriptionCardProps {
  subscription: TenantSubscriptionInfo;
  tenantId: string;
  availablePlans: SubscriptionPlanInfo[];
}

function CurrentSubscriptionCard({
  subscription,
  tenantId,
  availablePlans,
}: CurrentSubscriptionCardProps): React.ReactElement {
  const toast = useToast();
  const { isOpen, onOpen, onClose } = useDisclosure();
  const { mutate: changePlan, isPending } = useRequestPlanChange();
  const { mutate: processPayment } = useProcessMockPayment();
  const [selectedPlan, setSelectedPlan] = React.useState<SubscriptionPlan>(
    subscription.plan,
  );

  React.useEffect(() => {
    setSelectedPlan(subscription.plan);
  }, [subscription.plan]);

  function handleConfirm(): void {
    onClose();
    changePlan(
      { tenantId, data: { plan: selectedPlan } },
      {
        onSuccess: () => {
          toast({
            title: "Plan został zmieniony",
            status: "success",
            duration: 3000,
            isClosable: true,
            position: "top-right",
          });
          if (selectedPlan !== SubscriptionPlan.Free) {
            processPayment(tenantId, {
              onSuccess: (result) => {
                toast({
                  title: "Płatność zakończona",
                  description: `Opłacono ${result.amount.toFixed(2)} ${result.currency}. Aktywny do ${formatDate(result.periodEnd)}.`,
                  status: "success",
                  duration: 6000,
                  isClosable: true,
                  position: "top-right",
                });
              },
              onError: () => {
                toast({
                  title: "Płatność nie powiodła się",
                  description: "Opłać subskrypcję ręcznie w sekcji Billing.",
                  status: "warning",
                  duration: 7000,
                  isClosable: true,
                  position: "top-right",
                });
              },
            });
          }
        },
        onError: () => {
          toast({
            title: "Błąd podczas zmiany planu",
            status: "error",
            duration: 5000,
            isClosable: true,
            position: "top-right",
          });
        },
      },
    );
  }

  const periodEnd = subscription.currentPeriodEnd
    ? formatDate(subscription.currentPeriodEnd)
    : "bezterminowy";

  return (
    <>
      <Card variant="outline">
        <CardHeader pb={2}>
          <Heading size="sm">Bieżąca subskrypcja</Heading>
        </CardHeader>
        <CardBody pt={0}>
          <VStack align="stretch" spacing={3}>
            <HStack justify="space-between">
              <Text fontSize="sm" color="gray.500">Plan</Text>
              <Badge colorScheme={getPlanColorScheme(subscription.plan)}>
                {PlanLabels[subscription.plan]}
              </Badge>
            </HStack>

            <HStack justify="space-between">
              <Text fontSize="sm" color="gray.500">Status</Text>
              <Badge colorScheme={getStatusColorScheme(subscription.status)}>
                {StatusLabels[subscription.status]}
              </Badge>
            </HStack>

            <HStack justify="space-between">
              <Text fontSize="sm" color="gray.500">Limit projektów</Text>
              <Text fontSize="sm" fontWeight="medium">
                {formatLimit(subscription.maxProjects)}
              </Text>
            </HStack>

            <HStack justify="space-between">
              <Text fontSize="sm" color="gray.500">Limit użytkowników</Text>
              <Text fontSize="sm" fontWeight="medium">
                {formatLimit(subscription.maxUsers)}
              </Text>
            </HStack>

            {subscription.isFullAccess && (
              <HStack justify="space-between">
                <Text fontSize="sm" color="gray.500">Full access</Text>
                <Badge colorScheme="green">Aktywny</Badge>
              </HStack>
            )}

            <HStack justify="space-between">
              <Text fontSize="sm" color="gray.500">Okres</Text>
              <Text fontSize="sm" fontWeight="medium">
                {formatDate(subscription.currentPeriodStart)} — {periodEnd}
              </Text>
            </HStack>

            {subscription.trialEndsAt && (
              <HStack justify="space-between">
                <Text fontSize="sm" color="gray.500">Trial do</Text>
                <Text fontSize="sm" fontWeight="medium">
                  {formatDate(subscription.trialEndsAt)}
                </Text>
              </HStack>
            )}

            {availablePlans.length > 0 && (
              <HStack mt={2} spacing={2}>
                <Select
                  size="sm"
                  value={selectedPlan}
                  onChange={(e) =>
                    setSelectedPlan(Number(e.target.value) as SubscriptionPlan)
                  }
                  flex={1}
                >
                  {availablePlans.map((p) => (
                    <option key={p.plan} value={p.plan}>
                      {p.name}
                    </option>
                  ))}
                </Select>
                <Button
                  size="sm"
                  colorScheme="blue"
                  isLoading={isPending}
                  isDisabled={selectedPlan === subscription.plan}
                  onClick={onOpen}
                >
                  Zmień plan
                </Button>
              </HStack>
            )}
          </VStack>
        </CardBody>
      </Card>

      <ConfirmAlertDialog
        isOpen={isOpen}
        onClose={onClose}
        onConfirm={handleConfirm}
        title="Zmiana planu"
        body={`Czy na pewno chcesz zmienić plan na ${PlanLabels[selectedPlan]}?`}
        confirmLabel="Zmień"
        confirmColorScheme="blue"
        isLoading={isPending}
      />
    </>
  );
}

// ── Karta statusu billingu ─────────────────────────────────────────────────

interface BillingStatusCardProps {
  billingStatus: SubscriptionStatusInfo;
  tenantId: string;
}

function BillingStatusCard({
  billingStatus,
  tenantId,
}: BillingStatusCardProps): React.ReactElement {
  const toast = useToast();
  const { isOpen, onOpen, onClose } = useDisclosure();
  const { mutate: processPayment, isPending } = useProcessMockPayment();

  function handleConfirm(): void {
    onClose();
    processPayment(tenantId, {
      onSuccess: (result) => {
        toast({
          title: "Płatność zakończona sukcesem",
          description: `Opłacono ${result.amount.toFixed(2)} ${result.currency}. Subskrypcja aktywna do ${formatDate(result.periodEnd)}.`,
          status: "success",
          duration: 6000,
          isClosable: true,
          position: "top-right",
        });
      },
      onError: () => {
        toast({
          title: "Błąd podczas płatności",
          status: "error",
          duration: 5000,
          isClosable: true,
          position: "top-right",
        });
      },
    });
  }

  const isFree = billingStatus.plan === SubscriptionPlan.Free;
  const isPastDue = billingStatus.status === SubscriptionStatus.PastDue;
  const isGracePeriod = billingStatus.status === SubscriptionStatus.GracePeriod;
  const canPay = !isFree && !billingStatus.isCurrentPeriodPaid;

  return (
    <>
      <Card variant="outline" borderColor={isPastDue ? "red.300" : isGracePeriod ? "orange.300" : undefined}>
        <CardHeader pb={2}>
          <HStack justify="space-between">
            <Heading size="sm">Billing</Heading>
            {canPay && (
              <Button
                size="sm"
                colorScheme={isPastDue || isGracePeriod ? "orange" : "blue"}
                leftIcon={<CreditCard size={14} />}
                isLoading={isPending}
                onClick={onOpen}
              >
                Opłać subskrypcję
              </Button>
            )}
          </HStack>
        </CardHeader>
        <CardBody pt={0}>
          <VStack align="stretch" spacing={3}>
            {(isPastDue || isGracePeriod) && (
              <Alert status={isPastDue ? "error" : "warning"} borderRadius="md" py={2}>
                <AlertIcon />
                <Text fontSize="sm">
                  {isPastDue
                    ? "Subskrypcja jest przeterminowana. Opłać ją, aby przywrócić dostęp."
                    : `Jesteś w okresie karencji. Opłać do ${formatDate(billingStatus.gracePeriodEndsAt)}.`}
                </Text>
              </Alert>
            )}

            {billingStatus.isCurrentPeriodPaid && !isFree && (
              <Alert status="success" borderRadius="md" py={2}>
                <AlertIcon />
                <Text fontSize="sm">
                  Bieżący okres jest opłacony. Następna płatność: {formatDate(billingStatus.nextPaymentDue)}.
                </Text>
              </Alert>
            )}

            {isFree ? (
              <Text fontSize="sm" color="gray.500">
                Plan Free nie wymaga płatności.
              </Text>
            ) : (
              <>
                <HStack justify="space-between">
                  <Text fontSize="sm" color="gray.500">Cena miesięczna</Text>
                  <Text fontSize="sm" fontWeight="medium">
                    {billingStatus.price.toFixed(2)} {billingStatus.currency}
                  </Text>
                </HStack>

                <Divider />

                <HStack justify="space-between">
                  <Text fontSize="sm" color="gray.500">Następna płatność</Text>
                  <Text fontSize="sm" fontWeight="medium">
                    {formatDate(billingStatus.nextPaymentDue)}
                  </Text>
                </HStack>

                {billingStatus.gracePeriodEndsAt && (
                  <HStack justify="space-between">
                    <Text fontSize="sm" color="gray.500">Koniec karencji</Text>
                    <Text fontSize="sm" fontWeight="medium" color="orange.500">
                      {formatDate(billingStatus.gracePeriodEndsAt)}
                    </Text>
                  </HStack>
                )}

                {billingStatus.lastPaidAt && (
                  <>
                    <Divider />
                    <HStack justify="space-between">
                      <Text fontSize="sm" color="gray.500">Ostatnia płatność</Text>
                      <Text fontSize="sm" fontWeight="medium">
                        {formatDate(billingStatus.lastPaidAt)}
                      </Text>
                    </HStack>
                    <HStack justify="space-between">
                      <Text fontSize="sm" color="gray.500">Kwota</Text>
                      <Text fontSize="sm" fontWeight="medium">
                        {billingStatus.lastPaidAmount?.toFixed(2)} {billingStatus.currency}
                      </Text>
                    </HStack>
                  </>
                )}
              </>
            )}
          </VStack>
        </CardBody>
      </Card>

      <ConfirmAlertDialog
        isOpen={isOpen}
        onClose={onClose}
        onConfirm={handleConfirm}
        title="Opłać subskrypcję"
        body={`Czy chcesz opłacić subskrypcję (${billingStatus.price.toFixed(2)} ${billingStatus.currency})?`}
        confirmLabel="Opłać"
        confirmColorScheme="blue"
        isLoading={isPending}
      />
    </>
  );
}

// ── Tabela historii płatności ──────────────────────────────────────────────

interface PaymentHistoryTableProps {
  payments: SubscriptionPaymentInfo[];
}

function getPaymentStatusColor(statusLabel: string): string {
  switch (statusLabel.toLowerCase()) {
    case "succeeded": return "green";
    case "failed":    return "red";
    case "pending":   return "yellow";
    default:          return "gray";
  }
}

function PaymentHistoryTable({ payments }: PaymentHistoryTableProps): React.ReactElement {
  if (payments.length === 0) {
    return (
      <Card variant="outline">
        <CardHeader pb={2}>
          <Heading size="sm">Historia płatności</Heading>
        </CardHeader>
        <CardBody pt={0}>
          <Text fontSize="sm" color="gray.500">Brak płatności do wyświetlenia.</Text>
        </CardBody>
      </Card>
    );
  }

  return (
    <Card variant="outline">
      <CardHeader pb={2}>
        <Heading size="sm">Historia płatności</Heading>
      </CardHeader>
      <CardBody pt={0} px={0}>
        <Box overflowX="auto">
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th>Data</Th>
                <Th>Plan</Th>
                <Th>Okres</Th>
                <Th isNumeric>Kwota</Th>
                <Th>Status</Th>
              </Tr>
            </Thead>
            <Tbody>
              {payments.map((p) => (
                <Tr key={p.id}>
                  <Td>
                    <Text fontSize="sm">{p.paidAt ? formatDate(p.paidAt) : formatDate(p.createdAt)}</Text>
                  </Td>
                  <Td>
                    <Badge colorScheme={getPlanColorScheme(p.plan)}>{p.planName}</Badge>
                  </Td>
                  <Td>
                    <Text fontSize="sm" whiteSpace="nowrap">
                      {formatDate(p.periodStart)} — {formatDate(p.periodEnd)}
                    </Text>
                  </Td>
                  <Td isNumeric>
                    <Text fontSize="sm" fontWeight="medium">
                      {p.amount.toFixed(2)} {p.currency}
                    </Text>
                  </Td>
                  <Td>
                    <Badge colorScheme={getPaymentStatusColor(p.statusLabel)}>
                      {p.statusLabel}
                    </Badge>
                    {p.failureReason && (
                      <Text fontSize="xs" color="red.500" mt={1}>{p.failureReason}</Text>
                    )}
                  </Td>
                </Tr>
              ))}
            </Tbody>
          </Table>
        </Box>
      </CardBody>
    </Card>
  );
}

// ── Tabela dostępnych planów ────────────────────────────────────────────────

interface AvailablePlansTableProps {
  plans: SubscriptionPlanInfo[];
  currentPlan: SubscriptionPlan;
}

function AvailablePlansTable({
  plans,
  currentPlan,
}: AvailablePlansTableProps): React.ReactElement {
  return (
    <Card variant="outline">
      <CardHeader pb={2}>
        <Heading size="sm">Dostępne plany</Heading>
      </CardHeader>
      <CardBody pt={0} px={0}>
        <Box overflowX="auto">
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th>Plan</Th>
                <Th isNumeric>Max projektów</Th>
                <Th isNumeric>Max użytkowników</Th>
                <Th isNumeric>Cena</Th>
                <Th></Th>
              </Tr>
            </Thead>
            <Tbody>
              {plans.map((p) => (
                <Tr key={p.plan} bg={p.plan === currentPlan ? "blue.50" : undefined}>
                  <Td>
                    <HStack spacing={2}>
                      <Badge colorScheme={getPlanColorScheme(p.plan)}>
                        {p.name}
                      </Badge>
                    </HStack>
                  </Td>
                  <Td isNumeric>
                    <Text fontSize="sm">{formatLimit(p.maxProjects)}</Text>
                  </Td>
                  <Td isNumeric>
                    <Text fontSize="sm">{formatLimit(p.maxUsers)}</Text>
                  </Td>
                  <Td isNumeric>
                    <Text fontSize="sm">
                      {p.price === 0
                        ? "Bezpłatny"
                        : `${p.price.toFixed(2)} ${p.currency}`}
                    </Text>
                  </Td>
                  <Td>
                    {p.plan === currentPlan && (
                      <Badge colorScheme="green" variant="subtle">
                        Aktualny
                      </Badge>
                    )}
                  </Td>
                </Tr>
              ))}
            </Tbody>
          </Table>
        </Box>
      </CardBody>
    </Card>
  );
}

// ── Strona główna ──────────────────────────────────────────────────────────

export default function TenantSubscriptionPage(): React.ReactElement {
  const { tenantId } = useParams<{ tenantId: string }>();
  const navigate = useNavigate();

  const {
    data: subscription,
    isLoading: isLoadingSubscription,
    isError: isErrorSubscription,
  } = useMyTenantSubscription(tenantId ?? null);

  const { data: plans = [], isLoading: isLoadingPlans } = useSubscriptionPlans(
    tenantId ?? null,
  );

  const { data: billingStatus } = useSubscriptionStatus(tenantId ?? null);
  const { data: payments = [] } = usePaymentHistory(tenantId ?? null);

  const isLoading = isLoadingSubscription || isLoadingPlans;

  if (!tenantId) {
    return (
      <MainLayout>
        <Box p={{ base: 4, md: 8 }}>
          <Alert status="warning" borderRadius="md">
            <AlertIcon />
            Brak identyfikatora organizacji w adresie URL.
          </Alert>
        </Box>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 8 }} maxW="2xl">
        <HStack mb={6} spacing={3}>
          <Button
            variant="ghost"
            size="sm"
            leftIcon={<ArrowLeft size={16} />}
            onClick={() => navigate(`/tenants/${tenantId}`)}
          >
            Powrót do organizacji
          </Button>
        </HStack>

        <Heading size="lg" mb={1}>
          Subskrypcja
        </Heading>
        <Text color="gray.500" mb={6} fontSize="sm">
          Zarządzaj planem subskrypcji swojej organizacji
        </Text>

        {isLoading && (
          <Box py={10} textAlign="center">
            <Spinner color="primary.500" />
          </Box>
        )}

        {isErrorSubscription && !isLoading && (
          <Alert status="warning" borderRadius="md">
            <AlertIcon />
            Nie znaleziono subskrypcji dla tej organizacji.
          </Alert>
        )}

        {!isLoading && !isErrorSubscription && subscription && (
          <VStack spacing={6} align="stretch">
            <CurrentSubscriptionCard
              subscription={subscription}
              tenantId={tenantId}
              availablePlans={plans}
            />

            {billingStatus && (
              <BillingStatusCard
                billingStatus={billingStatus}
                tenantId={tenantId}
              />
            )}

            <PaymentHistoryTable payments={payments} />

            {plans.length > 0 && (
              <AvailablePlansTable
                plans={plans}
                currentPlan={subscription.plan}
              />
            )}
          </VStack>
        )}
      </Box>
    </MainLayout>
  );
}
