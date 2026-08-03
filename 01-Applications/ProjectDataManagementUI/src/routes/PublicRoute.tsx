import type { ReactNode } from "react";
import { useContext } from "react";
import { Navigate } from "react-router-dom";
import { AuthContext } from "../context/AuthContext";
import { isDemoModeActive } from "../api/mock";
import { Spinner, Flex } from "@chakra-ui/react";

export default function PublicRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated, user, loading } = useContext(AuthContext);

  // Wait for auth check to complete
  if (loading) {
    return (
      <Flex justify="center" align="center" minH="100vh">
        <Spinner size="xl" color="primary.500" thickness="4px" />
      </Flex>
    );
  }

  const demoAuthenticated = isDemoModeActive() && user !== null;
  const msalAuthenticated = isAuthenticated && user !== null;

  if (msalAuthenticated || demoAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  // Show public page (login, register, etc.)
  return <>{children}</>;
}