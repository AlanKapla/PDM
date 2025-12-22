import { Box, Button, Container, Heading, HStack, Text, VStack, useColorModeValue } from "@chakra-ui/react";
import { LogIn, Building2 } from "lucide-react";
import { useNavigate } from "react-router-dom";

export default function Home() {
  const navigate = useNavigate();

  const bg = useColorModeValue("gray.50", "gray.900");
  const cardBg = useColorModeValue("white", "gray.800");
  const textColor = useColorModeValue("gray.700", "gray.200");
  const accentColor = useColorModeValue("blue.600", "blue.400");

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
              Project Data Management
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
              <Text fontSize="lg" color={textColor} textAlign="center" fontWeight="medium">
                Zaloguj się lub utwórz nowe konto za pomocą Microsoft
              </Text>

              <Button
                leftIcon={<LogIn size={20} />}
                colorScheme="blue"
                size="lg"
                w="100%"
                onClick={() => navigate("/login")}
                fontSize="md"
                fontWeight="semibold"
              >
                Zaloguj się / Zarejestruj się
              </Button>
              
              <Text fontSize="xs" color="gray.500" textAlign="center">
                Używamy Microsoft Entra External ID do bezpiecznego logowania
              </Text>
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
