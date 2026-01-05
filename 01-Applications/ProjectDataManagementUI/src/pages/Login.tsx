import { useEffect, useContext } from "react";
import {
  Box,
  Button,
  Flex,
  Heading,
  VStack,
  Text,
  useColorModeValue,
  Spinner,
  Container,
} from "@chakra-ui/react";
import { useNavigate, useLocation } from "react-router-dom";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import { loginRequest } from "../config/authConfig";
import { AuthContext } from "../context/AuthContext";
import { LogIn } from "lucide-react";

export default function Login() {
  const navigate = useNavigate();
  const location = useLocation();
  const { instance, inProgress } = useMsal();
  const msalAuthenticated = useIsAuthenticated();
  const { isAuthenticated, user, loading: authLoading } = useContext(AuthContext);

  // All hooks must be at the top - before any conditional returns
  const bgColor = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const pageBackground = useColorModeValue("gray.50", "gray.900");

  const isLoading = inProgress !== InteractionStatus.None;

  // Logging for debugging only - PublicRoute handles redirect to dashboard
  useEffect(() => {
    console.log("🔍 Login.tsx state:", {
      msalAuthenticated,
      isLoading,
      authLoading,
      hasUser: !!user,
    });
  }, [msalAuthenticated, isLoading, authLoading, user]);

  // Simple login handler - follows MSAL redirect pattern
  // See: https://learn.microsoft.com/en-us/entra/identity-platform/scenario-spa-sign-in
  const handleLogin = async () => {
    try {
      console.log("🚀 Starting login redirect to External ID...");
      console.log("🔧 Authority:", instance.getConfiguration().auth.authority);
      console.log("🔧 Scopes:", loginRequest.scopes);
      
      // Preserve return URL through OAuth state
      const returnUrl = (location.state as any)?.from?.pathname || "/dashboard";
      
      // Redirect to External ID login/signup page
      await instance.loginRedirect({
        ...loginRequest,
        state: JSON.stringify({ returnUrl }),
      });
      
      // User will be redirected to External ID, then back to /auth/callback
    } catch (error) {
      console.error("❌ Login redirect failed:", error);
    }
  };

  if (isLoading) {
    return (
      <Flex minH="100vh" align="center" justify="center">
        <VStack spacing={4}>
          <Spinner size="xl" color="blue.500" thickness="4px" />
          <Text>Przetwarzanie logowania...</Text>
        </VStack>
      </Flex>
    );
  }
  // If MSAL authenticated, wait for user profile
  if (msalAuthenticated && authLoading) {
    return (
      <Flex minH="100vh" align="center" justify="center">
        <VStack spacing={4}>
          <Spinner size="xl" color="green.500" thickness="4px" />
          <Text>Ładowanie profilu użytkownika...</Text>
        </VStack>
      </Flex>
    );
  }

  // If fully authenticated with profile, show redirect message
  if (isAuthenticated && user) {
    return (
      <Flex minH="100vh" align="center" justify="center">
        <VStack spacing={4}>
          <Spinner size="xl" color="green.500" thickness="4px" />
          <Text>Przekierowywanie do aplikacji...</Text>
        </VStack>
      </Flex>
    );
  }

  return (
    <Flex minH="100vh" align="center" justify="center" bg={pageBackground}>
      <Container maxW="md">
        <Box
          bg={bgColor}
          p={8}
          rounded="xl"
          shadow="lg"
          borderWidth="1px"
          borderColor={borderColor}
        >
          <VStack spacing={6} align="stretch">
            <VStack spacing={2}>
              <Heading size="lg" textAlign="center">
                Brickly
              </Heading>
              <Text color="gray.600" textAlign="center">
                Zaloguj się aby kontynuować
              </Text>
            </VStack>

            <VStack spacing={4} pt={4}>
              <Text fontSize="sm" color="gray.600" textAlign="center">
                Używamy Microsoft Entra External ID do bezpiecznej autentykacji
              </Text>

              <Button
                colorScheme="blue"
                size="lg"
                w="full"
                onClick={handleLogin}
                leftIcon={<LogIn size={20} />}
                isLoading={isLoading}
                loadingText="Przekierowywanie..."
                _hover={{ transform: "translateY(-2px)", shadow: "lg" }}
                transition="all 0.2s"
              >
                Zaloguj się / Zarejestruj się
              </Button>

              <Text fontSize="xs" color="gray.500" textAlign="center" pt={4}>
                Po kliknięciu zostaniesz przekierowany do bezpiecznej strony logowania Microsoft.
                Jeśli nie masz konta, możesz je utworzyć podczas procesu logowania.
              </Text>
            </VStack>
          </VStack>
        </Box>

        <Text fontSize="xs" color="gray.500" textAlign="center" mt={4}>
          Logowanie jest zabezpieczone przez Microsoft Entra External ID
        </Text>
      </Container>
    </Flex>
  );
}
