import React from "react";
import {
  Box,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  Badge,
  Text,
  Card,
  CardHeader,
  CardBody,
  Heading,
} from "@chakra-ui/react";
import type { SubscriptionPaymentInfo } from "../../../../types/subscription";
import { SubscriptionPlan } from "../../../../types/subscription";

interface PaymentHistoryTableProps {
  payments: SubscriptionPaymentInfo[];
}

function getPlanColorScheme(plan: SubscriptionPlan): string {
  switch (plan) {
    case SubscriptionPlan.Free:       return "gray";
    case SubscriptionPlan.Standard:   return "blue";
    case SubscriptionPlan.Premium:    return "purple";
    case SubscriptionPlan.Enterprise: return "orange";
  }
}

function getPaymentStatusColor(statusLabel: string): string {
  switch (statusLabel.toLowerCase()) {
    case "succeeded": return "green";
    case "failed":    return "red";
    case "pending":   return "yellow";
    default:          return "gray";
  }
}

function formatDate(value: string | null): string {
  if (!value) return "—";
  return new Date(value).toLocaleDateString("pl-PL");
}

export function PaymentHistoryTable({ payments }: PaymentHistoryTableProps): React.ReactElement {
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
                    <Text fontSize="sm">
                      {p.paidAt ? formatDate(p.paidAt) : formatDate(p.createdAt)}
                    </Text>
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
