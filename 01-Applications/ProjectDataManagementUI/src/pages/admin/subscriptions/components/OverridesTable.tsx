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
  useDisclosure,
  useToast,
} from "@chakra-ui/react";
import ConfirmAlertDialog from "../../../../components/ui/ConfirmAlertDialog";
import { useDeactivateSubscriptionOverride } from "../../../../hooks/queries";
import type { SubscriptionOverride } from "../../../../types/subscription";

interface OverridesTableProps {
  overrides: SubscriptionOverride[];
  tenantId: string;
}

function formatDateTime(value: string | null): string {
  if (!value) return "—";
  return new Date(value).toLocaleString("pl-PL");
}

export function OverridesTable({
  overrides,
  tenantId,
}: OverridesTableProps): React.ReactElement {
  const toast = useToast();
  const { isOpen, onOpen, onClose } = useDisclosure();
  const [pendingOverrideId, setPendingOverrideId] = React.useState<string | null>(null);

  const { mutate: deactivate, isPending } = useDeactivateSubscriptionOverride();

  function handleDeactivateClick(overrideId: string): void {
    setPendingOverrideId(overrideId);
    onOpen();
  }

  function handleConfirm(): void {
    if (!pendingOverrideId) return;
    onClose();

    deactivate(
      { tenantId, overrideId: pendingOverrideId },
      {
        onSuccess: () => {
          setPendingOverrideId(null);
          toast({
            title: "Override dezaktywowany",
            status: "success",
            duration: 3000,
            isClosable: true,
            position: "top-right",
          });
        },
        onError: () => {
          toast({
            title: "Błąd podczas dezaktywacji",
            status: "error",
            duration: 5000,
            isClosable: true,
            position: "top-right",
          });
        },
      },
    );
  }

  function handleDialogClose(): void {
    onClose();
    setPendingOverrideId(null);
  }

  return (
    <>
      <Box overflowX="auto">
        <Table size="sm" variant="simple">
          <Thead>
            <Tr>
              <Th>Klucz</Th>
              <Th>Wartość</Th>
              <Th>Powód</Th>
              <Th>Wygasa</Th>
              <Th>Ważny</Th>
              <Th>Aktywny</Th>
              <Th />
            </Tr>
          </Thead>
          <Tbody>
            {overrides.length === 0 && (
              <Tr>
                <Td colSpan={7}>
                  <Text color="gray.500" textAlign="center" py={4}>
                    Brak override'ów
                  </Text>
                </Td>
              </Tr>
            )}
            {overrides.map((o) => (
              <Tr key={o.id}>
                <Td>
                  <Text fontWeight="medium" fontSize="sm">
                    {o.key}
                  </Text>
                </Td>
                <Td>
                  <Text fontSize="sm">{o.value}</Text>
                </Td>
                <Td maxW="200px">
                  <Text fontSize="sm" noOfLines={2} title={o.reason}>
                    {o.reason}
                  </Text>
                </Td>
                <Td>
                  <Text fontSize="sm">{formatDateTime(o.expiresAt)}</Text>
                </Td>
                <Td>
                  <Badge colorScheme={o.isValid ? "green" : "red"}>
                    {o.isValid ? "Tak" : "Nie"}
                  </Badge>
                </Td>
                <Td>
                  <Badge colorScheme={o.isActive ? "green" : "gray"}>
                    {o.isActive ? "Aktywny" : "Nieaktywny"}
                  </Badge>
                </Td>
                <Td>
                  {o.isActive && (
                    <Button
                      size="xs"
                      colorScheme="red"
                      variant="outline"
                      onClick={() => handleDeactivateClick(o.id)}
                      isLoading={isPending && pendingOverrideId === o.id}
                    >
                      Dezaktywuj
                    </Button>
                  )}
                </Td>
              </Tr>
            ))}
          </Tbody>
        </Table>
      </Box>

      <ConfirmAlertDialog
        isOpen={isOpen}
        onClose={handleDialogClose}
        onConfirm={handleConfirm}
        title="Dezaktywacja override"
        body="Czy na pewno chcesz dezaktywować ten override? Operacja nie usuwa rekordu."
        confirmLabel="Dezaktywuj"
        confirmColorScheme="red"
        isLoading={isPending}
      />
    </>
  );
}
