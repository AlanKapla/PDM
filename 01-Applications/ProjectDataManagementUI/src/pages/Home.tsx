import { Box, Button, Container, Heading, HStack, Text, VStack, useColorModeValue, Flex, Spinner } from "@chakra-ui/react";
import { LogIn, Building2 } from "lucide-react";
import { useEffect } from "react";
import { useLocation } from "react-router-dom";
import { useMsal, useIsAuthenticated, useAccount } from "@azure/msal-react";
import { loginRequest } from "../config/authConfig";

export default function Home() {
  const location = useLocation();
  const { instance, accounts, inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const account = useAccount(accounts[0] || null);

  const bg = useColorModeValue("gray.50", "gray.900");
  const cardBg = useColorModeValue("white", "gray.800");
  const textColor = useColorModeValue("gray.700", "gray.200");
  const accentColor = useColorModeValue("blue.600", "blue.400");

  const isLoading = inProgress === "login" || inProgress === "acquireToken";
  const authLoading = isAuthenticated && !account;

  useEffect(() => {
  }, [isAuthenticated, isLoading, authLoading, account]);


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
  // If authenticated, wait for user profile
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

  // If fully authenticated with profile, show redirect message
  if (isAuthenticated && account) {
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
    <Box bg={bg} minH="100vh" py={10} overflowY="auto">
      <Container maxW="container.md" py={10} pb={40}>
        <VStack spacing={8} align="center">
          {/* Logo */}
          <Box
            p={6}
            bg={accentColor}
            rounded="2xl"
            shadow="xl"
            display="inline-flex"
            alignItems="center"
            justifyContent="center"
          >
            <Building2 size={64} color="white" />
          </Box>

          {/* Heading */}
          <VStack spacing={6} textAlign="center">
            <Heading
              size="2xl"
              bgGradient="linear(to-r, blue.400, blue.600)"
              bgClip="text"
              fontWeight="extrabold"
              lineHeight="1.3"
              pb={2}
              pt={1}
            >
              Brickly
            </Heading>
            <Text fontSize="xl" color={textColor} maxW="600px">
              Kompleksowe rozwiązanie do zarządzania projektami i danymi w środowisku wielotenantowym
            </Text>
          </VStack>

          {/* Card with buttons */}
          <Box
            bg={cardBg}
            p={8}
            rounded="2xl"
            shadow="2xl"
            w="100%"
            maxW="500px"
          >
            <VStack spacing={6}>
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
            </VStack>
          </Box>

          {/* Footer info */}
          <HStack spacing={8} pt={6} color={textColor} fontSize="sm">
            <Text>✓ Wielotenantowe</Text>
            <Text>✓ Bezpieczne</Text>
            <Text>✓ Skalowalne</Text>
          </HStack>
        </VStack>
      </Container>
    </Box>
  );
}
