import React from "react";
import {
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  Badge,
  Text,
  Box,
} from "@chakra-ui/react";
import type { SubscriptionPlanDefinitionWeb } from "../../types/admin.types";

interface AdminSubscriptionPlansTableProps {
  plans: SubscriptionPlanDefinitionWeb[];
  onEdit: (plan: SubscriptionPlanDefinitionWeb) => void;
}

function formatLimit(value: number): string {
  return value === -1 ? "Bez limitu" : value.toString();
}

export function AdminSubscriptionPlansTable({
  plans,
  onEdit,
}: AdminSubscriptionPlansTableProps): React.ReactElement {
  return (
    <Box overflowX="auto">
      <Table size="sm" variant="simple">
        <Thead>
          <Tr>
            <Th>Plan</Th>
            <Th>Nazwa</Th>
            <Th isNumeric>Max projektów</Th>
            <Th isNumeric>Max użytkowników</Th>
            <Th isNumeric>Cena</Th>
            <Th>Waluta</Th>
            <Th>Status</Th>
          </Tr>
        </Thead>
        <Tbody>
          {plans.map((plan) => (
            <Tr
              key={plan.id}
              onClick={() => onEdit(plan)}
              cursor="pointer"
              _hover={{ bg: "neutral.50" }}
            >
              <Td>
                <Text fontWeight="semibold">{plan.plan}</Text>
              </Td>
              <Td>{plan.name}</Td>
              <Td isNumeric>{formatLimit(plan.maxProjects)}</Td>
              <Td isNumeric>{formatLimit(plan.maxUsers)}</Td>
              <Td isNumeric>{plan.price.toFixed(2)}</Td>
              <Td>{plan.currency}</Td>
              <Td>
                <Badge colorScheme={plan.isActive ? "green" : "gray"}>
                  {plan.isActive ? "Aktywny" : "Nieaktywny"}
                </Badge>
              </Td>
            </Tr>
          ))}
        </Tbody>
      </Table>
    </Box>
  );
}
