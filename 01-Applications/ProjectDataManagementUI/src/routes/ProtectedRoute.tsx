import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useContext } from "react";
import { AuthContext } from "../context/AuthContext";
import { Flex, Spinner, Text, VStack } from "@chakra-ui/react";

export default function ProtectedRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated, loading, user } = useContext(AuthContext);
  const location = useLocation();

  // Czekaj na zakończenie sprawdzania sesji I pobranie danych użytkownika
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

  if (!isAuthenticated || !user) {
    // Zapisz aktualny URL w state, aby móc wrócić po zalogowaniu
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return <>{children}</>;
}
