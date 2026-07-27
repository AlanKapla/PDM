import { useState, type ReactElement } from "react";
import { Box, Button, Text, VStack } from "@chakra-ui/react";
import { Monitor } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { useDemoMode } from "../context/DemoContext";

export function DemoModeHomeToggle(): ReactElement {
  const { enterDemoMode } = useDemoMode();
  const navigate = useNavigate();
  const [isEntering, setIsEntering] = useState(false);

  const handleEnterDemo = async () => {
    setIsEntering(true);
    try {
      await enterDemoMode();
      navigate("/dashboard");
    } finally {
      setIsEntering(false);
    }
  };

  return (
    <Box
      w="full"
      pt={4}
      mt={2}
      borderTop="1px solid"
      borderColor="neutral.100"
    >
      <VStack spacing={3}>
        <Text fontSize="sm" color="neutral.600">
          Przeglądaj aplikację z przykładowymi danymi bez rejestracji
        </Text>
        <Button
          size="md"
          w="full"
          variant="outline"
          borderColor="neutral.200"
          color="neutral.700"
          fontWeight={600}
          borderRadius="10px"
          leftIcon={<Monitor size={18} aria-hidden />}
          onClick={() => {
            void handleEnterDemo();
          }}
          isLoading={isEntering}
          loadingText="Uruchamianie demo..."
          aria-label="Wypróbuj demo bez logowania"
          _hover={{ bg: "neutral.50", borderColor: "neutral.300" }}
        >
          Wypróbuj demo
        </Button>
      </VStack>
    </Box>
  );
}
