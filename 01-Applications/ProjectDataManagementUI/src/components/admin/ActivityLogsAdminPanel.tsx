import React from "react";
import {
  Box,
  Button,
  HStack,
  Text,
  useColorModeValue,
} from "@chakra-ui/react";
import { Activity } from "lucide-react";
import { useNavigate } from "react-router-dom";

export function ActivityLogsAdminPanel(): React.ReactElement {
  const navigate = useNavigate();
  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const mutedText = useColorModeValue("gray.600", "gray.400");

  return (
    <Box
      bg={cardBg}
      borderWidth="1px"
      borderColor={borderColor}
      rounded="xl"
      p={6}
    >
      <HStack spacing={3} mb={4}>
        <Activity size={20} aria-hidden="true" />
        <Text fontSize="lg" fontWeight="semibold">
          Aktywność użytkowników
        </Text>
      </HStack>

      <Text color={mutedText} mb={6} fontSize="sm">
        Przeglądaj logi logowań i wejść w tryb demo — adres IP, czas i trasa.
      </Text>

      <Button
        colorScheme="primary"
        onClick={() => navigate("/admin/activity-logs")}
      >
        Otwórz logi aktywności
      </Button>
    </Box>
  );
}
