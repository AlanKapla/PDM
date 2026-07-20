import React from "react";
import {
  Box,
  FormControl,
  FormHelperText,
  FormLabel,
  HStack,
  Switch,
  Text,
  useColorModeValue,
} from "@chakra-ui/react";
import { Monitor } from "lucide-react";
import { useDemoMode } from "../../context/DemoContext";

export function DemoModePanel(): React.ReactElement {
  const { isDemoMode, toggleDemoMode } = useDemoMode();
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
        <Monitor size={20} aria-hidden />
        <Text fontSize="lg" fontWeight="semibold">
          Tryb demo
        </Text>
      </HStack>

      <Text color={mutedText} mb={4} fontSize="sm">
        Włącza mockowane dane we wszystkich modułach aplikacji. Przydatne do prezentacji bez
        połączenia z backendem.
      </Text>

      <FormControl>
        <HStack align="center">
          <FormLabel htmlFor="demo-mode-toggle" mb={0} flex={1}>
            Tryb demo {isDemoMode ? "włączony" : "wyłączony"}
          </FormLabel>
          <Switch
            id="demo-mode-toggle"
            colorScheme="orange"
            isChecked={isDemoMode}
            onChange={() => {
              toggleDemoMode();
            }}
          />
        </HStack>

        <FormHelperText mt={2}>
          Po włączeniu sesja działa w trybie demo-only bez pełnej autoryzacji MSAL.
        </FormHelperText>
      </FormControl>
    </Box>
  );
}
