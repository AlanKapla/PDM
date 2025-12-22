import { useEffect, useRef } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { InteractionStatus, EventType } from "@azure/msal-browser";
import { Flex, Spinner, VStack, Text } from "@chakra-ui/react";

/**
 * Dedicated OAuth callback page - handles redirect from Azure AD B2C
 * This is where the authorization code is exchanged for tokens
 */
export default function AuthCallback() {
  const navigate = useNavigate();
  const location = useLocation();
  const { instance, inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const hasHandledCallback = useRef(false);

  useEffect(() => {
    // Only run once, and only if we haven't handled callback yet
    if (hasHandledCallback.current) {
      console.log("⏭️ AuthCallback: Already handled, skipping");
      return;
    }

    const handleCallback = async () => {
      console.log("🔄 AuthCallback: inProgress=", inProgress, "isAuthenticated=", isAuthenticated);

      // CRITICAL: Wait for MSAL to finish processing the redirect
      // Don't do ANYTHING until inProgress === None
      if (inProgress !== InteractionStatus.None) {
        console.log("⏳ AuthCallback: Waiting for MSAL to finish (inProgress=" + inProgress + ")");
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
                  console.log("📍 AuthCallback: Found returnUrl in state:", returnUrl);
                }
              }
            } catch (e) {
              console.warn("⚠️ AuthCallback: Could not parse state, using default:", e);
            }
          }
        } catch (error) {
          console.warn("⚠️ AuthCallback: Error reading state:", error);
        }
        
        console.log("✅ AuthCallback: Authentication successful, redirecting to", returnUrl);
        navigate(returnUrl, { replace: true });
      } else {
        console.log("❌ AuthCallback: Authentication failed, redirecting to login");
        navigate("/login", { replace: true });
      }
    };

    handleCallback();
  }, [inProgress, isAuthenticated, navigate, location]);

  return (
    <Flex minH="100vh" align="center" justify="center">
      <VStack spacing={4}>
        <Spinner size="xl" color="blue.500" thickness="4px" />
        <Text fontSize="lg" fontWeight="medium">
          Finalizowanie logowania...
        </Text>
        <Text fontSize="sm" color="gray.600">
          Wymieniamy kod autoryzacyjny na tokeny dostępu
        </Text>
      </VStack>
    </Flex>
  );
}
