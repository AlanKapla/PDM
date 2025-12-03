import { useEffect, useState } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import {
  Box,
  Button,
  Flex,
  Heading,
  Text,
  Spinner,
  Alert,
  AlertIcon,
  AlertTitle,
  AlertDescription,
  VStack,
  useColorModeValue,
  Icon,
} from "@chakra-ui/react";
import { CheckCircle, XCircle } from "lucide-react";
import { activateAccount } from "../services/authService";

export default function ActivateAccount() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get("token");

  const [loading, setLoading] = useState(true);
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!token) {
      setLoading(false);
      setError("Nieprawidłowy link aktywacyjny. Brak tokenu.");
      return;
    }

    const activate = async () => {
      try {
        const result = await activateAccount(token);
        if (result) {
          setSuccess(true);
        } else {
          setError("Aktywacja nie powiodła się. Link może być nieprawidłowy lub wygasły.");
        }
      } catch (err) {
        setError("Wystąpił błąd podczas aktywacji. Spróbuj ponownie później.");
      } finally {
        setLoading(false);
      }
    };

    activate();
  }, [token]);

  const cardBg = useColorModeValue("white", "gray.800");
  const pageBg = useColorModeValue("gray.50", "gray.900");

  return (
    <Flex justify="center" align="center" minH="100vh" bg={pageBg} px={{ base: 4, md: 0 }}>
      <Box bg={cardBg} p={{ base: 6, md: 8 }} rounded="lg" shadow="lg" maxW="500px" width="100%">
        <VStack spacing={6} textAlign="center">
          {loading && (
            <>
              <Spinner size="xl" color="blue.500" thickness="4px" />
              <Heading size="lg">Aktywowanie konta...</Heading>
              <Text color="gray.600">
                Proszę czekać, weryfikujemy link aktywacyjny.
              </Text>
            </>
          )}

          {!loading && success && (
            <>
              <Icon as={CheckCircle} boxSize={20} color="green.500" />
              <Heading size="xl">Konto aktywowane!</Heading>
              <Text color="gray.600">
                Twoje konto zostało pomyślnie aktywowane. Możesz się teraz zalogować.
              </Text>
              <Button
                colorScheme="blue"
                size="lg"
                width="100%"
                onClick={() => navigate("/login")}
              >
                Przejdź do logowania
              </Button>
            </>
          )}

          {!loading && error && (
            <>
              <Icon as={XCircle} boxSize={20} color="red.500" />
              <Heading size="lg">Aktywacja nie powiodła się</Heading>
              <Alert status="error" rounded="md">
                <AlertIcon />
                <Box>
                  <AlertTitle>Błąd</AlertTitle>
                  <AlertDescription>{error}</AlertDescription>
                </Box>
              </Alert>
              <Button
                variant="outline"
                colorScheme="blue"
                size="lg"
                width="100%"
                onClick={() => navigate("/login")}
              >
                Powrót do logowania
              </Button>
            </>
          )}
        </VStack>
      </Box>
    </Flex>
  );
}
