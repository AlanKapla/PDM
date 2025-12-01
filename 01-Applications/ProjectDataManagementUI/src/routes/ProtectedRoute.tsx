import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import { Flex, Spinner } from "@chakra-ui/react";

export default function ProtectedRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated, loading } = useAuth();
  const location = useLocation();

  // Czekaj na zakończenie sprawdzania sesji
  if (loading) {
    return (
      <Flex justify="center" align="center" minH="100vh">
        <Spinner size="xl" color="blue.500" thickness="4px" />
      </Flex>
    );
  }

  if (!isAuthenticated) {
    // Zapisz aktualny URL w state, aby móc wrócić po zalogowaniu
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return <>{children}</>;
}
