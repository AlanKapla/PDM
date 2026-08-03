import React, { useState } from "react";
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
  useDisclosure,
} from "@chakra-ui/react";
import type { ColdMailHistoryWeb } from "../../types/admin.types";
import { formatDate } from "../../utils/formatters";
import { ColdMailHistoryDetailsModal } from "./ColdMailHistoryDetailsModal";
import { formatColdMailStatus, coldMailStatusColorScheme } from "./coldMailStatus";

export interface ColdMailHistoryTableProps {
  items: ColdMailHistoryWeb[];
}

export function ColdMailHistoryTable({
  items,
}: ColdMailHistoryTableProps): React.ReactElement {
  const detailsDisclosure = useDisclosure();
  const [selectedItem, setSelectedItem] = useState<ColdMailHistoryWeb | null>(
    null
  );

  const handleOpenDetails = (item: ColdMailHistoryWeb): void => {
    setSelectedItem(item);
    detailsDisclosure.onOpen();
  };

  const handleCloseDetails = (): void => {
    detailsDisclosure.onClose();
    setSelectedItem(null);
  };

  return (
    <>
      <TableContainer>
        <Table variant="simple" size="sm">
          <Thead>
            <Tr>
              <Th scope="col">Odbiorca</Th>
              <Th scope="col">Temat</Th>
              <Th scope="col">Status</Th>
              <Th scope="col">Data</Th>
            </Tr>
          </Thead>
          <Tbody>
            {items.map((item: ColdMailHistoryWeb) => {
              const statusLabel: string = formatColdMailStatus(item.status);
              const statusColor: string = coldMailStatusColorScheme(item.status);

              return (
                <Tr
                  key={item.id}
                  cursor="pointer"
                  _hover={{ bg: "neutral.50" }}
                  onClick={() => handleOpenDetails(item)}
                  tabIndex={0}
                  onKeyDown={(e: React.KeyboardEvent<HTMLTableRowElement>) => {
                    if (e.key === "Enter" || e.key === " ") {
                      e.preventDefault();
                      handleOpenDetails(item);
                    }
                  }}
                  aria-label={`Szczegóły cold maila do ${item.recipientEmail}`}
                >
                  <Td>
                    <Text fontWeight="medium" color="neutral.800">
                      {item.recipientEmail}
                    </Text>
                  </Td>
                  <Td>
                    <Text
                      fontSize="sm"
                      color="neutral.700"
                      noOfLines={1}
                      maxW="320px"
                    >
                      {item.subject}
                    </Text>
                  </Td>
                  <Td>
                    <Badge colorScheme={statusColor}>{statusLabel}</Badge>
                  </Td>
                  <Td>
                    <Text fontSize="sm" color="neutral.700">
                      {formatDate(item.sentAt)}
                    </Text>
                  </Td>
                </Tr>
              );
            })}
          </Tbody>
        </Table>
      </TableContainer>

      <ColdMailHistoryDetailsModal
        item={selectedItem}
        isOpen={detailsDisclosure.isOpen}
        onClose={handleCloseDetails}
      />
    </>
  );
}
