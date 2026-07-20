import React from "react";
import {
  Box,
  Button,
  HStack,
  Text,
  useColorModeValue,
} from "@chakra-ui/react";
import { Mail } from "lucide-react";
import { useNavigate } from "react-router-dom";

export function ColdMailsAdminPanel(): React.ReactElement {
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
        <Mail size={20} aria-hidden="true" />
        <Text fontSize="lg" fontWeight="semibold">
          Cold mail
        </Text>
      </HStack>

      <Text color={mutedText} mb={6} fontSize="sm">
        Wysyłaj wiadomości do potencjalnych klientów i przeglądaj historię
        wysyłek z filtrem po adresie e-mail.
      </Text>

      <Button
        colorScheme="primary"
        onClick={() => navigate("/admin/cold-mails")}
      >
        Otwórz cold mail
      </Button>
    </Box>
  );
}
