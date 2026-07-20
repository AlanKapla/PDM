import React from "react";
import {
  Alert,
  AlertIcon,
  Badge,
  Box,
  Divider,
  Text,
  VStack,
} from "@chakra-ui/react";
import AppModal from "../ui/AppModal";
import type { ColdMailHistoryWeb } from "../../types/admin.types";
import { formatDate } from "../../utils/formatters";
import { formatColdMailStatus, coldMailStatusColorScheme } from "./coldMailStatus";

export interface ColdMailHistoryDetailsModalProps {
  item: ColdMailHistoryWeb | null;
  isOpen: boolean;
  onClose: () => void;
}

export function ColdMailHistoryDetailsModal({
  item,
  isOpen,
  onClose,
}: ColdMailHistoryDetailsModalProps): React.ReactElement {
  if (!item) {
    return <></>;
  }

  const statusLabel: string = formatColdMailStatus(item.status);
  const isFailed: boolean = item.status === "Failed";
  const statusColor: string = coldMailStatusColorScheme(item.status);

  return (
    <AppModal
      isOpen={isOpen}
      onClose={onClose}
      title="Szczegóły cold maila"
      hideFooter
      desktopSize="xl"
    >
      <VStack align="stretch" spacing={4}>
        <Box>
          <Text fontSize="xs" color="neutral.600" mb={0.5}>
            Odbiorca
          </Text>
          <Text fontSize="md" fontWeight="semibold" color="neutral.800">
            {item.recipientEmail}
          </Text>
        </Box>

        <Box>
          <Text fontSize="xs" color="neutral.600" mb={0.5}>
            Temat
          </Text>
          <Text fontSize="sm" fontWeight="medium" color="neutral.800">
            {item.subject}
          </Text>
        </Box>

        <Box display="flex" gap={3} flexWrap="wrap" alignItems="center">
          <Badge colorScheme={statusColor}>{statusLabel}</Badge>
          <Text fontSize="sm" color="neutral.700">
            {formatDate(item.sentAt)}
          </Text>
        </Box>

        {isFailed && item.errorMessage && (
          <Alert status="error" role="alert" borderRadius="md">
            <AlertIcon aria-hidden="true" />
            {item.errorMessage}
          </Alert>
        )}

        <Divider />

        <Box>
          <Text fontSize="xs" color="neutral.600" mb={2}>
            Wyrenderowany mail
          </Text>
          <Box
            borderWidth="1px"
            borderColor="neutral.200"
            borderRadius="md"
            overflow="hidden"
            bg="neutral.100"
          >
            <Box
              as="iframe"
              title={`Podgląd maila do ${item.recipientEmail}`}
              srcDoc={item.htmlBody}
              sandbox=""
              w="100%"
              h="480px"
              border="0"
              bg="white"
            />
          </Box>
        </Box>
      </VStack>
    </AppModal>
  );
}
