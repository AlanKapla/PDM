import React from "react";
import {
  VStack,
  HStack,
  Text,
  Badge,
  Divider,
  Box,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  Skeleton,
} from "@chakra-ui/react";
import AppModal from "../ui/AppModal";
import { useAdminTenantDetails } from "../../hooks/queries";
import type { AdminTenantDetailsWeb } from "../../types/admin.types";

interface AdminTenantDetailsModalProps {
  tenantId: string | null;
  isOpen: boolean;
  onClose: () => void;
}

function TenantDetailsContent({
  data,
}: {
  data: AdminTenantDetailsWeb;
}): React.ReactElement {
  return (
    <VStack align="stretch" spacing={4}>
      {/* Podstawowe */}
      <Box>
        <Text fontSize="xs" fontWeight="bold" color="gray.400" mb={2} textTransform="uppercase">
          Informacje podstawowe
        </Text>
        <HStack justify="space-between" py={1}>
          <Text fontSize="sm" color="gray.500" minW="140px">Nazwa</Text>
          <Text fontSize="sm" fontWeight="medium">{data.name}</Text>
        </HStack>
        <HStack justify="space-between" py={1}>
          <Text fontSize="sm" color="gray.500" minW="140px">Status</Text>
          <Badge colorScheme={data.isActive ? "green" : "red"}>
            {data.isActive ? "Aktywna" : "Nieaktywna"}
          </Badge>
        </HStack>
        <HStack justify="space-between" py={1}>
          <Text fontSize="sm" color="gray.500" minW="140px">Data utworzenia</Text>
          <Text fontSize="sm" fontWeight="medium">
            {new Date(data.createdAt).toLocaleDateString("pl-PL")}
          </Text>
        </HStack>
        <HStack justify="space-between" py={1}>
          <Text fontSize="sm" color="gray.500" minW="140px">Członkowie</Text>
          <Text fontSize="sm" fontWeight="medium">{data.memberCount}</Text>
        </HStack>
      </Box>

      {data.projects.length > 0 && (
        <>
          <Divider />
          <Box>
            <Text fontSize="xs" fontWeight="bold" color="gray.400" mb={2} textTransform="uppercase">
              Projekty ({data.projects.length})
            </Text>
            <Box overflowX="auto">
              <Table size="sm" variant="simple">
                <Thead>
                  <Tr>
                    <Th>Nazwa</Th>
                    <Th>Status</Th>
                    <Th isNumeric>Członkowie</Th>
                    <Th isNumeric>Budżet netto</Th>
                    <Th>Utworzony</Th>
                  </Tr>
                </Thead>
                <Tbody>
                  {data.projects.map((p) => (
                    <Tr key={p.id}>
                      <Td>
                        <Text fontWeight="medium">{p.name}</Text>
                      </Td>
                      <Td>
                        <Badge colorScheme={p.isActive ? "green" : "red"}>
                          {p.isActive ? "Aktywny" : "Nieaktywny"}
                        </Badge>
                      </Td>
                      <Td isNumeric>
                        <Text fontSize="sm">{p.memberCount}</Text>
                      </Td>
                      <Td isNumeric>
                        <Text fontSize="sm">
                          {p.budgetNet != null
                            ? new Intl.NumberFormat("pl-PL", {
                                style: "currency",
                                currency: "PLN",
                              }).format(p.budgetNet)
                            : "—"}
                        </Text>
                      </Td>
                      <Td>
                        <Text fontSize="sm" color="gray.500">
                          {new Date(p.createdAt).toLocaleDateString("pl-PL")}
                        </Text>
                      </Td>
                    </Tr>
                  ))}
                </Tbody>
              </Table>
            </Box>
          </Box>
        </>
      )}

      {data.projects.length === 0 && (
        <Text fontSize="sm" color="gray.500" textAlign="center" py={2}>
          Brak projektów
        </Text>
      )}
    </VStack>
  );
}

export function AdminTenantDetailsModal({
  tenantId,
  isOpen,
  onClose,
}: AdminTenantDetailsModalProps): React.ReactElement {
  const { data, isLoading } = useAdminTenantDetails(tenantId);

  return (
    <AppModal
      isOpen={isOpen}
      onClose={onClose}
      title={data?.name ?? "Szczegóły organizacji"}
      desktopSize="xl"
      hideFooter
    >
      {isLoading ? (
        <VStack align="stretch" spacing={3}>
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} h="24px" borderRadius="md" />
          ))}
        </VStack>
      ) : data ? (
        <TenantDetailsContent data={data} />
      ) : null}
    </AppModal>
  );
}
