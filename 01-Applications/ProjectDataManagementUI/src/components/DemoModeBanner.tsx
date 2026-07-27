import { Alert, AlertDescription, AlertIcon, Button, HStack } from "@chakra-ui/react";
import { LogIn } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { useState, type ReactElement } from "react";
import { useAppSession } from "../hooks/useAppSession";
import { useDemoMode } from "../context/DemoContext";

export function DemoModeBanner(): ReactElement | null {
  const { isDemoOnlySession } = useAppSession();
  const { exitDemoMode } = useDemoMode();
  const navigate = useNavigate();
  const [isLeaving, setIsLeaving] = useState(false);

  if (!isDemoOnlySession) {
    return null;
  }

  const handleLogin = async (): Promise<void> => {
    setIsLeaving(true);
    try {
      await exitDemoMode();
      navigate("/login");
    } finally {
      setIsLeaving(false);
    }
  };

  return (
    <Alert
      status="info"
      variant="subtle"
      borderRadius="none"
      py={2}
      px={{ base: 2, md: 4 }}
      bg="orange.50"
      borderTop="1px solid"
      borderColor="orange.200"
    >
      <AlertIcon color="orange.500" />
      <HStack flex={1} justify="space-between" spacing={3} flexWrap="wrap">
        <AlertDescription fontSize="sm" color="orange.900">
          Przeglądasz wersję demo z przykładowymi danymi.
        </AlertDescription>
        <Button
          size="sm"
          colorScheme="orange"
          variant="solid"
          leftIcon={<LogIn size={16} aria-hidden />}
          onClick={() => {
            void handleLogin();
          }}
          isLoading={isLeaving}
          loadingText="Przekierowywanie..."
          aria-label="Zaloguj się — wyjdź z trybu demo"
          flexShrink={0}
        >
          Zaloguj się
        </Button>
      </HStack>
    </Alert>
  );
}
