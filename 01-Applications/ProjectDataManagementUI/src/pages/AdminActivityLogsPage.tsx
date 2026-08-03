import React from "react";
import {
  Box,
  Button,
  Heading,
  HStack,
  Text,
  useColorModeValue,
} from "@chakra-ui/react";
import { Activity, ArrowLeft } from "lucide-react";
import { useNavigate } from "react-router-dom";
import MainLayout from "../layout/MainLayout";
import { EmptyState, ErrorAlert, LoadingSpinner } from "../components/common";
import { ActivityLogsTable } from "../components/admin/ActivityLogsTable";
import { useActivityLogs } from "../hooks/useActivityLogs";
import { getApiErrorMessage } from "../utils/apiErrorUtils";

export default function AdminActivityLogsPage(): React.ReactElement {
  const navigate = useNavigate();
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const mutedText = useColorModeValue("gray.600", "gray.400");

  const { data: logs, isLoading, error } = useActivityLogs();

  let content: React.ReactElement;

  if (isLoading) {
    content = <LoadingSpinner message="Ładowanie logów aktywności..." />;
  } else if (error) {
    content = (
      <ErrorAlert
        title="Nie udało się pobrać logów aktywności"
        description={getApiErrorMessage(error)}
      />
    );
  } else if (!logs || logs.length === 0) {
    content = (
      <EmptyState
        icon={Activity}
        title="Brak logów aktywności"
        description="Po pierwszym logowaniu lub wejściu w tryb demo wpisy pojawią się tutaj."
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
        <ActivityLogsTable items={logs} />
      </Box>
    );
  }

  return (
    <MainLayout>
      <Box bg={pageBg} minH="calc(100vh - 60px)" p={{ base: 4, md: 8 }}>
        <Box maxW="1100px" mx="auto">
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
            <Activity size={24} aria-hidden="true" />
            <Heading size="lg">Logi aktywności</Heading>
          </HStack>

          <Text color={mutedText} mb={8}>
            Logowania i wejścia w tryb demo — IP, czas, trasa i powiązany
            użytkownik.
          </Text>

          {content}
        </Box>
      </Box>
    </MainLayout>
  );
}
