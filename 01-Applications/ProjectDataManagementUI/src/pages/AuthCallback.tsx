import { useEffect, useRef, useState } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import { Button, Flex, Spinner, VStack, Text } from "@chakra-ui/react";
import { clearStaleMsalInteraction } from "../auth/clearStaleMsalInteraction";

const CALLBACK_STUCK_MS = 12_000;

/**
 * Dedicated OAuth callback page - handles redirect from Azure AD B2C
 * This is where the authorization code is exchanged for tokens
 */
export default function AuthCallback() {
  const navigate = useNavigate();
  const location = useLocation();
  const { inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const hasHandledCallback = useRef(false);
  const [stuck, setStuck] = useState(false);

  useEffect(() => {
    // Only run once, and only if we haven't handled callback yet
    if (hasHandledCallback.current) {
      return;
    }

    const handleCallback = async () => {
      // CRITICAL: Wait for MSAL to finish processing the redirect
      // Don't do ANYTHING until inProgress === None
      if (inProgress !== InteractionStatus.None) {
        return; // Don't mark as handled yet - wait for next render
      }

      // Mark as handled BEFORE navigating to prevent re-execution
      hasHandledCallback.current = true;

      // Now MSAL has finished processing
      if (isAuthenticated) {
        // Try to get returnUrl from MSAL state first (passed through OAuth flow)
        let returnUrl = "/dashboard";

        try {
          // MSAL stores state in the URL hash (#state=...)
          const hash = window.location.hash;
          const hashParams = new URLSearchParams(hash.substring(1));
          const state = hashParams.get("state");

          if (state) {
            try {
              // State format: "msalStateId|{returnUrl:'/dashboard'}"
              const parts = state.split("|");
              if (parts.length > 1) {
                const stateObj = JSON.parse(decodeURIComponent(parts[1]));
                if (stateObj.returnUrl) {
                  returnUrl = stateObj.returnUrl;
                }
              }
            } catch {
              // ignore malformed state
            }
          }
        } catch {
          // ignore
        }
        navigate(returnUrl, { replace: true });
      } else {
        navigate("/login", { replace: true });
      }
    };

    void handleCallback();
  }, [inProgress, isAuthenticated, navigate, location]);

  // iOS PWA: hard kill w trakcie redirect zostawia inProgress ≠ None na zawsze.
  useEffect(() => {
    const timer: ReturnType<typeof setTimeout> = setTimeout(() => {
      setStuck(true);
    }, CALLBACK_STUCK_MS);
    return () => {
      clearTimeout(timer);
    };
  }, []);

  if (stuck) {
    return (
      <Flex minH="100vh" align="center" justify="center" p={6}>
        <VStack spacing={4} maxW="md" textAlign="center">
          <Text fontWeight="semibold">Logowanie nie dokończyło się</Text>
          <Text fontSize="sm" color="neutral.600">
            Sesja została przerwana. Spróbuj zalogować się ponownie.
          </Text>
          <Button
            colorScheme="primary"
            onClick={() => {
              clearStaleMsalInteraction();
              window.location.assign("/login");
            }}
          >
            Przejdź do logowania
          </Button>
        </VStack>
      </Flex>
    );
  }

  return (
    <Flex minH="100vh" align="center" justify="center">
      <VStack spacing={4}>
        <Spinner size="xl" color="primary.500" thickness="4px" />
        <Text fontSize="lg" fontWeight="medium">
          Finalizowanie logowania...
        </Text>
        <Text fontSize="sm" color="neutral.600">
          Wymieniamy kod autoryzacyjny na tokeny dostępu
        </Text>
      </VStack>
    </Flex>
  );
}
