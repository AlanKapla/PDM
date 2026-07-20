import React, { useState } from "react";
import {
  Box,
  Button,
  Heading,
  HStack,
  Text,
  useColorModeValue,
  useDisclosure,
} from "@chakra-ui/react";
import { ArrowLeft, Mail, Users } from "lucide-react";
import { useNavigate } from "react-router-dom";
import MainLayout from "../layout/MainLayout";
import { EmptyState, ErrorAlert, LoadingSpinner } from "../components/common";
import { AdminUsersTable } from "../components/admin/AdminUsersTable";
import DeleteAlertDialog from "../components/ui/DeleteAlertDialog";
import { useAdminUsers } from "../hooks/useAdminUsers";
import { useSendWelcomeEmails } from "../hooks/useSendWelcomeEmails";
import { useSendWelcomeEmailToUser } from "../hooks/useSendWelcomeEmailToUser";
import { getApiErrorMessage } from "../utils/apiErrorUtils";

export default function AdminUsersPage(): React.ReactElement {
  const navigate = useNavigate();
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const mutedText = useColorModeValue("gray.600", "gray.400");

  const { data: users, isLoading, error } = useAdminUsers();
  const bulkSend = useSendWelcomeEmails();
  const singleSend = useSendWelcomeEmailToUser();
  const bulkDisclosure = useDisclosure();
  const [sendingUserId, setSendingUserId] = useState<string | null>(null);

  const handleSendToUser = async (userId: string): Promise<void> => {
    setSendingUserId(userId);
    try {
      await singleSend.mutateAsync(userId);
    } finally {
      setSendingUserId(null);
    }
  };

  const handleBulkConfirm = async (): Promise<void> => {
    await bulkSend.mutateAsync();
    bulkDisclosure.onClose();
  };

  let content: React.ReactElement;

  if (isLoading) {
    content = <LoadingSpinner message="Ładowanie użytkowników..." />;
  } else if (error) {
    content = (
      <ErrorAlert
        title="Nie udało się pobrać użytkowników"
        description={getApiErrorMessage(error)}
      />
    );
  } else if (!users || users.length === 0) {
    content = (
      <EmptyState
        icon={Users}
        title="Brak użytkowników"
        description="W systemie nie ma jeszcze żadnych kont użytkowników."
      />
    );
  } else {
    content = (
      <Box
        bg={cardBg}
        borderWidth="1px"
        borderColor={borderColor}
        rounded="xl"
        overflow="hidden"
      >
        <AdminUsersTable
          users={users}
          onSendWelcomeEmail={handleSendToUser}
          isSending={singleSend.isPending}
          sendingUserId={sendingUserId}
        />
      </Box>
    );
  }

  return (
    <MainLayout>
      <Box bg={pageBg} minH="calc(100vh - 60px)" p={{ base: 4, md: 8 }}>
        <Box maxW="1100px" mx="auto">
          <HStack mb={6} justify="space-between" flexWrap="wrap" spacing={3}>
            <Button
              leftIcon={<ArrowLeft size={16} aria-hidden />}
              variant="ghost"
              size="sm"
              onClick={() => navigate("/admin")}
            >
              Wróć do panelu
            </Button>

            <Button
              leftIcon={<Mail size={16} aria-hidden />}
              colorScheme="primary"
              size="sm"
              onClick={bulkDisclosure.onOpen}
              isLoading={bulkSend.isPending}
            >
              Wyślij maile powitalne
            </Button>
          </HStack>

          <HStack spacing={3} mb={2}>
            <Users size={24} aria-hidden />
            <Heading size="lg">Użytkownicy</Heading>
          </HStack>

          <Text color={mutedText} mb={8}>
            Lista wszystkich użytkowników systemu. Kliknij wiersz, aby zobaczyć szczegóły.
          </Text>

          {content}
        </Box>
      </Box>

      <DeleteAlertDialog
        isOpen={bulkDisclosure.isOpen}
        onClose={bulkDisclosure.onClose}
        onConfirm={() => {
          void handleBulkConfirm();
        }}
        title="Wysłać maile powitalne?"
        description="Wyśle maile powitalne do wszystkich aktywnych użytkowników, którzy jeszcze ich nie otrzymali. Tej operacji nie można cofnąć."
        confirmLabel="Wyślij"
        isLoading={bulkSend.isPending}
      />
    </MainLayout>
  );
}
