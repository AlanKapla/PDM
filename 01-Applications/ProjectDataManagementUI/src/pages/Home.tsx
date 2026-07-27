import { Button, Flex, Spinner, Text, VStack } from "@chakra-ui/react";
import { LogIn } from "lucide-react";
import { useEffect } from "react";
import { Link as RouterLink } from "react-router-dom";
import { useMsal, useIsAuthenticated, useAccount } from "@azure/msal-react";
import { DemoModeHomeToggle } from "../components/DemoModeHomeToggle";
import {
  AuthPageHeading,
  AuthPageShell,
} from "../features/auth/components/AuthPageShell";

export default function Home() {
  const { accounts, inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const account = useAccount(accounts[0] || null);

  const isLoading = inProgress === "login" || inProgress === "acquireToken";
  const authLoading = isAuthenticated && !account;

  useEffect(() => {}, [isAuthenticated, isLoading, authLoading, account]);

  if (isLoading) {
    return (
      <Flex minH="100vh" align="center" justify="center" bg="white">
        <VStack spacing={4}>
          <Spinner size="xl" color="primary.500" thickness="3px" />
          <Text color="neutral.600">Przetwarzanie logowania...</Text>
        </VStack>
      </Flex>
    );
  }

  if (isAuthenticated && authLoading) {
    return (
      <Flex minH="100vh" align="center" justify="center" bg="white">
        <VStack spacing={4}>
          <Spinner size="xl" color="primary.500" thickness="3px" />
          <Text color="neutral.600">Ładowanie profilu użytkownika...</Text>
        </VStack>
      </Flex>
    );
  }

  if (isAuthenticated && account) {
    return (
      <Flex minH="100vh" align="center" justify="center" bg="white">
        <VStack spacing={4}>
          <Spinner size="xl" color="primary.500" thickness="3px" />
          <Text color="neutral.600">Przekierowywanie do aplikacji...</Text>
        </VStack>
      </Flex>
    );
  }

  return (
    <AuthPageShell
      footer={
        <Text fontSize="sm" color="neutral.600">
          Kosztorysy · Harmonogramy · Pliki · Komunikacja
        </Text>
      }
    >
      <VStack spacing={5} align="stretch">
        <AuthPageHeading
          title="Witaj w Brickly"
          hint="Zaloguj się, żeby kontynuować pracę."
        />
        <Button
          as={RouterLink}
          to="/login"
          size="lg"
          w="full"
          colorScheme="primary"
          fontWeight={700}
          borderRadius="10px"
          leftIcon={<LogIn size={18} aria-hidden="true" />}
        >
          Zaloguj się
        </Button>
        <Button
          as={RouterLink}
          to="/register"
          size="md"
          w="full"
          variant="outline"
          colorScheme="primary"
        >
          Utwórz konto
        </Button>
        <DemoModeHomeToggle />
      </VStack>
    </AuthPageShell>
  );
}
