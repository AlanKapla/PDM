import {
  Box,
  Button,
  Container,
  Heading,
  Text,
  VStack,
  HStack,
  Icon,
} from "@chakra-ui/react";
import { LogIn, UserPlus, Database } from "lucide-react";
import { useNavigate } from "react-router-dom";

export default function Home() {
  const navigate = useNavigate();

  return (
    <Box bg="gray.50" minH="100vh" py={20} px={4}>
      <Container maxW="600px">
        <VStack spacing={12}>

          {/* LOGO — styl Linear */}
          <Box
            w="90px"
            h="90px"
            rounded="xl"
            bg="white"
            border="1px solid"
            borderColor="gray.200"
            display="flex"
            alignItems="center"
            justifyContent="center"
          >
            <Icon as={Database} boxSize={42} color="gray.200" />
          </Box>

          {/* TYTUŁ */}
          <VStack spacing={4}>
            <Heading
              size="2xl"
              fontWeight="bold"
              color="white"
              textAlign="center"
              letterSpacing="-0.5px"
            >
              Project Data Management
            </Heading>

            <Text
              fontSize="lg"
              color="gray.600"
              textAlign="center"
              maxW="450px"
              lineHeight="1.6"
            >
              Nowoczesna platforma do zarządzania projektami, organizacjami i plikami
              w środowisku wielotenantowym.
            </Text>
          </VStack>

          {/* KARTA — minimalistyczna, bez shadowów */}
          <Box
            w="100%"
            bg="white"
            border="1px solid"
            borderColor="gray.200"
            rounded="lg"
            p={8}
          >
            <VStack spacing={6}>
              <Text color="gray.700" fontSize="md" textAlign="center">
                Zaloguj się lub utwórz konto, aby rozpocząć pracę.
              </Text>

              <VStack spacing={3} w="100%">
                <Button
                  leftIcon={<LogIn size={18} />}
                  w="100%"
                  size="lg"
                  bg="blue.600"
                  _hover={{ bg: "blue.500" }}
                  color="white"
                  fontWeight="semibold"
                  onClick={() => navigate("/login")}
                >
                  Zaloguj się
                </Button>

                <Button
                  leftIcon={<UserPlus size={18} />}
                  w="100%"
                  size="lg"
                  variant="outline"
                  color="gray.700"
                  borderColor="gray.300"
                  _hover={{ bg: "#181818", borderColor: "#2e2e2e" }}
                  onClick={() => navigate("/register")}
                >
                  Załóż konto
                </Button>
              </VStack>
            </VStack>
          </Box>

          {/* FOOTER — prosta linia */}
          <HStack spacing={6} color="gray.500" fontSize="sm" pt={4}>
            <Text>Wielotenantowe</Text>
            <Text>Bezpieczne</Text>
            <Text>Skalowalne</Text>
          </HStack>
        </VStack>
      </Container>
    </Box>
  );
}
