import { useEffect, useState } from "react";
import {
  Box,
  Button,
  Container,
  HStack,
  Text,
  useColorModeValue,
  VStack,
} from "@chakra-ui/react";
import { Cookie } from "lucide-react";

export default function CookieBanner() {
  const [isVisible, setIsVisible] = useState(false);

  const bg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");

  useEffect(() => {
    const consent = localStorage.getItem("cookieConsent");
    if (!consent) {
      setIsVisible(true);
    }
  }, []);

  const handleAccept = () => {
    localStorage.setItem("cookieConsent", "accepted");
    setIsVisible(false);
  };

  const handleReject = () => {
    localStorage.setItem("cookieConsent", "rejected");
    setIsVisible(false);
  };

  if (!isVisible) return null;

  return (
    <Box
      position="fixed"
      bottom={0}
      left={0}
      right={0}
      bg={bg}
      borderTop="1px solid"
      borderColor={borderColor}
      shadow="2xl"
      zIndex={1000}
      py={4}
    >
      <Container maxW="container.xl">
        <HStack
          spacing={{ base: 4, md: 6 }}
          align="center"
          flexDirection={{ base: "column", md: "row" }}
        >
          <HStack flex={1} spacing={3} align="flex-start">
            <Cookie size={24} />
            <VStack align="flex-start" spacing={1} flex={1}>
              <Text fontWeight="semibold" fontSize="md">
                Ta strona używa plików cookies
              </Text>
              <Text fontSize="sm" color="gray.600">
                Używamy plików cookies do zapewnienia prawidłowego działania aplikacji,
                uwierzytelniania użytkowników oraz analizy ruchu. Kontynuując korzystanie
                ze strony, wyrażasz zgodę na ich użycie.
              </Text>
            </VStack>
          </HStack>

          <HStack spacing={3} flexShrink={0}>
            <Button
              size="sm"
              variant="outline"
              onClick={handleReject}
              minW="100px"
            >
              Odrzuć
            </Button>
            <Button
              size="sm"
              colorScheme="primary"
              onClick={handleAccept}
              minW="100px"
            >
              Akceptuję
            </Button>
          </HStack>
        </HStack>
      </Container>
    </Box>
  );
}
