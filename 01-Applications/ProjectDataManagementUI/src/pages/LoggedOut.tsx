import { useEffect, useContext } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { Button, Container, Heading, Text, VStack, Flex, Spinner } from "@chakra-ui/react";
import { CheckCircle, LogIn, Home } from "lucide-react";
import { useMsal } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import { loginRequest } from "../config/authConfig";
import { AuthContext } from "../context/AuthContext";

export default function LoggedOut() {
  const navigate = useNavigate();
  const location = useLocation();
  const { instance, inProgress } = useMsal();
  const { isAuthenticated, user, loading: authLoading } = useContext(AuthContext);

  useEffect(() => {
    // Final cleanup after logout redirect

    // Clear any remaining non-MSAL storage
    Object.keys(localStorage).forEach(key => {
      if (!key.startsWith('msal.')) {
        localStorage.removeItem(key);
      }
    });
    sessionStorage.clear();
  }, []);

  const isLoading = inProgress !== InteractionStatus.None;

  useEffect(() => {
  }, [isAuthenticated, isLoading, authLoading, user]);

  const handleLogin = async () => {
    try {
      
      // Preserve return URL through OAuth state
      const returnUrl = (location.state as any)?.from?.pathname || "/dashboard";
      
      // Redirect to External ID login/signup page
      await instance.loginRedirect({
        ...loginRequest,
        state: JSON.stringify({ returnUrl }),
      });
      
      // User will be redirected to External ID, then back to /auth/callback
    } catch (error) {
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
  if (isAuthenticated && authLoading) {
    return (
      <Flex minH="100vh" align="center" justify="center">
        <VStack spacing={4}>
          <Spinner size="xl" color="green.500" thickness="4px" />
          <Text>Ładowanie profilu użytkownika...</Text>
        </VStack>
      </Flex>
    );
  }
  return (
    <Container maxW="md" py={20}>
      <VStack spacing={6} textAlign="center">
        <CheckCircle size={64} color="green" />
        <Heading size="lg">Wylogowano pomyślnie</Heading>
        <Text color="gray.600">
          Zostałeś wylogowany z systemu. Twoja sesja została zamknięta.
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
          Zaloguj się ponownie
        </Button>

        <Button
          variant="outline"
          colorScheme="blue"
          size="lg"
          w="full"
          onClick={() => navigate("/")}
          leftIcon={<Home size={20} />}
          _hover={{ transform: "translateY(-2px)", shadow: "lg" }}
          transition="all 0.2s"
        >
          Powrót do strony głównej
        </Button>
      </VStack>
    </Container>
  );
}
