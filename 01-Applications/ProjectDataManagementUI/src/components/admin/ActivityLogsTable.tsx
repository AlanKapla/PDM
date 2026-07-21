import React from "react";
import {
  Badge,
  Table,
  TableContainer,
  Tbody,
  Td,
  Text,
  Th,
  Thead,
  Tr,
} from "@chakra-ui/react";
import type { UserActivityLogWeb } from "../../types/activity.types";
import { UserActivityEventType } from "../../types/activity.types";
import { formatDate } from "../../utils/formatters";

export interface ActivityLogsTableProps {
  items: UserActivityLogWeb[];
}

function formatEventTypeLabel(eventType: string): string {
  if (eventType === UserActivityEventType.Login) {
    return "Logowanie";
  }
  if (eventType === UserActivityEventType.DemoEnter) {
    return "Wejście w demo";
  }
  return eventType;
}

function eventTypeColorScheme(eventType: string): string {
  if (eventType === UserActivityEventType.Login) {
    return "blue";
  }
  if (eventType === UserActivityEventType.DemoEnter) {
    return "purple";
  }
  return "gray";
}

function formatUserCell(log: UserActivityLogWeb): string {
  if (log.userId) {
    return log.userId;
  }
  if (log.azureAdB2CObjectId) {
    const oid: string = log.azureAdB2CObjectId;
    if (oid.length > 12) {
      return `${oid.slice(0, 8)}…`;
    }
    return oid;
  }
  return "—";
}

export function ActivityLogsTable({
  items,
}: ActivityLogsTableProps): React.ReactElement {
  return (
    <TableContainer>
      <Table variant="simple" size="sm">
        <Thead>
          <Tr>
            <Th scope="col">Data</Th>
            <Th scope="col">Typ</Th>
            <Th scope="col">IP</Th>
            <Th scope="col">Route</Th>
            <Th scope="col">Użytkownik</Th>
          </Tr>
        </Thead>
        <Tbody>
          {items.map((item: UserActivityLogWeb) => (
            <Tr key={item.id}>
              <Td>
                <Text fontSize="sm" color="neutral.700">
                  {formatDate(item.occurredAtUtc)}
                </Text>
              </Td>
              <Td>
                <Badge colorScheme={eventTypeColorScheme(item.eventType)}>
                  {formatEventTypeLabel(item.eventType)}
                </Badge>
              </Td>
              <Td>
                <Text fontSize="sm" color="neutral.700">
                  {item.ipAddress}
                </Text>
              </Td>
              <Td>
                <Text
                  fontSize="sm"
                  color="neutral.700"
                  noOfLines={1}
                  maxW="280px"
                >
                  {item.route ?? "—"}
                </Text>
              </Td>
              <Td>
                <Text
                  fontSize="sm"
                  color="neutral.700"
                  fontFamily="mono"
                  noOfLines={1}
                  maxW="220px"
                  title={item.userId ?? item.azureAdB2CObjectId ?? undefined}
                >
                  {formatUserCell(item)}
                </Text>
              </Td>
            </Tr>
          ))}
        </Tbody>
      </Table>
    </TableContainer>
  );
}
