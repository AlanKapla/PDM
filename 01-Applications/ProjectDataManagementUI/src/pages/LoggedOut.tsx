import { useEffect, useContext } from "react";
import { Link as RouterLink } from "react-router-dom";
import { Button, Flex, Spinner, Text, VStack } from "@chakra-ui/react";
import { LogIn } from "lucide-react";
import { useMsal } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import { AuthContext } from "../context/AuthContext";
import { DemoModeHomeToggle } from "../components/DemoModeHomeToggle";
import {
  AuthPageHeading,
  AuthPageShell,
} from "../features/auth/components/AuthPageShell";

export default function LoggedOut() {
  const { inProgress } = useMsal();
  const { isAuthenticated, user, loading: authLoading } = useContext(AuthContext);

  useEffect(() => {
    Object.keys(localStorage).forEach((key) => {
      if (!key.startsWith("msal.")) {
        localStorage.removeItem(key);
      }
    });
    sessionStorage.clear();
  }, []);

  const isLoading = inProgress !== InteractionStatus.None;

  useEffect(() => {
  }, [isAuthenticated, isLoading, authLoading, user]);

  if (isLoading) {
    return (
      <Flex minH="100vh" align="center" justify="center" bg="white">
        <VStack spacing={4}>
          <Spinner size="xl" color="primary.500" thickness="3px" />
          <Text color="neutral.600">Przetwarzanie wylogowania...</Text>
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
          title="Wylogowano"
          hint="Zostałeś wylogowany z systemu. Możesz zalogować się ponownie."
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
          Zaloguj się ponownie
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
