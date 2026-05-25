import React from "react";
import {
  Card,
  CardBody,
  CardHeader,
  Heading,
  HStack,
  VStack,
  Text,
  Badge,
  Select,
  Button,
  useToast,
} from "@chakra-ui/react";
import {
  PlanLabels,
  StatusLabels,
  formatLimit,
  SubscriptionPlan,
  SubscriptionStatus,
  type TenantSubscription,
} from "../../../../types/subscription";
import { useChangeTenantPlan } from "../../../../hooks/queries";

interface TenantSubscriptionCardProps {
  subscription: TenantSubscription;
  tenantId: string;
}

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

export function TenantSubscriptionCard({
  subscription,
  tenantId,
}: TenantSubscriptionCardProps): React.ReactElement {
  const toast = useToast();
  const { mutate: changePlan, isPending } = useChangeTenantPlan();
  const [selectedPlan, setSelectedPlan] = React.useState<SubscriptionPlan>(
    subscription.plan,
  );

  React.useEffect(() => {
    setSelectedPlan(subscription.plan);
  }, [subscription.plan]);

  function handleChangePlan(): void {
    changePlan(
      { tenantId, plan: selectedPlan },
      {
        onSuccess: () => {
          toast({
            title: "Plan zmieniony",
            status: "success",
            duration: 3000,
            isClosable: true,
            position: "top-right",
          });
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
    <Card variant="outline">
      <CardHeader pb={2}>
        <Heading size="sm">Subskrypcja</Heading>
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
            <Text fontSize="sm" color="gray.500">Max projektów</Text>
            <Text fontSize="sm" fontWeight="medium">
              {formatLimit(subscription.maxProjects)}
            </Text>
          </HStack>

          <HStack justify="space-between">
            <Text fontSize="sm" color="gray.500">Max użytkowników</Text>
            <Text fontSize="sm" fontWeight="medium">
              {formatLimit(subscription.maxUsers)}
            </Text>
          </HStack>

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

          <HStack mt={2} spacing={2}>
            <Select
              size="sm"
              value={selectedPlan}
              onChange={(e) => setSelectedPlan(Number(e.target.value) as SubscriptionPlan)}
              flex={1}
            >
              {(Object.values(SubscriptionPlan).filter(
                (v) => typeof v === "number",
              ) as SubscriptionPlan[]).map((plan) => (
                <option key={plan} value={plan}>
                  {PlanLabels[plan]}
                </option>
              ))}
            </Select>
            <Button
              size="sm"
              colorScheme="blue"
              isLoading={isPending}
              onClick={handleChangePlan}
              isDisabled={selectedPlan === subscription.plan}
            >
              Zmień plan
            </Button>
          </HStack>
        </VStack>
      </CardBody>
    </Card>
  );
}
