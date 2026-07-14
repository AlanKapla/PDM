import { useEffect, useContext } from "react";
import { useLocation } from "react-router-dom";
import { Box, Button, Container, Link, Text, VStack, Flex, Spinner } from "@chakra-ui/react";
import { LogIn } from "lucide-react";
import { useMsal } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import { loginRequest } from "../config/authConfig";
import { AuthContext } from "../context/AuthContext";

export default function LoggedOut() {
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
      <Flex minH="100vh" align="center" justify="center" bg="white">
        <VStack spacing={4}>
          <Spinner size="xl" color="gray.400" thickness="3px" />
          <Text color="gray.500">Przetwarzanie logowania...</Text>
        </VStack>
      </Flex>
    );
  }
  
  // If MSAL authenticated, wait for user profile
  if (isAuthenticated && authLoading) {
    return (
      <Flex minH="100vh" align="center" justify="center" bg="white">
        <VStack spacing={4}>
          <Spinner size="xl" color="gray.400" thickness="3px" />
          <Text color="gray.500">Ładowanie profilu użytkownika...</Text>
        </VStack>
      </Flex>
    );
  }
  return (
    <Flex minH="100vh" bg="white" align="flex-start" justify="center" pt="12vh" px={4}>
      <Container maxW="440px">
        <VStack spacing={12} align="center" textAlign="center">

          {/* Logo */}
          <VStack spacing={3}>
            <Link href="https://brickly.pro" target="_blank" rel="noopener noreferrer">
              <img src="/logo.png" alt="Brickly" style={{ height: "64px", width: "auto" }} />
            </Link>
          </VStack>

          {/* Card */}
          <Box
            w="full"
            bg="white"
            border="1px solid"
            borderColor="gray.200"
            borderRadius="16px"
            p={8}
          >
            <VStack spacing={4}>
              <Text fontSize="sm" color="gray.500">
                Zostałeś wylogowany z systemu.
              </Text>
              <Button
                size="lg"
                w="full"
                bg="#0047AB"
                color="white"
                fontWeight={700}
                borderRadius="10px"
                _hover={{ bg: "#003A8C", transform: "translateY(-1px)" }}
                transition="all 0.2s"
                leftIcon={<LogIn size={18} />}
                onClick={handleLogin}
                isLoading={isLoading}
                loadingText="Przekierowywanie..."
              >
                Zaloguj się ponownie
              </Button>
            </VStack>
          </Box>

          <Text fontSize="sm" color="gray.400" textAlign="center">
            Kosztorysy · Harmonogramy · Pliki · Komunikacja
          </Text>

          <Link
            href="https://brickly.pro"
            target="_blank"
            rel="noopener noreferrer"
            fontSize="sm"
            color="gray.400"
            _hover={{ color: "#0047AB" }}
          >
            brickly.pro
          </Link>

        </VStack>
      </Container>
    </Flex>
  );
}
