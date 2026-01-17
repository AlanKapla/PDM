import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useContext } from "react";
import { AuthContext } from "../context/AuthContext";
import { Flex, Spinner, Text, VStack } from "@chakra-ui/react";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";

export default function ProtectedRoute({ children }: { children: ReactNode }) {
  const { loading, user } = useContext(AuthContext);
  const isAuthenticated = useIsAuthenticated();
  const { inProgress } = useMsal();
  const location = useLocation();

  // Czekaj na zakończenie MSAL initialization (handleRedirectPromise, login, logout, itp.)
  if (inProgress !== InteractionStatus.None) {
    return (
      <Flex justify="center" align="center" minH="100vh">
        <VStack spacing={4}>
          <Spinner size="xl" color="blue.500" thickness="4px" />
          <Text color="gray.600">Sprawdzanie sesji...</Text>
        </VStack>
      </Flex>
    );
  }

  // Czekaj na zakończenie pobrania profilu użytkownika
  if (loading) {
    return (
      <Flex justify="center" align="center" minH="100vh">
        <VStack spacing={4}>
          <Spinner size="xl" color="blue.500" thickness="4px" />
          <Text color="gray.600">Ładowanie profilu użytkownika...</Text>
        </VStack>
      </Flex>
    );
  }

  // Tylko jeśli MSAL i AuthContext są gotowe i user nie istnieje, przekieruj na home
  if (!isAuthenticated || !user) {
    // Zapisz aktualny URL w state, aby móc wrócić po zalogowaniu
    return <Navigate to="/" state={{ from: location }} replace />;
  }

  return <>{children}</>;
}
