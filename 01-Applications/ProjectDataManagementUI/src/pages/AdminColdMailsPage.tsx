import React, { useCallback, useRef, useState } from "react";
import {
  Box,
  Button,
  Heading,
  HStack,
  Text,
  useColorModeValue,
  VStack,
} from "@chakra-ui/react";
import { ArrowLeft, Mail } from "lucide-react";
import { useNavigate } from "react-router-dom";
import MainLayout from "../layout/MainLayout";
import { EmptyState, ErrorAlert, LoadingSpinner } from "../components/common";
import { ColdMailSendForm } from "../components/admin/ColdMailSendForm";
import { ColdMailHistoryFilter } from "../components/admin/ColdMailHistoryFilter";
import { ColdMailHistoryTable } from "../components/admin/ColdMailHistoryTable";
import { useColdMailHistory } from "../hooks/useColdMailHistory";
import { useSendColdMails } from "../hooks/useSendColdMails";
import { getApiErrorMessage } from "../utils/apiErrorUtils";
import type { SendColdMailsRequest } from "../types/admin.types";

export default function AdminColdMailsPage(): React.ReactElement {
  const navigate = useNavigate();
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const mutedText = useColorModeValue("gray.600", "gray.400");

  const [emailFilter, setEmailFilter] = useState<string>("");
  const [debouncedFilter, setDebouncedFilter] = useState<string>("");
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const handleFilterChange = useCallback((value: string): void => {
    setEmailFilter(value);
    if (debounceRef.current) {
      clearTimeout(debounceRef.current);
    }
    debounceRef.current = setTimeout(() => {
      setDebouncedFilter(value);
    }, 300);
  }, []);

  const {
    data: history,
    isLoading,
    error,
  } = useColdMailHistory(debouncedFilter);
  const sendMutation = useSendColdMails();

  const handleSend = async (request: SendColdMailsRequest): Promise<void> => {
    await sendMutation.mutateAsync(request);
  };

  let historyContent: React.ReactElement;

  if (isLoading) {
    historyContent = <LoadingSpinner message="Ładowanie historii..." />;
  } else if (error) {
    historyContent = (
      <ErrorAlert
        title="Nie udało się pobrać historii"
        description={getApiErrorMessage(error)}
      />
    );
  } else if (!history || history.length === 0) {
    historyContent = (
      <EmptyState
        icon={Mail}
        title="Brak historii wysyłek"
        description={
          debouncedFilter.trim()
            ? "Brak wyników dla podanego filtra e-mail."
            : "Po pierwszej wysyłce historia pojawi się tutaj."
        }
      />
    );
  } else {
    historyContent = (
      <Box
        bg={cardBg}
        borderWidth="1px"
        borderColor={borderColor}
        rounded="xl"
        overflow="hidden"
      >
        <ColdMailHistoryTable items={history} />
      </Box>
    );
  }

  return (
    <MainLayout>
      <Box bg={pageBg} minH="calc(100vh - 60px)" p={{ base: 4, md: 8 }}>
        <Box maxW="1280px" mx="auto">
          <HStack mb={6}>
            <Button
              leftIcon={<ArrowLeft size={16} aria-hidden="true" />}
              variant="ghost"
              size="sm"
              onClick={() => navigate("/admin")}
            >
              Wróć do panelu
            </Button>
          </HStack>

          <HStack spacing={3} mb={2}>
            <Mail size={24} aria-hidden="true" />
            <Heading size="lg">Cold mail</Heading>
          </HStack>

          <Text color={mutedText} mb={8}>
            Wysyłka cold maili do potencjalnych klientów oraz historia wysyłek.
          </Text>

          <VStack align="stretch" spacing={8}>
            <ColdMailSendForm
              onSubmit={handleSend}
              isSubmitting={sendMutation.isPending}
            />

            <Box as="section" aria-labelledby="cold-mail-history-heading">
              <HStack
                justify="space-between"
                align={{ base: "stretch", md: "center" }}
                flexDir={{ base: "column", md: "row" }}
                spacing={3}
                mb={4}
              >
                <Heading id="cold-mail-history-heading" size="md">
                  Historia wysyłek
                </Heading>
                <ColdMailHistoryFilter
                  value={emailFilter}
                  onChange={handleFilterChange}
                />
              </HStack>

              {historyContent}
            </Box>
          </VStack>
        </Box>
      </Box>
    </MainLayout>
  );
}
