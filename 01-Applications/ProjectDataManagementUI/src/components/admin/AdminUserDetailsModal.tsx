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
import { useAdminUserDetails } from "../../hooks/queries";
import type { AdminUserDetailsWeb } from "../../types/admin.types";

interface AdminUserDetailsModalProps {
  userId: string | null;
  isOpen: boolean;
  onClose: () => void;
}

function DetailRow({
  label,
  value,
}: {
  label: string;
  value: string | null | undefined;
}): React.ReactElement {
  return (
    <HStack justify="space-between" py={1}>
      <Text fontSize="sm" color="gray.500" minW="140px">
        {label}
      </Text>
      <Text fontSize="sm" fontWeight="medium" textAlign="right">
        {value ?? "—"}
      </Text>
    </HStack>
  );
}

function UserDetailsContent({
  data,
}: {
  data: AdminUserDetailsWeb;
}): React.ReactElement {
  return (
    <VStack align="stretch" spacing={4}>
      {/* Podstawowe */}
      <Box>
        <Text fontSize="xs" fontWeight="bold" color="gray.400" mb={2} textTransform="uppercase">
          Informacje podstawowe
        </Text>
        <DetailRow label="Imię" value={data.firstName} />
        <DetailRow label="Nazwisko" value={data.lastName} />
        <DetailRow label="Email" value={data.email} />
        <HStack justify="space-between" py={1}>
          <Text fontSize="sm" color="gray.500" minW="140px">Rola systemowa</Text>
          <Badge colorScheme={data.systemRole === "SuperAdmin" ? "red" : "gray"}>
            {data.systemRole}
          </Badge>
        </HStack>
        <HStack justify="space-between" py={1}>
          <Text fontSize="sm" color="gray.500" minW="140px">Status</Text>
          <Badge colorScheme={data.isActive ? "green" : "red"}>
            {data.isActive ? "Aktywny" : "Nieaktywny"}
          </Badge>
        </HStack>
        <DetailRow
          label="Data rejestracji"
          value={new Date(data.createdAt).toLocaleDateString("pl-PL")}
        />
      </Box>

      <Divider />

      {/* Kontaktowe */}
      <Box>
        <Text fontSize="xs" fontWeight="bold" color="gray.400" mb={2} textTransform="uppercase">
          Dane kontaktowe
        </Text>
        <DetailRow label="Telefon" value={data.phoneNumber} />
        <DetailRow label="Firma" value={data.companyName} />
        <DetailRow label="NIP" value={data.taxId} />
      </Box>

      <Divider />

      {/* Adresowe */}
      <Box>
        <Text fontSize="xs" fontWeight="bold" color="gray.400" mb={2} textTransform="uppercase">
          Adres
        </Text>
        <DetailRow label="Ulica" value={data.street} />
        <DetailRow label="Miasto" value={data.city} />
        <DetailRow label="Kod pocztowy" value={data.postalCode} />
        <DetailRow label="Kraj" value={data.country} />
      </Box>

      {data.tenantMemberships.length > 0 && (
        <>
          <Divider />
          <Box>
            <Text fontSize="xs" fontWeight="bold" color="gray.400" mb={2} textTransform="uppercase">
              Organizacje ({data.tenantMemberships.length})
            </Text>
            <Box overflowX="auto">
              <Table size="sm" variant="simple">
                <Thead>
                  <Tr>
                    <Th>Organizacja</Th>
                    <Th>Rola</Th>
                    <Th>Dołączył</Th>
                  </Tr>
                </Thead>
                <Tbody>
                  {data.tenantMemberships.map((m) => (
                    <Tr key={m.tenantId}>
                      <Td>{m.tenantName}</Td>
                      <Td>
                        <Badge colorScheme="primary" variant="subtle">
                          {m.roleName}
                        </Badge>
                      </Td>
                      <Td>
                        <Text fontSize="xs" color="gray.500">
                          {new Date(m.joinedAt).toLocaleDateString("pl-PL")}
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
    </VStack>
  );
}

export function AdminUserDetailsModal({
  userId,
  isOpen,
  onClose,
}: AdminUserDetailsModalProps): React.ReactElement {
  const { data, isLoading } = useAdminUserDetails(userId);

  const title = data
    ? `${data.firstName} ${data.lastName}`
    : "Szczegóły użytkownika";

  return (
    <AppModal
      isOpen={isOpen}
      onClose={onClose}
      title={title}
      hideFooter
      desktopSize="xl"
    >
      {isLoading && (
        <VStack align="stretch" spacing={3}>
          <Skeleton h="20px" />
          <Skeleton h="20px" />
          <Skeleton h="20px" />
          <Skeleton h="20px" />
        </VStack>
      )}
      {!isLoading && data && <UserDetailsContent data={data} />}
    </AppModal>
  );
}
