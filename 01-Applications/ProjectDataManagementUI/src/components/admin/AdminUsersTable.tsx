import React, { useState } from "react";
import {
  Badge,
  HStack,
  IconButton,
  Table,
  TableContainer,
  Tbody,
  Td,
  Text,
  Th,
  Thead,
  Tooltip,
  Tr,
  useDisclosure,
} from "@chakra-ui/react";
import { Mail } from "lucide-react";
import type { AdminUserWeb } from "../../types/admin.types";
import { formatDate } from "../../utils/formatters";
import { AdminUserDetailsModal } from "./AdminUserDetailsModal";
import DeleteAlertDialog from "../ui/DeleteAlertDialog";

export interface AdminUsersTableProps {
  users: AdminUserWeb[];
  onSendWelcomeEmail: (userId: string) => Promise<void>;
  isSending: boolean;
  sendingUserId: string | null;
}

export function AdminUsersTable({
  users,
  onSendWelcomeEmail,
  isSending,
  sendingUserId,
}: AdminUsersTableProps): React.ReactElement {
  const detailsDisclosure = useDisclosure();
  const confirmDisclosure = useDisclosure();
  const [selectedUser, setSelectedUser] = useState<AdminUserWeb | null>(null);
  const [userToSend, setUserToSend] = useState<AdminUserWeb | null>(null);

  const handleOpenDetails = (user: AdminUserWeb): void => {
    setSelectedUser(user);
    detailsDisclosure.onOpen();
  };

  const handleRequestSend = (user: AdminUserWeb): void => {
    setUserToSend(user);
    confirmDisclosure.onOpen();
  };

  const handleConfirmSend = async (): Promise<void> => {
    if (!userToSend) {
      return;
    }

    await onSendWelcomeEmail(userToSend.id);
    confirmDisclosure.onClose();
    setUserToSend(null);
  };

  return (
    <>
      <TableContainer>
        <Table variant="simple" size="sm">
          <Thead>
            <Tr>
              <Th>Użytkownik</Th>
              <Th>Email</Th>
              <Th>Status</Th>
              <Th>Mail powitalny</Th>
              <Th>Data wysyłki</Th>
              <Th width="60px">Akcje</Th>
            </Tr>
          </Thead>
          <Tbody>
            {users.map((user) => {
              const welcomeSent = user.welcomeEmailSentAt !== null;
              const isRowSending = isSending && sendingUserId === user.id;

              return (
                <Tr
                  key={user.id}
                  cursor="pointer"
                  _hover={{ bg: "neutral.50" }}
                  onClick={() => handleOpenDetails(user)}
                >
                  <Td>
                    <Text fontWeight="medium">
                      {user.firstName} {user.lastName}
                    </Text>
                    <Text fontSize="xs" color="neutral.600">
                      {user.systemRole}
                    </Text>
                  </Td>
                  <Td>{user.email}</Td>
                  <Td>
                    <Badge colorScheme={user.isActive ? "green" : "gray"}>
                      {user.isActive ? "Aktywny" : "Nieaktywny"}
                    </Badge>
                  </Td>
                  <Td>
                    <Badge colorScheme={welcomeSent ? "green" : "orange"}>
                      {welcomeSent ? "Wysłany" : "Nie wysłany"}
                    </Badge>
                  </Td>
                  <Td>
                    <Text fontSize="sm" color="neutral.700">
                      {welcomeSent ? formatDate(user.welcomeEmailSentAt) : "—"}
                    </Text>
                  </Td>
                  <Td>
                    <HStack spacing={1} onClick={(e) => e.stopPropagation()}>
                      <Tooltip label="Wyślij mail powitalny">
                        <IconButton
                          aria-label={`Wyślij mail powitalny do ${user.firstName} ${user.lastName}`}
                          icon={<Mail size={14} aria-hidden />}
                          size="xs"
                          variant="ghost"
                          colorScheme="primary"
                          isLoading={isRowSending}
                          onClick={(e) => {
                            e.stopPropagation();
                            handleRequestSend(user);
                          }}
                        />
                      </Tooltip>
                    </HStack>
                  </Td>
                </Tr>
              );
            })}
          </Tbody>
        </Table>
      </TableContainer>

      <AdminUserDetailsModal
        user={selectedUser}
        isOpen={detailsDisclosure.isOpen}
        onClose={detailsDisclosure.onClose}
        isSending={isSending && selectedUser !== null && sendingUserId === selectedUser.id}
        onSendWelcomeEmail={(userId) => {
          const user = users.find((u) => u.id === userId) ?? null;
          if (user) {
            handleRequestSend(user);
          }
        }}
      />

      <DeleteAlertDialog
        isOpen={confirmDisclosure.isOpen}
        onClose={confirmDisclosure.onClose}
        onConfirm={() => {
          void handleConfirmSend();
        }}
        title="Wysłać mail powitalny?"
        description={
          userToSend
            ? `Wyśle mail powitalny do ${userToSend.firstName} ${userToSend.lastName} (${userToSend.email}).`
            : "Wyśle mail powitalny do wybranego użytkownika."
        }
        confirmLabel="Wyślij"
        isLoading={isSending && userToSend !== null && sendingUserId === userToSend.id}
      />
    </>
  );
}
