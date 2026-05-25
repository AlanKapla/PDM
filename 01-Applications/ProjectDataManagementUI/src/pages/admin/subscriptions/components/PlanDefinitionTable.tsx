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
  Button,
} from "@chakra-ui/react";
import {
  formatLimit,
  PlanLabels,
  type PlanDefinition,
} from "../../../../types/subscription";

interface PlanDefinitionTableProps {
  plans: PlanDefinition[];
  onEdit: (plan: PlanDefinition) => void;
}

export function PlanDefinitionTable({
  plans,
  onEdit,
}: PlanDefinitionTableProps): React.ReactElement {
  return (
    <Box overflowX="auto">
      <Table size="sm" variant="simple">
        <Thead>
          <Tr>
            <Th>Plan</Th>
            <Th>Nazwa</Th>
            <Th isNumeric>Max projektów</Th>
            <Th isNumeric>Max userów</Th>
            <Th isNumeric>Cena</Th>
            <Th>Waluta</Th>
            <Th>Aktywny</Th>
            <Th />
          </Tr>
        </Thead>
        <Tbody>
          {plans.length === 0 && (
            <Tr>
              <Td colSpan={8}>
                <Text color="gray.500" textAlign="center" py={4}>
                  Brak planów
                </Text>
              </Td>
            </Tr>
          )}
          {plans.map((plan) => (
            <Tr key={plan.id}>
              <Td>
                <Text fontWeight="semibold">{PlanLabels[plan.plan]}</Text>
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
              <Td>
                <Button size="xs" variant="outline" onClick={() => onEdit(plan)}>
                  Edytuj
                </Button>
              </Td>
            </Tr>
          ))}
        </Tbody>
      </Table>
    </Box>
  );
}
