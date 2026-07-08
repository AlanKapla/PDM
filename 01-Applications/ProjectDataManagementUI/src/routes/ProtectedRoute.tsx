import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useContext, useEffect, useState } from "react";
import { AuthContext } from "../context/AuthContext";
import { Button, Flex, Spinner, Text, VStack } from "@chakra-ui/react";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import TenantAccessGuard from "../components/TenantAccessGuard";

// Po tylu ms utknięcia w stanie innym niż None uznajemy sesję MSAL za
// zawieszoną (np. porzucona flaga interaction_in_progress w localStorage) i
// pokazujemy użytkownikowi możliwość awaryjnego resetu.
const STUCK_TIMEOUT_MS = 8000;

// Usuwa cache MSAL (w tym zawieszoną flagę interaction) i przeładowuje stronę.
function resetMsalSessionAndReload(): void {
  try {
    Object.keys(localStorage)
      .filter((key) => key.startsWith("msal."))
      .forEach((key) => localStorage.removeItem(key));
    sessionStorage.clear();
  } catch {
    // Storage może być niedostępny (tryb prywatny) — mimo to spróbuj przeładować.
  }
  window.location.reload();
}

export default function ProtectedRoute({ children }: { children: ReactNode }) {
  const { loading, user } = useContext(AuthContext);
  const isAuthenticated = useIsAuthenticated();
  const { inProgress } = useMsal();
  const location = useLocation();
  const [sessionStuck, setSessionStuck] = useState(false);

  // Wykryj zawieszoną sesję MSAL, aby użytkownik nie utknął na zawsze na loaderze.
  useEffect(() => {
    if (inProgress === InteractionStatus.None) {
      setSessionStuck(false);
      return;
    }

    const timer = setTimeout(() => setSessionStuck(true), STUCK_TIMEOUT_MS);
    return () => clearTimeout(timer);
  }, [inProgress]);

  // Czekaj na zakończenie MSAL initialization (handleRedirectPromise, login, logout, itp.)
  if (inProgress !== InteractionStatus.None) {
    if (sessionStuck) {
      return (
        <Flex justify="center" align="center" minH="100vh" p={6}>
          <VStack spacing={4} maxW="md" textAlign="center">
            <Text fontWeight="semibold">Nie można potwierdzić sesji</Text>
            <Text color="neutral.600" fontSize="sm">
              Sprawdzanie sesji trwa zbyt długo. Zresetuj sesję, aby kontynuować.
            </Text>
            <Button colorScheme="primary" onClick={resetMsalSessionAndReload}>
              Zresetuj sesję i odśwież
            </Button>
          </VStack>
        </Flex>
      );
    }

    return (
      <Flex justify="center" align="center" minH="100vh">
        <VStack spacing={4}>
          <Spinner size="xl" color="primary.500" thickness="4px" />
          <Text color="neutral.600">Sprawdzanie sesji...</Text>
        </VStack>
      </Flex>
    );
  }

  // Czekaj na zakończenie pobrania profilu użytkownika
  if (loading) {
    return (
      <Flex justify="center" align="center" minH="100vh">
        <VStack spacing={4}>
          <Spinner size="xl" color="primary.500" thickness="4px" />
          <Text color="neutral.600">Ładowanie profilu użytkownika...</Text>
        </VStack>
      </Flex>
    );
  }

  // Tylko jeśli MSAL i AuthContext są gotowe i user nie istnieje, przekieruj na home
  if (!isAuthenticated || !user) {
    // Zapisz aktualny URL w state, aby móc wrócić po zalogowaniu
    return <Navigate to="/" state={{ from: location }} replace />;
  }

  return <TenantAccessGuard>{children}</TenantAccessGuard>;
}
