import { useEffect, useState } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import {
  Box,
  Button,
  Flex,
  Heading,
  Text,
  Spinner,
  VStack,
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
        if (result) setSuccess(true);
        else setError("Link aktywacyjny jest nieprawidłowy lub wygasł.");
      } catch {
        setError("Wystąpił błąd podczas aktywacji. Spróbuj ponownie później.");
      } finally {
        setLoading(false);
      }
    };

    activate();
  }, [token]);

  return (
    <Flex
      justify="center"
      align="center"
      minH="100vh"
      bg="gray.50"
      px={4}
    >
      <Box
        bg="white"
        border="1px solid"
        borderColor="gray.200"
        rounded="lg"
        p={10}
        width="100%"
        maxW="480px"
      >
        <VStack spacing={6} textAlign="center">
          
          {/* ŁADOWANIE */}
          {loading && (
            <>
              <Spinner size="xl" color="gray.600" thickness="4px" />
              <Heading size="lg" color="gray.800">
                Aktywowanie konta...
              </Heading>
              <Text color="gray.600">
                Proszę czekać, sprawdzamy poprawność linku aktywacyjnego.
              </Text>
            </>
          )}

          {/* SUKCES */}
          {!loading && success && (
            <>
              <Icon as={CheckCircle} boxSize={16} color="green.400" />
              <Heading size="lg" color="gray.200">
                Konto aktywowane!
              </Heading>
              <Text color="gray.400">
                Twoje konto zostało pomyślnie aktywowane.
                Możesz teraz zalogować się do systemu.
              </Text>

              <Button
                onClick={() => navigate("/login")}
                bg="blue.600"
                _hover={{ bg: "blue.700" }}
                width="100%"
                size="lg"
              >
                Przejdź do logowania
              </Button>
            </>
          )}

          {/* BŁĄD */}
          {!loading && error && (
            <>
              <Icon as={XCircle} boxSize={16} color="red.400" />
              <Heading size="lg" color="gray.200">
                Aktywacja nie powiodła się
              </Heading>
              <Text color="gray.400" fontSize="sm" px={2}>
                {error}
              </Text>

              <Button
                variant="outline"
                borderColor="#1e1e1e"
                _hover={{ bg: "#181818" }}
                onClick={() => navigate("/login")}
                width="100%"
                size="lg"
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
