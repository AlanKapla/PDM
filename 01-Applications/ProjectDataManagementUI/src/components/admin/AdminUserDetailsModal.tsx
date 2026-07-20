import React from "react";
import {
  Badge,
  Box,
  Button,
  Divider,
  HStack,
  SimpleGrid,
  Text,
  VStack,
} from "@chakra-ui/react";
import { Mail } from "lucide-react";
import AppModal from "../ui/AppModal";
import type { AdminUserWeb } from "../../types/admin.types";
import { formatDate } from "../../utils/formatters";

export interface AdminUserDetailsModalProps {
  user: AdminUserWeb | null;
  isOpen: boolean;
  onClose: () => void;
  onSendWelcomeEmail: (userId: string) => void;
  isSending: boolean;
}

export function AdminUserDetailsModal({
  user,
  isOpen,
  onClose,
  onSendWelcomeEmail,
  isSending,
}: AdminUserDetailsModalProps): React.ReactElement {
  if (!user) {
    return <></>;
  }

  const welcomeSent = user.welcomeEmailSentAt !== null;

  return (
    <AppModal
      isOpen={isOpen}
      onClose={onClose}
      title={`${user.firstName} ${user.lastName}`}
      hideFooter
      desktopSize="md"
    >
      <VStack align="stretch" spacing={4}>
        <SimpleGrid columns={2} spacing={3}>
          <DetailField label="Email" value={user.email} />
          <DetailField label="Rola systemowa" value={user.systemRole} />
          <DetailField
            label="Status"
            value={user.isActive ? "Aktywny" : "Nieaktywny"}
          />
          <DetailField label="Utworzono" value={formatDate(user.createdAt)} />
          <DetailField label="Telefon" value={user.phoneNumber ?? "—"} />
          <DetailField label="Firma" value={user.companyName ?? "—"} />
          <DetailField label="NIP" value={user.taxId ?? "—"} />
          <DetailField label="Miasto" value={user.city ?? "—"} />
          <DetailField label="Ulica" value={user.street ?? "—"} />
          <DetailField label="Kod pocztowy" value={user.postalCode ?? "—"} />
          <DetailField label="Kraj" value={user.country ?? "—"} />
        </SimpleGrid>

        <Divider />

        <Box>
          <Text fontSize="sm" color="neutral.600" mb={2}>
            Mail powitalny
          </Text>
          <HStack spacing={3} mb={4}>
            <Badge colorScheme={welcomeSent ? "green" : "orange"}>
              {welcomeSent ? "Wysłany" : "Nie wysłany"}
            </Badge>
            {welcomeSent && (
              <Text fontSize="sm" color="neutral.700">
                {formatDate(user.welcomeEmailSentAt)}
              </Text>
            )}
          </HStack>

          <Button
            leftIcon={<Mail size={16} aria-hidden />}
            colorScheme="primary"
            size="sm"
            isLoading={isSending}
            onClick={() => onSendWelcomeEmail(user.id)}
          >
            Wyślij mail powitalny
          </Button>
        </Box>
      </VStack>
    </AppModal>
  );
}

interface DetailFieldProps {
  label: string;
  value: string;
}

function DetailField({ label, value }: DetailFieldProps): React.ReactElement {
  return (
    <Box>
      <Text fontSize="xs" color="neutral.600" mb={0.5}>
        {label}
      </Text>
      <Text fontSize="sm" fontWeight="medium" color="neutral.800">
        {value}
      </Text>
    </Box>
  );
}
