import { Box, Button, Container, Text, Link, VStack, Flex, Spinner } from "@chakra-ui/react";
import { LogIn } from "lucide-react";
import { useEffect } from "react";
import { useLocation } from "react-router-dom";
import { useMsal, useIsAuthenticated, useAccount } from "@azure/msal-react";
import { loginRequest } from "../config/authConfig";

export default function Home() {
  const location = useLocation();
  const { instance, accounts, inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const account = useAccount(accounts[0] || null);

  const isLoading = inProgress === "login" || inProgress === "acquireToken";
  const authLoading = isAuthenticated && !account;

  useEffect(() => {}, [isAuthenticated, isLoading, authLoading, account]);

  const handleLogin = async () => {
    try {
      const returnUrl = (location.state as { from?: { pathname?: string } })?.from?.pathname || "/dashboard";
      await instance.loginRedirect({
        ...loginRequest,
        state: JSON.stringify({ returnUrl }),
      });
    } catch {}
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

  if (isAuthenticated && account) {
    return (
      <Flex minH="100vh" align="center" justify="center" bg="white">
        <VStack spacing={4}>
          <Spinner size="xl" color="gray.400" thickness="3px" />
          <Text color="gray.500">Przekierowywanie do aplikacji...</Text>
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
                Zaloguj się, żeby kontynuować pracę
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
                Zaloguj się / Zarejestruj
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

      <Box
        position="fixed"
        bottom={4}
        left={0}
        right={0}
        textAlign="center"
      >
        <Text fontSize="xs" color="gray.300">
          © 2026 Brickly
        </Text>
      </Box>
    </Flex>
  );
}

