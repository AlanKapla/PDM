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
  Button,
} from "@chakra-ui/react";
import { Search } from "lucide-react";
import { useNavigate } from "react-router-dom";
import type { AdminTenantListItemWeb } from "../../types/admin.types";

interface AdminTenantTableProps {
  tenants: AdminTenantListItemWeb[];
  onViewDetails: (tenantId: string) => void;
}

export function AdminTenantTable({
  tenants,
  onViewDetails,
}: AdminTenantTableProps): React.ReactElement {
  const navigate = useNavigate();
  const [search, setSearch] = React.useState("");

  const filtered = tenants.filter((t) =>
    t.name.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <Box>
      <InputGroup mb={4} maxW="400px">
        <InputLeftElement pointerEvents="none">
          <Search size={16} />
        </InputLeftElement>
        <Input
          placeholder="Szukaj organizacji..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </InputGroup>

      <Box overflowX="auto">
        <Table size="sm" variant="simple">
          <Thead>
            <Tr>
              <Th>Nazwa</Th>
              <Th>Status</Th>
              <Th isNumeric>Członkowie</Th>
              <Th isNumeric>Projekty</Th>
              <Th>Data utworzenia</Th>
              <Th />
            </Tr>
          </Thead>
          <Tbody>
            {filtered.length === 0 && (
              <Tr>
                <Td colSpan={5}>
                  <Text color="gray.500" textAlign="center" py={4}>
                    Brak wyników
                  </Text>
                </Td>
              </Tr>
            )}
            {filtered.map((tenant) => (
              <Tr
                key={tenant.id}
                onClick={() => onViewDetails(tenant.id)}
                cursor="pointer"
                _hover={{ bg: "neutral.50" }}
              >
                <Td>
                  <Text fontWeight="medium">{tenant.name}</Text>
                </Td>
                <Td>
                  <Badge colorScheme={tenant.isActive ? "green" : "red"}>
                    {tenant.isActive ? "Aktywna" : "Nieaktywna"}
                  </Badge>
                </Td>
                <Td isNumeric>
                  <Text fontSize="sm">{tenant.memberCount}</Text>
                </Td>
                <Td isNumeric>
                  <Text fontSize="sm">{tenant.projectCount}</Text>
                </Td>
                <Td>
                  <Text fontSize="sm" color="gray.500">
                    {new Date(tenant.createdAt).toLocaleDateString("pl-PL")}
                  </Text>
                </Td>
                <Td>
                  <Button
                    size="xs"
                    variant="ghost"
                    colorScheme="blue"
                    onClick={(e) => {
                      e.stopPropagation();
                      navigate(`/admin/subscriptions/tenants/${tenant.id}`);
                    }}
                  >
                    Subskrypcja
                  </Button>
                </Td>
              </Tr>
            ))}
          </Tbody>
        </Table>
      </Box>
    </Box>
  );
}
