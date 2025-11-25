import { useEffect, useState, useRef } from "react";
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
import { acceptTenantInvitation } from "../services/tenantService";

export default function AcceptInvitation() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get("token");

  const [loading, setLoading] = useState(true);
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState("");
  const hasProcessed = useRef(false);

  useEffect(() => {
    console.log("[Component] AcceptInvitation useEffect triggered");
    console.log("[Component] Token z URL:", token);
    console.log("[Component] hasProcessed.current:", hasProcessed.current);
    
    if (!token) {
      console.error("[Component] Brak tokenu w URL");
      setLoading(false);
      setError("Nieprawidłowy link zaproszenia. Brak tokenu.");
      return;
    }

    if (hasProcessed.current) {
      console.log("[Component] Request już został wykonany, pomijam");
      return;
    }

    hasProcessed.current = true;

    const acceptInvite = async () => {
      console.log("[Component] Rozpoczynam akceptację zaproszenia...");
      try {
        const result = await acceptTenantInvitation(token);
        console.log("[Component] Wynik akceptacji:", result);
        
        if (result) {
          console.log("[Component] Sukces!");
          setSuccess(true);
        } else {
          console.error("[Component] Akceptacja zwróciła false");
          setError("Akceptacja zaproszenia nie powiodła się. Link może być nieprawidłowy, wygasły lub jesteś już członkiem tej organizacji.");
        }
      } catch (err) {
        console.error("[Component] Wyjątek podczas akceptacji:", err);
        setError("Wystąpił błąd podczas akceptacji zaproszenia. Spróbuj ponownie później.");
      } finally {
        setLoading(false);
      }
    };

    acceptInvite();
    // eslint-disable-next-line react-hooks/exhaustive-deps
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
              <Heading size="lg">Przetwarzanie zaproszenia...</Heading>
              <Text color="gray.600">
                Proszę czekać, dodajemy Cię do organizacji.
              </Text>
            </>
          )}

          {!loading && success && (
            <>
              <Icon as={CheckCircle} boxSize={20} color="green.500" />
              <Heading size="xl">Zaproszenie zaakceptowane!</Heading>
              <Text color="gray.600">
                Pomyślnie dołączyłeś do organizacji. Możesz teraz przejść do panelu.
              </Text>
              <Button
                colorScheme="blue"
                size="lg"
                width="100%"
                onClick={() => navigate("/tenants")}
              >
                Przejdź do organizacji
              </Button>
            </>
          )}

          {!loading && error && (
            <>
              <Icon as={XCircle} boxSize={20} color="red.500" />
              <Heading size="lg">Nie udało się zaakceptować zaproszenia</Heading>
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
                onClick={() => navigate("/tenants")}
              >
                Powrót do organizacji
              </Button>
            </>
          )}
        </VStack>
      </Box>
    </Flex>
  );
}
