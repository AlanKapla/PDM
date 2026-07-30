import type { ReactElement, ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useContext, useEffect, useRef, useState } from "react";
import { AuthContext } from "../context/AuthContext";
import { isDemoModeActive } from "../api/mock";
import { Button, Flex, Spinner, Text, VStack } from "@chakra-ui/react";
import { useMsal } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import { clearStaleMsalInteraction } from "../auth/clearStaleMsalInteraction";
import TenantAccessGuard from "../components/TenantAccessGuard";

// Po tylu ms utknięcia w stanie innym niż None uznajemy sesję MSAL za
// zawieszoną (np. porzucona flaga interaction_in_progress w localStorage) i
// pokazujemy użytkownikowi możliwość awaryjnego resetu.
const STUCK_TIMEOUT_MS = 8000;
/** Escape hatch gdy /user/me lub token refresh wisi po powrocie z background. */
const PROFILE_STUCK_TIMEOUT_MS = 12_000;

// Usuwa cache MSAL (w tym zawieszoną flagę interaction) i przeładowuje stronę.
function resetMsalSessionAndReload(): void {
  clearStaleMsalInteraction();
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

function SessionStuckEscape(): ReactElement {
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

export default function ProtectedRoute({ children }: { children: ReactNode }) {
  const { loading, user, isAuthenticated } = useContext(AuthContext);
  const { inProgress } = useMsal();
  const location = useLocation();
  const [sessionStuck, setSessionStuck] = useState(false);
  const [profileStuck, setProfileStuck] = useState(false);
  // Wall-clock start — nie resetuj przy każdej zmianie inProgress między stanami ≠ None
  // (np. startup → handleRedirect), bo wtedy spinner nigdy nie pokaże resetu.
  const busySinceRef = useRef<number | null>(null);
  const loadingSinceRef = useRef<number | null>(null);

  useEffect(() => {
    if (inProgress === InteractionStatus.None) {
      busySinceRef.current = null;
      setSessionStuck(false);
      return;
    }

    if (busySinceRef.current === null) {
      busySinceRef.current = Date.now();
    }

    const elapsed: number = Date.now() - busySinceRef.current;
    const remaining: number = Math.max(0, STUCK_TIMEOUT_MS - elapsed);

    const timer = setTimeout(() => setSessionStuck(true), remaining);
    return () => clearTimeout(timer);
  }, [inProgress]);

  useEffect(() => {
    // Tylko gdy MSAL idle, a profil nadal się ładuje.
    if (!loading || inProgress !== InteractionStatus.None) {
      loadingSinceRef.current = null;
      setProfileStuck(false);
      return;
    }

    if (loadingSinceRef.current === null) {
      loadingSinceRef.current = Date.now();
    }

    const elapsed: number = Date.now() - loadingSinceRef.current;
    const remaining: number = Math.max(0, PROFILE_STUCK_TIMEOUT_MS - elapsed);

    const timer = setTimeout(() => setProfileStuck(true), remaining);
    return () => clearTimeout(timer);
  }, [loading, inProgress]);

  // Czekaj na zakończenie MSAL initialization (handleRedirectPromise, login, logout, itp.)
  if (inProgress !== InteractionStatus.None) {
    if (sessionStuck) {
      return <SessionStuckEscape />;
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
    if (profileStuck) {
      return <SessionStuckEscape />;
    }

    return (
      <Flex justify="center" align="center" minH="100vh">
        <VStack spacing={4}>
          <Spinner size="xl" color="primary.500" thickness="4px" />
          <Text color="neutral.600">Ładowanie profilu użytkownika...</Text>
        </VStack>
      </Flex>
    );
  }

  const demoAuthenticated = isDemoModeActive() && user !== null;
  const msalAuthenticated = isAuthenticated && user !== null;

  if (!msalAuthenticated && !demoAuthenticated) {
    return <Navigate to="/" state={{ from: location }} replace />;
  }

  return <TenantAccessGuard>{children}</TenantAccessGuard>;
}
