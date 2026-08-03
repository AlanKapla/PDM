import React from "react";
import {
  Box,
  Button,
  HStack,
  Text,
  useColorModeValue,
} from "@chakra-ui/react";
import { Users } from "lucide-react";
import { useNavigate } from "react-router-dom";

export function UsersAdminPanel(): React.ReactElement {
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
        <Users size={20} aria-hidden />
        <Text fontSize="lg" fontWeight="semibold">
          Użytkownicy
        </Text>
      </HStack>

      <Text color={mutedText} mb={6} fontSize="sm">
        Przeglądaj wszystkich użytkowników, status maili powitalnych i wysyłaj
        wiadomości powitalne.
      </Text>

      <Button colorScheme="primary" onClick={() => navigate("/admin/users")}>
        Otwórz listę użytkowników
      </Button>
    </Box>
  );
}
