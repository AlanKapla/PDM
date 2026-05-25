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
  Input,
  InputGroup,
  InputLeftElement,
} from "@chakra-ui/react";
import { Search } from "lucide-react";
import type { AdminUserListItemWeb } from "../../types/admin.types";

interface AdminUserTableProps {
  users: AdminUserListItemWeb[];
  onViewDetails: (userId: string) => void;
}

function getSystemRoleBadgeColor(role: string): string {
  switch (role) {
    case "SuperAdmin":
      return "red";
    case "Support":
      return "orange";
    default:
      return "gray";
  }
}

export function AdminUserTable({
  users,
  onViewDetails,
}: AdminUserTableProps): React.ReactElement {
  const [search, setSearch] = React.useState("");

  const filtered = users.filter(
    (u) =>
      u.firstName.toLowerCase().includes(search.toLowerCase()) ||
      u.lastName.toLowerCase().includes(search.toLowerCase()) ||
      u.email.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <Box>
      <InputGroup mb={4} maxW="400px">
        <InputLeftElement pointerEvents="none">
          <Search size={16} />
        </InputLeftElement>
        <Input
          placeholder="Szukaj użytkownika..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </InputGroup>

      <Box overflowX="auto">
        <Table size="sm" variant="simple">
          <Thead>
            <Tr>
              <Th>Imię i nazwisko</Th>
              <Th>Email</Th>
              <Th>Rola systemowa</Th>
              <Th>Status</Th>
              <Th>Organizacje</Th>
              <Th>Data rejestracji</Th>
            </Tr>
          </Thead>
          <Tbody>
            {filtered.length === 0 && (
              <Tr>
                <Td colSpan={7}>
                  <Text color="gray.500" textAlign="center" py={4}>
                    Brak wyników
                  </Text>
                </Td>
              </Tr>
            )}
            {filtered.map((user) => (
              <Tr
                key={user.id}
                onClick={() => onViewDetails(user.id)}
                cursor="pointer"
                _hover={{ bg: "neutral.50" }}
              >
                <Td>
                  <Text fontWeight="medium">
                    {user.firstName} {user.lastName}
                  </Text>
                </Td>
                <Td>
                  <Text fontSize="sm" color="gray.600">
                    {user.email}
                  </Text>
                </Td>
                <Td>
                  <Badge colorScheme={getSystemRoleBadgeColor(user.systemRole)}>
                    {user.systemRole}
                  </Badge>
                </Td>
                <Td>
                  <Badge colorScheme={user.isActive ? "green" : "red"}>
                    {user.isActive ? "Aktywny" : "Nieaktywny"}
                  </Badge>
                </Td>
                <Td>
                  <Text fontSize="sm">{user.tenantCount}</Text>
                </Td>
                <Td>
                  <Text fontSize="sm" color="gray.500">
                    {new Date(user.createdAt).toLocaleDateString("pl-PL")}
                  </Text>
                </Td>
              </Tr>
            ))}
          </Tbody>
        </Table>
      </Box>
    </Box>
  );
}
