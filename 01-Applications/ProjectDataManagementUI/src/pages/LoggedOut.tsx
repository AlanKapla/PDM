import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { Box, Button, Container, Heading, Text, VStack } from "@chakra-ui/react";
import { CheckCircle } from "lucide-react";

export default function LoggedOut() {
  const navigate = useNavigate();

  useEffect(() => {
    // Final cleanup after logout redirect
    console.log("✅ LoggedOut: Cleaning up remaining app storage...");
    
    // Clear any remaining non-MSAL storage
    Object.keys(localStorage).forEach(key => {
      if (!key.startsWith('msal.')) {
        localStorage.removeItem(key);
      }
    });
    sessionStorage.clear();
  }, []);

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
          onClick={() => navigate("/login")}
        >
          Zaloguj się ponownie
        </Button>
      </VStack>
    </Container>
  );
}
